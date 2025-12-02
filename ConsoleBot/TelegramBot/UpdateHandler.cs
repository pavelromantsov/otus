using System.Threading;
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

        public delegate void MessageEventHandler(string message);
        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;

        private readonly IEnumerable<IScenario> _scenarios;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly ToDoList? list;

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

            try
            {
                //OnHandleUpdateStarted?.Invoke(update.Message.Text);
                //OnHandleUpdateStarted?.Invoke(update.Message.Text ?? update.CallbackQuery?.Data ?? "Unknown");
                var text = update.Message?.Text ?? update.CallbackQuery?.Data ?? "Unknown";
                OnHandleUpdateStarted?.Invoke(text);

                await (update switch
                {
                    { Message: { } message } => OnMessage(update, message, cancellationToken),
                    { CallbackQuery: { } callbackQuery } => OnCallbackQuery(update, callbackQuery, cancellationToken),
                    _ => OnUnknown(update)
                });
            }
            //finally
            //{
            //    OnHandleUpdateCompleted?.Invoke(update.Message.Text ?? update.CallbackQuery?.Data ?? "Unknown");
            //}
            finally
{
                string text = update.Message != null ? update.Message.Text :
                             update.CallbackQuery != null ? update.CallbackQuery.Data :
                             "Unknown";

                OnHandleUpdateCompleted?.Invoke(text);
            }
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
                var keyboard = new ReplyKeyboardMarkup("/addtask", "/show", "/showalltasks", "/report");
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
                        await CompleteTaskCommand(_botClient, update, chatId, user, message.Text, cancellationToken);
                        break;
                    case "/removetask":
                        await RemoveTaskCommand(_botClient, update, chatId, user, message.Text, cancellationToken);
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
            //await botClient.SendMessage(update.Message.Chat, "/showalltasks - показать все задачи");
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
            var result = await scenario.HandleMessageAsync(_botClient, context, message, list, cancellationToken);

            // Если сценарий завершился, сбросим контекст
            if (result == ScenarioResult.Completed)
            {
                await _contextRepository.ResetContext(message.From.Id, cancellationToken);
                await SendDefaultKeyboard(message.Chat.Id, cancellationToken);
            }
            else
            {
                await _contextRepository.SetContext(message.From.Id, context, cancellationToken);
            }
            // Отправляем кнопку /cancel при активном сценарии
            if (context.CurrentScenario != ScenarioType.None)
            {
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
        new[] { InlineKeyboardButton.WithCallbackData("📌 Без списка", ToDoListCallbackDto.Create("show", null)) }
    };

    // Кнопки для каждого списка
    foreach (var list in lists)
    {
        keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData(list.Name, ToDoListCallbackDto.Create("show", list.Id)) });
    }

    // Кнопка "Добавить"
    keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData("🆕 Добавить", "AddList") });

    // Кнопка "Удалить"
    keyboardRows.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ Удалить", "DeleteList") });

    var inlineKeyboard = new InlineKeyboardMarkup(keyboardRows);
    await botClient.SendMessage(chat, "Выберите список:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
}
            //{
        //    var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

        //    if (lists.Count == 0)
        //    {
        //        await botClient.SendMessage(update.Message.Chat.Id, "У вас нет созданных списков задач.", cancellationToken: cancellationToken);
        //        //return;
                
        //    }

        //    //var inlineKeyboard = new InlineKeyboardMarkup(lists.Select(list => InlineKeyboardButton.WithCallbackData(list.Name, new ToDoListCallbackDto("show", null).ToString())));
        //    //await botClient.SendMessage(update.Message.Chat.Id, "Выберите список:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        
        
        //    // Получаем все списки пользователя
        //   // var lists = await _toDoListService.GetUserLists(user.UserId, cancellationToken);

        //    // Создаем клавиатуру с кнопками
        //    var keyboardRows = new List<IEnumerable<InlineKeyboardButton>>();

        //    // Кнопка "Без списка"
        //    var noListButton = InlineKeyboardButton.WithCallbackData("📌 Без списка", new ToDoListCallbackDto("show", null).ToString());
        //    //var noListButton = InlineKeyboardButton.WithCallbackData("📌 Без списка", ToDoListCallbackDto.Create("show", null));
        //    keyboardRows.Add(new[] { noListButton });

        //    // Кнопки для каждого списка
        //    foreach (var list in lists)
        //    {
        //        var listButton = InlineKeyboardButton.WithCallbackData(list.Name, ToDoListCallbackDto.Create("show", list.Id));
        //        keyboardRows.Add(new[] { listButton });
        //    }

        //    // Кнопка "Добавить"
        //    var addButton = InlineKeyboardButton.WithCallbackData("🆕 Добавить", "AddList");
        //    keyboardRows.Add(new[] { addButton });

        //    // Кнопка "Удалить"
        //    var deleteButton = InlineKeyboardButton.WithCallbackData("❌ Удалить", "DeleteList");
        //    keyboardRows.Add(new[] { deleteButton });

        //    // Формируем разметку клавиатуры
        //    var inlineKeyboard = new InlineKeyboardMarkup(keyboardRows);

        //    // Отправляем сообщение с клавиатурой
        //    await botClient.SendMessage(update.Message.Chat.Id, "Выберите список:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        //}

        public async Task CompleteTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {
            var parts = message.Split(' ', 2);
            if (parts.Length < 2 || !int.TryParse(parts[1], out var taskIndex))
            {
                await botClient.SendMessage(chat, "Некорректный номер задачи.", cancellationToken: cancellationToken);
                return;
            }

            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (taskIndex < 1 || taskIndex > allTasks.Count)
            {
                await botClient.SendMessage(chat, "Задача с таким номером не найдена.", cancellationToken: cancellationToken);
                return;
            }

            var task = allTasks[taskIndex - 1];
            await _toDoService.MarkCompletedAsync(task.Id, cancellationToken);
            await botClient.SendMessage(chat, $"Задача с номером '{taskIndex}' отмечена как выполненная.", cancellationToken: cancellationToken);
        }
        //{
        //    var parts = message.Split(' ');
        //    if (int.TryParse(parts[1], out var taskIndex))
        //    {
        //        var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
        //        var task = allTasks.ElementAt(taskIndex - 1);
        //        await _toDoService.MarkCompletedAsync(task.Id, cancellationToken);
        //        await botClient.SendMessage(update.Message.Chat, $"Задача с номером '{taskIndex}' отмечена как выполненная.");
        //    }
        //    else
        //    {
        //        await botClient.SendMessage(update.Message.Chat, "Некорректный номер задачи.");
        //    }
        //}

        private async Task RemoveTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {
            var parts = message.Split(' ', 2);
            if (parts.Length < 2 || !int.TryParse(parts[1], out var taskIndex))
            {
                await botClient.SendMessage(chat, "Некорректный номер задачи.", cancellationToken: cancellationToken);
                return;
            }

            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (taskIndex < 1 || taskIndex > allTasks.Count)
            {
                await botClient.SendMessage(chat, "Задача с таким номером не найдена.", cancellationToken: cancellationToken);
                return;
            }

            var task = allTasks[taskIndex - 1];
            _toDoService.Delete(task.Id, cancellationToken);
            await botClient.SendMessage(chat, $"Задача #{taskIndex} удалена.", cancellationToken: cancellationToken);
        }
        //{
        //    var parts = message.Split(' ');
        //    if (parts.Length < 2 || !int.TryParse(parts[1], out var taskIndex))
        //    {
        //        await botClient.SendMessage(update.Message.Chat, "Формат команды: /removetask <номер задачи>");
        //        return;
        //    }

        //    var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
        //    if (taskIndex > 0 && taskIndex <= allTasks.Count)
        //    {
        //        var task = allTasks.ElementAt(taskIndex - 1);
        //        _toDoService.Delete(task.Id, cancellationToken);
        //        await botClient.SendMessage(update.Message.Chat, $"Задача #{taskIndex} удалена.");
        //    }
        //    else
        //    {
        //        await botClient.SendMessage(update.Message.Chat, "Задача с таким номером не найдена.");
        //    }
        //}
        private async Task HelpCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(update.Message.Chat, "Доступные команды:\n" +
               "/start - начало работы\n" +
               "/addtask - добавить задачу\n" +
               "/show - показать активные задачи\n" +
               "/completetask - отметить задачу как выполненную\n" +
               "/showalltasks - показать все задачи\n" +
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
        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _toDoService.GetAllByUserIdAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _toDoService.GetActiveByUserIdAsync(userId, cancellationToken);
        }

        public async Task MarkCompletedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            await _toDoService.MarkCompletedAsync(id, cancellationToken);
        }

        public void Delete(Guid id, CancellationToken cancellationToken)
        {
            _toDoService.Delete(id, cancellationToken);
        }

        public int ParseAndValidateInt(string? str, int min, int max, CancellationToken cancellationToken)
        {
            return _toDoService.ParseAndValidateInt(str, min, max, cancellationToken);
        }

        public async Task ValidateStringAsync(string? str, CancellationToken cancellationToken)
        {
            await _toDoService.ValidateStringAsync(str, cancellationToken);
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
            ["/addtask", "/showalltasks", "/show", "/report"] :
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
            var defaultKeyboard = new ReplyKeyboardMarkup("/addtask", "/show", "/showalltasks", "/report");
            await _botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: defaultKeyboard, cancellationToken: cancellationToken);
        }

        private async Task SendCancelKeyboard(long chatId, CancellationToken cancellationToken)
        {
            var defaultKeyboard = new ReplyKeyboardMarkup("/cancel");
            await _botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: defaultKeyboard, cancellationToken: cancellationToken);
        }

        private async Task OnCallbackQuery(Update update,CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            // Проверяем регистрацию пользователя
            var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, cancellationToken);
            if (user == null)
            {
                await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Незарегистрированный пользователь", cancellationToken: cancellationToken);
                return;
            }

            // Парсим данные из callback-запроса
            var dto = CallbackDto.FromString(callbackQuery.Data);

            // Обрабатываем действие
            switch (dto.Action)
            {
                case "show":
                    await HandleShowAction(callbackQuery, dto as ToDoListCallbackDto, cancellationToken);
                    break;
                case "AddList": // Когда нажата кнопка "🆕Добавить"
                    var addListContext = new ScenarioContext(ScenarioType.AddList);
                    await _contextRepository.SetContext(callbackQuery.From.Id, addListContext, cancellationToken);
                    await ProcessScenario(addListContext, callbackQuery.Message, cancellationToken);
                    break;
                case "DeleteList": // Когда нажата кнопка "❌Удалить"
                    var deleteListContext = new ScenarioContext(ScenarioType.DeleteList);
                    await _contextRepository.SetContext(callbackQuery.From.Id, deleteListContext, cancellationToken);
                    await ProcessScenario(deleteListContext, callbackQuery.Message, cancellationToken);
                    break;
                    
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

            var listName = dto.ToDoListId.HasValue ? $"списка {dto.ToDoListId.Value}" : "без списка";

            await _botClient.EditMessageText(
                callbackQuery.Message.Chat.Id,
                callbackQuery.Message.MessageId,
                $"Задачи из {listName}:\n{response}",
                cancellationToken: cancellationToken
            );
        }


        //{
        //    var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, cancellationToken);
        //    if (dto == null || user == null)
        //    {
        //        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка обработки данных.", cancellationToken: cancellationToken);
        //        return;
        //    }

        //    var tasks = dto.ToDoListId.HasValue
        //        ? await _toDoService.GetByUserIdAndList(user.UserId, dto.ToDoListId.Value, cancellationToken)
        //        : await _toDoService.GetActiveByUserIdAsync(user.UserId, cancellationToken); // Без списка

        //    var response = tasks.Any()
        //        ? string.Join("\n", tasks.Select(t => $"{t.Name} ({t.Deadline:d})"))
        //        : "Задачи не найдены.";

        //    var listName = dto.ToDoListId.HasValue ? $"списка {dto.ToDoListId.Value}" : "без списка";

        //    await _botClient.EditMessageText(
        //        callbackQuery.Message.Chat.Id,
        //        callbackQuery.Message.MessageId,
        //        $"Задачи из {listName}:\n{response}",
        //        cancellationToken: cancellationToken
        //    );
        //}
        //{
        //    // Получаем пользователя по Telegram идентификатору
        //    var user = await _userService.GetUserByTelegramUserIdAsync(callbackQuery.From.Id, cancellationToken);

        //    // Проверяем наличие необходимых данных
        //    if (dto == null || user == null)
        //    {
        //        await _botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка обработки данных.", cancellationToken: cancellationToken);
        //        return;
        //    }

        //    // Получаем задачи, привязанные к выбранному списку
        //    var tasks = await _toDoService.GetByUserIdAndList(user.UserId, dto.ToDoListId, cancellationToken);

        //    // Готовим ответ
        //    var response = string.Join("\n", tasks.Select(t => $"{t.Name} ({t.Deadline:d})"));

        //    // Редактируем предыдущее сообщение с результатами
        //    await _botClient.EditMessageText(
        //        callbackQuery.Message.Chat.Id,
        //        callbackQuery.Message.MessageId,
        //        $"Задачи из списка {dto.ToDoListId.Value}:\n{response}",
        //        cancellationToken: cancellationToken
        //    );
        //}
    }
}