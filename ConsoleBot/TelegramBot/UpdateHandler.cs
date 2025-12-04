using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
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
                var userId = message.From.Id;

                var user = await _userService.GetUserAsync(message.From.Id, message.From.FirstName, cancellationToken);
                if (user == null)
                {
                    await _userService.RegisterUserAsync(message.From.Id, cancellationToken);
                    user = await _userService.GetUserAsync(message.From.Id, message.From.FirstName, cancellationToken);
                }

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
                    case "/completetask":
                        await CompleteTaskCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/removetask":
                        await RemoveTaskCommand(_botClient, update, chatId, user, cancellationToken);
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
            var keyboard = new ReplyKeyboardMarkup();
            await botClient.SendMessage(update.Message.Chat, $"Привет, {user.TelegramUserName}! " +
                $"Я твой помощник по управлению задачами.");
            await botClient.SendMessage(update.Message.Chat, "Добро пожаловать в Телеграмм-бот!");
            await botClient.SendMessage(update.Message.Chat, "Выберите команду:", replyMarkup: keyboard);
            await botClient.SendMessage(update.Message.Chat, "/start - начать работу");
            await botClient.SendMessage(update.Message.Chat, "/addtask - добавить задачу в список");
            await botClient.SendMessage(update.Message.Chat, "/show - отображение списка активных задач");
            await botClient.SendMessage(update.Message.Chat, "/completetask - отметить задачу как завершенную");
            await botClient.SendMessage(update.Message.Chat, "/removetask - удалить задачу из списка");
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
                //return;
            }

            var keyboardRows = new List<IEnumerable<InlineKeyboardButton>>
            {
                // Кнопка "Без списка"
                new[] { InlineKeyboardButton.WithCallbackData("📌 Без списка", new ToDoListCallbackDto("show", null).ToString()) }
            };
            Console.WriteLine($"Пользователь {user.UserId}: {lists.Count} списков");
            
            // Кнопки для каждого списка
            foreach (var list in lists)
            {
                keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name, new ToDoListCallbackDto("show", list.Id).ToString()) });
            }
            
            // Кнопка "Добавить"
            keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData("🆕 Добавить", "AddList") });

            // Кнопка "Удалить"
            keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Удалить", "DeleteList") });

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

        private async Task RemoveTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var tasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (tasks.Count == 0)
            {
                await botClient.SendMessage(chat, "У вас нет задач для удаления.", cancellationToken: cancellationToken);
                return;
            }

            var rows = new List<InlineKeyboardButton[]>();
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var data = $"delete|{task.Id}";
                rows.Add(new[]
                {
            InlineKeyboardButton.WithCallbackData($"{i + 1}. {task.Name}", data)});
            }

            var keyboard = new InlineKeyboardMarkup(rows);

            await botClient.SendMessage(
                chat,
                "Выберите задачу для удаления:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HelpCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(update.Message.Chat, "Доступные команды:\n" +
               "/start - начало работы\n" +
               "/addtask - добавить задачу\n" +
               "/show - показать активные задачи\n" +
               "/completetask - отметить задачу как выполненную\n" +
               "/removetask - удалить задачу\n" +
               "/report - отображение всех задач пользователя\n" +
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
                var defaultKeyboard = new ReplyKeyboardMarkup(new[]{ new KeyboardButton[] { "/addtask", "/show", "/report" }})
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

        private async Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, cancellationToken);
            if (user == null)
            {
                await _botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    "Незарегистрированный пользователь",
                    cancellationToken: cancellationToken);
                return;
            }

            var data = callbackQuery.Data ?? string.Empty;
            var parts = data.Split('|', 2);
            var action = parts[0].ToLowerInvariant();

            var context = await _contextRepository.GetContext(callbackQuery.From.Id, cancellationToken);

            switch (action)
            {
                case "show":
                    {
                        var showDto = ToDoListCallbackDto.FromString(callbackQuery.Data!);
                        await HandleShowAction(callbackQuery, showDto, cancellationToken);
                        break;
                    }

                case "addlist":
                    {
                        var addListContext = new ScenarioContext(ScenarioType.AddList);
                        await _contextRepository.SetContext(callbackQuery.From.Id, addListContext, cancellationToken);
                        await ProcessScenario(addListContext, callbackQuery.Message!, cancellationToken);
                        break;
                    }

                case "deletelist":
                    {
                        if (context == null || context.CurrentScenario != ScenarioType.DeleteList)
                        {
                            var deleteContext = new ScenarioContext(ScenarioType.DeleteList);
                            await _contextRepository.SetContext(callbackQuery.From.Id, deleteContext, cancellationToken);
                            await ProcessScenario(deleteContext, callbackQuery.Message!, cancellationToken);
                        }
                        else
                        {
                            context.Data["Callback"] = callbackQuery;
                            await _contextRepository.SetContext(callbackQuery.From.Id, context, cancellationToken);
                            await ProcessScenario(context, callbackQuery.Message!, cancellationToken);
                        }
                        break;
                    }

                case "yes":
                case "no":
                    {
                        if (context != null && context.CurrentScenario == ScenarioType.DeleteList)
                        {
                            context.Data["Callback"] = callbackQuery;
                            await _contextRepository.SetContext(callbackQuery.From.Id, context, cancellationToken);
                            await ProcessScenario(context, callbackQuery.Message!, cancellationToken);
                        }
                        break;
                    }

                case "selectlist":
                    {
                        if (context != null && context.CurrentScenario == ScenarioType.AddTask)
                        {
                            var scenario = GetScenario(ScenarioType.AddTask, context) as AddTaskScenario;
                            if (scenario != null)
                            {
                                await scenario.HandleCallbackQueryAsync(_botClient, context, callbackQuery, cancellationToken);
                            }
                        }
                        break;
                    }

                case "complete":
                    {
                        if (parts.Length == 2 && Guid.TryParse(parts[1], out var taskId))
                        {
                            await _toDoService.MarkCompletedAsync(taskId, cancellationToken);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                "Задача завершена.",
                                cancellationToken: cancellationToken);

                            await _botClient.EditMessageText(
                                callbackQuery.Message!.Chat.Id,
                                callbackQuery.Message.MessageId,
                                "Задача отмечена как выполненная.",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                "Некорректные данные задачи.",
                                cancellationToken: cancellationToken);
                        }
                        break;
                    }
                case "delete":
                    {
                        if (parts.Length == 2 && Guid.TryParse(parts[1], out var taskId))
                        {
                            _toDoService.Delete(taskId, cancellationToken);

                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                "Задача удалена.",
                                cancellationToken: cancellationToken);

                            await _botClient.EditMessageText(
                                callbackQuery.Message!.Chat.Id,
                                callbackQuery.Message.MessageId,
                                "Задача удалена.",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _botClient.AnswerCallbackQuery(
                                callbackQuery.Id,
                                "Некорректные данные задачи.",
                                cancellationToken: cancellationToken);
                        }
                        break;
                    }

                default:
                    {
                        await _botClient.AnswerCallbackQuery(
                            callbackQuery.Id,
                            "Неизвестное действие",
                            cancellationToken: cancellationToken);
                        break;
                    }
            }
        }
        private async Task HandleShowAction(CallbackQuery callbackQuery, ToDoListCallbackDto dto, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, cancellationToken);
            if (dto == null || user == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка обработки данных.", cancellationToken: cancellationToken);
                return;
            }

            var tasks = dto.ToDoListId.HasValue
                ? await _toDoService.GetByUserIdAndList(user.UserId, dto.ToDoListId.Value, cancellationToken)
                : await _toDoService.GetActiveByUserIdAsync(user.UserId, cancellationToken);

            var response = tasks.Any()
                ? string.Join("\n", tasks.Select(t => $"{t.Name} ({t.Deadline:d})"))
                : "Задачи не найдены.";

            string listName;
            if (dto.ToDoListId.HasValue)
            {
                var list = await _toDoListService.Get(dto.ToDoListId.Value, cancellationToken);
                listName = list != null ? $"списка \"{list.Name}\"" : "списка";
            }
            else
            {
                listName = "без списка";
            }
            await _botClient.EditMessageText(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId,$"Задачи из {listName}:\n{response}", cancellationToken: cancellationToken);
        }
    }
}