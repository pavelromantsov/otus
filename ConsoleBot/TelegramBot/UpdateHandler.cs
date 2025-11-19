using System.Collections.Generic;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.Scenarios;
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

        public delegate void MessageEventHandler(string message);
        public event MessageEventHandler? OnHandleUpdateStarted;
        public event MessageEventHandler? OnHandleUpdateCompleted;

        private readonly IEnumerable<IScenario> _scenarios;
        private readonly IScenarioContextRepository _contextRepository;
        private readonly DateTime deadline;
        public UpdateHandler(ITelegramBotClient botClient, IUserService userService, IToDoService toDoService, IToDoReportService toDoReportService, IEnumerable<IScenario> scenarios, IScenarioContextRepository contextRepository)
        {
            _botClient = botClient;
            _userService = userService;
            _toDoService = toDoService;
            _toDoReportService = toDoReportService;
            _contextRepository = contextRepository;
            _scenarios = scenarios.ToList();
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            OnHandleUpdateStarted?.Invoke(update.Message.Text);

            try
            {
                // Обработка обновления
                await Task.Delay(1000); // Задержка для симуляции длительной обработки
            }
            finally
            {
                OnHandleUpdateCompleted?.Invoke(update.Message.Text);
            }

            try
            {
                var message = update.Message;
                if (message == null)
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
                var keyboard = new ReplyKeyboardMarkup("/addtask", "/showtasks", "/showalltasks", "/report");
                var command = message.Text.Split(' ').First();

                var context = await _contextRepository.GetContext(userId, cancellationToken);


                if (context != null && message.Text == "/cancel")
                {
                    await _contextRepository.ResetContext(userId, cancellationToken);
                    await botClient.SendMessage(chatId, "Сценарий остановлен.", cancellationToken: cancellationToken);
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
                        var addTaskScenario = new AddTaskScenario(_userService, _toDoService, _contextRepository);
                        await _contextRepository.SetContext(userId, context, cancellationToken);
                        await ProcessScenario(context, message, cancellationToken);
                        break;
                    case "/showtasks":
                        await ShowTasksCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/completetask":
                        await CompleteTaskCommand(_botClient, update, chatId, user, message.Text, cancellationToken);
                        break;
                    case "/showalltasks":
                        await ShowAllTasksCommand(_botClient, update, chatId, user, message.Text, cancellationToken);
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
                        await botClient.SendMessage(update.Message.Chat, "Неизвестная команда");
                        break;
                }
            }
            catch (Exception ex)
            {
                botClient.SendMessage(update.Message.Chat, $"Произошла ошибка: {ex.Message}");
            }
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
            await botClient.SendMessage(update.Message.Chat, "/showtasks - отображение списка активных задач");
            await botClient.SendMessage(update.Message.Chat, "/completetask - отметить задачу как завершенную");
            await botClient.SendMessage(update.Message.Chat, "/showalltasks - показать все задачи");
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
        public async Task ProcessScenario(ScenarioContext context, Telegram.Bot.Types.Message message, CancellationToken cancellationToken)
        {
            // Найдем сценарий, соответствующий текущему типу сценария 
            var scenario = GetScenario(context.CurrentScenario, context);
            var result = await scenario.HandleMessageAsync(_botClient, context, message, deadline, cancellationToken);

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
        public async Task ShowTasksCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId, cancellationToken);
            if (tasks.Count > 0)
            {
                var output = string.Join("\n", tasks.Select(t => $"Задача `{t.Id}`: {t.Name} " +
                $"- создана {t.CreatedAt:g}" +
                $" - крайний срок: {t.Deadline:d}"));
                await botClient.SendMessage(update.Message.Chat, output);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Активных задач нет");
            }
        }
        public async Task CompleteTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {
            var parts = message.Split(' ');
            if (int.TryParse(parts[1], out var taskIndex))
            {
                var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
                var task = allTasks.ElementAt(taskIndex - 1);
                await _toDoService.MarkCompletedAsync(task.Id, cancellationToken);
                await botClient.SendMessage(update.Message.Chat, $"Задача с номером '{taskIndex}' отмечена как выполненная.");
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Некорректный номер задачи.");
            }
        }
        public async Task ShowAllTasksCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string command, CancellationToken cancellationToken)
        {
            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (allTasks.Any())
            {
                var output = string.Join(Environment.NewLine, allTasks.Select((t, i) =>
                    $"{i + 1}. {t.Name} (создана {t.CreatedAt}, {(t.State == ToDoItemState.Active ? "активна" : "завершена")})"));
                await botClient.SendMessage(update.Message.Chat, $"Все задачи:\n{output}");
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Задач нет.");
            }
        }
        private async Task RemoveTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {
            var parts = message.Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[1], out var taskIndex))
            {
                await botClient.SendMessage(update.Message.Chat, "Формат команды: /removetask <номер задачи>");
                return;
            }

            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (taskIndex > 0 && taskIndex <= allTasks.Count)
            {
                var task = allTasks.ElementAt(taskIndex - 1);
                _toDoService.Delete(task.Id, cancellationToken);
                await botClient.SendMessage(update.Message.Chat, $"Задача #{taskIndex} удалена.");
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Задача с таким номером не найдена.");
            }
        }
        private async Task HelpCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(update.Message.Chat, "Доступные команды:\n" +
               "/start - начало работы\n" +
               "/addtask - добавить задачу\n" +
               "/showtasks - показать активные задачи\n" +
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

        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, CancellationToken cancellationToken)
        {
            return await _toDoService.AddAsync(user, name, deadline, cancellationToken);
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
            ["/addtask", "/showalltasks", "/showtasks", "/report"] :
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
            var defaultKeyboard = new ReplyKeyboardMarkup("/addtask", "/showtasks", "/showalltasks", "/report");
            await _botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: defaultKeyboard, cancellationToken: cancellationToken);
        }

        private async Task SendCancelKeyboard(long chatId, CancellationToken cancellationToken)
        {
            var defaultKeyboard = new ReplyKeyboardMarkup("/cancel");
            await _botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: defaultKeyboard, cancellationToken: cancellationToken);
        }
    }
}