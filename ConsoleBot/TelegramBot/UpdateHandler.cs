using System.Threading;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.Helpers;
using ConsoleBot.Scenarios;
using ConsoleBot.TelegramBot.Dto;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;


namespace ConsoleBot.TelegramBot
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoReportService _toDoReportService;
        private readonly ToDoListService _toDoListService;
        private readonly ToDoListCallbackDto _toDoListCallbackDto;

        public delegate void MessageEventHandler(string message);
        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;

        private readonly IEnumerable<IScenario> _scenarios;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly ToDoList? list;
        private readonly Guid? toDoListId;
        private static int _pageSize = 5;

        public UpdateHandler(ITelegramBotClient botClient, IUserService userService, IToDoService toDoService, IToDoReportService toDoReportService, IEnumerable<IScenario> scenarios, IScenarioContextRepository contextRepository, ToDoListService toDoListService)
        {
            _botClient = botClient;
            _userService = userService;
            _toDoService = toDoService;
            _toDoReportService = toDoReportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
            _toDoListService = toDoListService;
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            await (update switch
            {
                { Message: { } message } => OnMessage(update, message, cancellationToken),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(update, callbackQuery, cancellationToken),
                _ => OnUnknown(update)
            });
        }

        private async Task OnMessage(Update update, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message == null)
                    return;
                var chatId = message.Chat.Id;

                var telegramId = message.From?.Id ?? 0;
                var firstName = message.From?.FirstName ?? "Unknown";

                if (telegramId == 0)
                {
                    await _botClient.SendMessage(chatId, "Ошибка: не удалось определить пользователя.",
                        cancellationToken: cancellationToken);
                    return;
                }


                var user = await _userService.GetUserAsync(telegramId, firstName, cancellationToken);
                if (user == null)
                {
                    await _userService.RegisterUserAsync(telegramId, firstName, cancellationToken);
                    user = await _userService.GetUserAsync(telegramId, firstName, cancellationToken);
                }

                if (user == null)
                {
                    await _botClient.SendMessage(message.Chat.Id,
                        "Ошибка регистрации пользователя. Попробуйте /start позже.", cancellationToken: cancellationToken);
                    return;
                }

                var userId = message.From.Id;

                var keyboardMarkup = CreateKeyboard(userId, cancellationToken);
                var keyboard = new ReplyKeyboardMarkup("/addtask", "/show", "/report");
                var command = message.Text.Split(' ').First();

                var context = await _contextRepository.GetContext(userId, cancellationToken);

                if (context != null && message.Text == "/cancel")
                {
                    await _contextRepository.ResetContext(userId, cancellationToken);
                    await _botClient.SendMessage(chatId, "Сценарий остановлен.", cancellationToken: cancellationToken);
                    await SendDefaultKeyboard(userId, cancellationToken);
                    return;
                }
                else if (context != null)
                {
                    // Если контекст сценария найден, обрабатываем его
                    await ProcessScenario(context, message, cancellationToken);
                    return;
                }

                switch (command)
                {
                    case "/start":
                        await StartCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/addtask":
                        context = new ScenarioContext(ScenarioType.AddTask);
                        var addTaskScenario = new AddTaskScenario(_userService, _toDoService, _contextRepository, _toDoListService);
                        await _contextRepository.SetContext(userId, context, cancellationToken);
                        await ProcessScenario(context, message, cancellationToken);
                        break;
                    case "/show":
                        await ShowCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/report":
                        await ReportCommand(_botClient, update, chatId, user, _toDoReportService, cancellationToken);
                        break;
                    case "/find":
                        await FindCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/help":
                        await HelpCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/info":
                        await InfoCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    default:
                        await _botClient.SendMessage(update.Message.Chat, "Неизвестная команда");
                        break;
                }
            }
            catch (Exception ex)
            {
                _botClient.SendMessage(update.Message.Chat, $"Произошла ошибка: {ex.Message}");
            }
        }
        private async Task OnUnknown(Update update)
        {
            await _botClient.SendMessage(update.Message.Chat.Id, "Неизвестный тип обновления.");
        }

        private async Task StartCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                await botClient.SendMessage(update.Message.Chat, "Пользователь не найден. Используйте /start.", cancellationToken: cancellationToken);
                return;
            }
            var keyboard = new ReplyKeyboardMarkup();
            await botClient.SendMessage(update.Message.Chat, $"Привет, {user.TelegramUserName}! " +
                $"Я твой помощник по управлению задачами.");
            await botClient.SendMessage(update.Message.Chat, "Добро пожаловать в Телеграмм-бот!");
            await botClient.SendMessage(update.Message.Chat, "Выберите команду:", replyMarkup: keyboard);
            await botClient.SendMessage(update.Message.Chat, "/start - начать работу");
            await botClient.SendMessage(update.Message.Chat, "/addtask - добавить задачу в список");
            await botClient.SendMessage(update.Message.Chat, "/show - отображение списка активных задач");
            await botClient.SendMessage(update.Message.Chat, "/report - отображение всех задач пользователя");
            await botClient.SendMessage(update.Message.Chat, "/find - поиск задачи");
            await botClient.SendMessage(update.Message.Chat, "/help - справка по использованию");
            await botClient.SendMessage(update.Message.Chat, "/info - информация о программе");
        }
        public IScenario GetScenario(ScenarioType scenarioType, ScenarioContext context)
        {
            var matchScenario = _scenarios.FirstOrDefault(s => s.CanHandle(scenarioType));
            if (matchScenario == null)
            {
                throw new Exception($"Сценарий {scenarioType} не найден");
            }
            return matchScenario;
        }
        public async Task ProcessScenario(ScenarioContext context, Message message, CancellationToken cancellationToken)
        {
            // Найдем сценарий, соответствующий текущему типу сценария 
            var scenario = GetScenario(context.CurrentScenario, context);
            var currentStep = context.CurrentStep;
            var result = await scenario.HandleMessageAsync(_botClient, context, message, list,  cancellationToken);

            // Если сценарий завершился, сбросим контекст
            if (result == ScenarioResult.Completed)
            {
                await _contextRepository.ResetContext(message.Chat.Id, cancellationToken);
                await SendDefaultKeyboard(message.Chat.Id, cancellationToken);
            }
            else
            {
                await _contextRepository.SetContext(message.Chat.Id, context, cancellationToken);
                await SendCancelKeyboard(message.Chat.Id, cancellationToken);
            }
        }

        public async Task ShowCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

            if (lists.Count == 0)
            {
                await botClient.SendMessage(chat, "У вас нет созданных списков задач.", cancellationToken: cancellationToken);
            }
            //через LINQ
            var keyboardRows = new List<IEnumerable<InlineKeyboardButton>>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📌 Без списка",
                    new PagedListCallbackDto("show", null, 0).ToString())
                }
            }
            .Concat(
            lists.Select(l => new[]
            {
                InlineKeyboardButton.WithCallbackData(l.Name,
                new PagedListCallbackDto("show", l.Id, 0).ToString())
            }))
            .Concat(new[]
            {
                new[]
                {
                InlineKeyboardButton.WithCallbackData("🆕 Добавить", "addList")
                },
                new[]
                {
                InlineKeyboardButton.WithCallbackData("❌ Удалить", "deleteList")
                }
            });

            var inlineKeyboard = new InlineKeyboardMarkup(keyboardRows);

            await botClient.SendMessage(chat, "Выберите список:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }
        public async Task CompleteTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId, cancellationToken);
            if (tasks.Count == 0)
            {
                await botClient.SendMessage(chat, "У вас нет активных задач.", cancellationToken: cancellationToken);
                return;
            }

            var rows = new List<InlineKeyboardButton[]>();
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var data = $"complete|{task.Id}";
                rows.Add(new[]{InlineKeyboardButton.WithCallbackData($"{i + 1}. {task.Name}", data)});
            }

            var keyboard = new InlineKeyboardMarkup(rows);

            await botClient.SendMessage(
                chat,
                "Выберите задачу для завершения:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var telegramId = callbackQuery.From?.Id ?? 0;
            if (telegramId == 0)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка пользователя", cancellationToken: cancellationToken);
                return;
            }
            var user = await _userService.GetUserByTelegramUserIdAsync(telegramId, cancellationToken);
            if (user == null)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Незарегистрированный пользователь",
                    cancellationToken: cancellationToken);
                return;
            }

            var data = callbackQuery.Data ?? string.Empty;
            var baseDto = CallbackDto.FromString(data);
            var action = baseDto.Action.ToLowerInvariant();

            var context = await _contextRepository.GetContext(callbackQuery.From.Id, cancellationToken);

            switch (action)
            {
                case "show":
                    await HandleShowCallback(callbackQuery, cancellationToken);
                    break;

                case "addlist":
                    await HandleAddListCallback(callbackQuery, cancellationToken);
                    break;

                case "deletelist":
                case "deletelist_yes":
                case "deletelist_no":
                    await HandleDeleteListCallback(callbackQuery, context, cancellationToken);
                    break;

                case "selectlist":
                    await HandleSelectListCallback(callbackQuery, context, cancellationToken);
                    break;

                case "showtask":
                    await HandleShowTaskCallback(callbackQuery, cancellationToken);
                    break;

                case "completetask":
                    await HandleCompleteTaskCallback(callbackQuery, cancellationToken);
                    break;

                case "deletetask":
                case "deletetask_yes":
                case "deletetask_no":
                    await HandleDeleteTaskCallback(callbackQuery, cancellationToken);
                    break;

                case "show_completed":
                    await HandleShowCompletedCallback(callbackQuery, user, cancellationToken);
                    break;

                default:
                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Неизвестное действие",
                        cancellationToken: cancellationToken);
                    break;
            }
        }
        private async Task HandleShowCallback(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var dto = PagedListCallbackDto.FromString(callbackQuery.Data!);
            await HandleShowAction(callbackQuery, dto, ct);
        }

        private async Task HandleAddListCallback(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var addListContext = new ScenarioContext(ScenarioType.AddList);
            await _contextRepository.SetContext(callbackQuery.From.Id, addListContext, ct);
            await ProcessScenario(addListContext, callbackQuery.Message!, ct);
        }

        private async Task HandleDeleteListCallback(CallbackQuery callbackQuery, ScenarioContext? context, CancellationToken ct)
        {
            if (context == null || context.CurrentScenario != ScenarioType.DeleteList)
            {
                var deleteContext = new ScenarioContext(ScenarioType.DeleteList);
                deleteContext.Data["Callback"] = callbackQuery;
                await _contextRepository.SetContext(callbackQuery.From.Id, deleteContext, ct);
                await ProcessScenario(deleteContext, callbackQuery.Message!, ct);
            }
            else
            {
                context.Data["Callback"] = callbackQuery;
                await _contextRepository.SetContext(callbackQuery.From.Id, context, ct);
                await ProcessScenario(context, callbackQuery.Message!, ct);
            }
        }


        private async Task HandleSelectListCallback(CallbackQuery callbackQuery, ScenarioContext? context, CancellationToken ct)
        {
            if (context != null && context.CurrentScenario == ScenarioType.AddTask)
            {
                var scenario = GetScenario(ScenarioType.AddTask, context) as AddTaskScenario;
                if (scenario != null)
                {
                    await scenario.HandleCallbackQueryAsync(_botClient, context, callbackQuery, ct);
                }
            }
        }

        private async Task HandleShowTaskCallback(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var dto = ToDoItemCallbackDto.FromString(callbackQuery.Data!);
            var item = await _toDoService.Get(dto.ToDoItemId, ct);
            if (item == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Задача не найдена", cancellationToken: ct);
                return;
            }

            var completeDto = new ToDoItemCallbackDto("completetask", item.Id);
            var deleteDto = new ToDoItemCallbackDto("deletetask", item.Id);

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("✅Выполнить", completeDto.ToString()),
            InlineKeyboardButton.WithCallbackData("❌Удалить",   deleteDto.ToString())
        }
    });

            var statusText = item.State == ToDoItemState.Completed ? "выполнена" : "активна";

            var text =
                $"Задача: {item.Name}\n" +
                $"Статус: {statusText}\n" +
                $"Дедлайн: {item.Deadline:dd.MM.yyyy}";

            await _botClient.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                text,
                replyMarkup: keyboard,
                cancellationToken: ct);

            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        }

        private async Task HandleCompleteTaskCallback(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var dto = ToDoItemCallbackDto.FromString(callbackQuery.Data!);
            var item = await _toDoService.Get(dto.ToDoItemId, ct);
            if (item == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Задача не найдена", cancellationToken: ct);
                return;
            }

            await _toDoService.MarkCompletedAsync(item.Id, ct);

            await _botClient.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                $"Задача \"{item.Name}\" отмечена как выполненная.",
                cancellationToken: ct);

            await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Задача выполнена", cancellationToken: ct);
        }

        private async Task HandleDeleteTaskCallback(CallbackQuery callbackQuery, CancellationToken ct)
        {
            var dto = ToDoItemCallbackDto.FromString(callbackQuery.Data!);
            var action = dto.Action.ToLowerInvariant();

            if (action == "deletetask")
            {
                var item = await _toDoService.Get(dto.ToDoItemId, ct);
                if (item == null)
                {
                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Задача не найдена",
                        cancellationToken: ct);
                    return;
                }

                var deleteTaskContext = new ScenarioContext(ScenarioType.DeleteTask);
                deleteTaskContext.Data["ToDoItemId"] = item.Id;
                deleteTaskContext.Data["Callback"] = callbackQuery;

                await _contextRepository.SetContext(callbackQuery.From.Id, deleteTaskContext, ct);
                await ProcessScenario(deleteTaskContext, callbackQuery.Message!, ct);
            }
            else if (action == "deletetask_yes" || action == "deletetask_no")
            {
                var deleteContext = await _contextRepository.GetContext(callbackQuery.From.Id, ct);
                if (deleteContext != null && deleteContext.CurrentScenario == ScenarioType.DeleteTask)
                {
                    deleteContext.Data["Callback"] = callbackQuery;
                    await _contextRepository.SetContext(callbackQuery.From.Id, deleteContext, ct);
                    await ProcessScenario(deleteContext, callbackQuery.Message!, ct);
                }
                else
                {
                    await _botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        "Сценарий удаления задачи не найден",
                        cancellationToken: ct);
                }
            }
        }


        private async Task HandleShowCompletedCallback(CallbackQuery callbackQuery, ToDoUser user, CancellationToken ct)
        {
            var dto = PagedListCallbackDto.FromString(callbackQuery.Data!);

            var tasks = dto.ToDoListId.HasValue
                ? await _toDoService.GetByUserIdAndList(user.UserId, dto.ToDoListId.Value, ct)
                : await _toDoService.GetAllByUserIdAsync(user.UserId, ct);

            var completed = tasks.Where(t => t.State == ToDoItemState.Completed).ToList();
            if (!completed.Any())
            {
                await _botClient.EditMessageText(
                    callbackQuery.Message!.Chat.Id,
                    callbackQuery.Message.MessageId,
                    "Задач нет",
                    cancellationToken: ct);
                return;
            }

            var pairs = completed
                .Select(t => new KeyValuePair<string, string>(
                    t.Name,
                    new ToDoItemCallbackDto("showtask", t.Id).ToString()))
                .ToList();

            var keyboard = BuildPagedButtons(pairs, dto);

            await _botClient.EditMessageText(
                callbackQuery.Message!.Chat.Id,
                callbackQuery.Message.MessageId,
                "Выполненные задачи:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        private async Task HandleShowAction(CallbackQuery callbackQuery, PagedListCallbackDto dto, CancellationToken ct)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, ct);
            if (user == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка обработки данных.", cancellationToken: ct);
                return;
            }

            var tasks = await _toDoService.GetByUserIdAndList(user.UserId, dto.ToDoListId, ct);

            var listName = dto.ToDoListId.HasValue ? "выбранного списка" : "без списка";

            if (!tasks.Any())
            {
                await _botClient.EditMessageText(
                    callbackQuery.Message!.Chat.Id,
                    callbackQuery.Message.MessageId,
                    $"Задачи из {listName}:\nЗадач нет",
                    cancellationToken: ct);
                return;
            }

            var pairs = tasks
                .Select(t => new KeyValuePair<string, string>(
                    t.Name,
                    new ToDoItemCallbackDto("showtask", t.Id).ToString()))
                .ToList();

            var keyboard = BuildPagedButtons(pairs, dto);

            await _botClient.EditMessageText(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                $"Задачи из {listName}:",
                replyMarkup: keyboard,
                cancellationToken: ct);
            
            var completedDto = new PagedListCallbackDto("show_completed", dto.ToDoListId, 0);
            var completedRow = new[]
            {
            InlineKeyboardButton.WithCallbackData("☑️Посмотреть выполненные", completedDto.ToString())
            };

            var rows = keyboard.InlineKeyboard.ToList();
            rows.Add(completedRow);
            var finalKeyboard = new InlineKeyboardMarkup(rows);

            await _botClient.EditMessageText(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                $"Задачи из {listName}:",
                replyMarkup: finalKeyboard,
                cancellationToken: ct);
        }

        private InlineKeyboardMarkup BuildPagedButtons(
                IReadOnlyList<KeyValuePair<string, string>> callbackData,
                PagedListCallbackDto listDto)
        {
            var totalItems = callbackData.Count;
            if (totalItems == 0)
                return new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton[]>());

            var totalPages = (int)Math.Ceiling(totalItems / (double)_pageSize);
            var page = Math.Min(Math.Max(listDto.Page, 0), Math.Max(totalPages - 1, 0));

            var pageItems = callbackData
                .GetBatchByNumber(_pageSize, page)
                .ToList();

            //через LINQ
            var rows = pageItems.Select(kv => new[]
                {
                    InlineKeyboardButton.WithCallbackData(kv.Key, kv.Value)
                })
                .ToList();

            var navButtons = new List<InlineKeyboardButton>();

            if (page > 0)
            {
                var prevDto = new PagedListCallbackDto(listDto.Action, listDto.ToDoListId, page - 1);
                navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️", prevDto.ToString()));
            }

            if (page < totalPages - 1)
            {
                var nextDto = new PagedListCallbackDto(listDto.Action, listDto.ToDoListId, page + 1);
                navButtons.Add(InlineKeyboardButton.WithCallbackData("➡️", nextDto.ToString()));
            }

            if (navButtons.Any())
                rows.Add(navButtons.ToArray());

            return new InlineKeyboardMarkup(rows);
        }
        private async Task<ReplyKeyboardMarkup> CreateKeyboard(long telegramUserId, CancellationToken cancellationToken)
        {
            // Проверяем регистрацию пользователя
            var isRegistered = await _userService.IsUserRegistered(telegramUserId, cancellationToken);
            var buttons = isRegistered ?
            ["/addtask", "/show", "/report"] :
            new KeyboardButton[] { "/start" };
            return new ReplyKeyboardMarkup(buttons);
        }
        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource handleErrorSource, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            await Task.CompletedTask; // Чтобы удовлетворить контракт асинхронного метода
        }
        private async Task SendDefaultKeyboard(long chatId, CancellationToken cancellationToken)
        {
            {
                var defaultKeyboard = new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "/addtask", "/show", "/report" } })
                {
                    ResizeKeyboard = true
                };
                await _botClient.SendMessage(chatId, "Выберите действие", replyMarkup: defaultKeyboard, cancellationToken: cancellationToken);
            }
        }

        private async Task SendCancelKeyboard(long chatId, CancellationToken cancellationToken)
        {
            var defaultKeyboard = new ReplyKeyboardMarkup("/cancel");
            await _botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: defaultKeyboard, cancellationToken: cancellationToken);
        }
        private async Task ReportCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, IToDoReportService toDoReportService, CancellationToken cancellationToken)
        {
            var stats = await _toDoReportService.GetUserStatsAsync(user.UserId, cancellationToken);

            var reportMessage =
                $"Статистика по задачам на {stats.generatedAt:yyyy-MM-dd HH:mm:ss}:\n" +
                $"Всего: {stats.total};\n" +
                $"Завершенных: {stats.completed};\n" +
                $"Активных: {stats.active};";

            await botClient.SendMessage(update.Message.Chat, reportMessage);
        }
        private async Task FindCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var parts = update.Message.Text.Split(' ');
            if (parts.Length < 2)
            {
                await botClient.SendMessage(update.Message.Chat, "Формат команды: /find <префикс>");
                return;
            }

            var namePrefix = parts[1];
            var foundTasks = await _toDoService.FindAsync(user, namePrefix, cancellationToken);

            if (foundTasks.Any())
            {
                var output = string.Join("\n", foundTasks.Select((task, idx) =>
                    $"{idx + 1}. {task.Name} - создана {task.CreatedAt}"));
                await botClient.SendMessage(update.Message.Chat, $"Найденные задачи:\n{output}");
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Задачи не найдены.");
            }
        }

        private async Task HelpCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(update.Message.Chat, "Доступные команды:\n" +
               "/start - начало работы\n" +
               "/addtask - добавить задачу\n" +
               "/show - показать списки задач\n" +
               "/report - статистика по задачам пользователя\n" +
               "/find - поиск задач пользователя\n" +
               "/help - помощь\n" +
               "/info - информация о программе");
        }
        private async Task InfoCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(update.Message.Chat, $"Телеграм-бот для управления задачами, версия {Program.version}." +
                $"\nСоздан {Program.created_date}, обновлен {Program.updated_date}." +
                $"\nНовые функции: {Program.whatsNew_text}.");
        }
    }
}
