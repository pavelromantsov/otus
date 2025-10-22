using System.Runtime.CompilerServices;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;
using ConsoleBot.Core.Services;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using System.Linq;



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

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            await Task.CompletedTask; // Чтобы удовлетворить контракт асинхронного метода
        }
        public UpdateHandler(ITelegramBotClient botClient, IUserService userService, IToDoService toDoService, IToDoReportService toDoReportService)
        {
            _botClient = botClient;
            _userService = userService;
            _toDoService = toDoService;
            _toDoReportService = toDoReportService;

            OnHandleUpdateStarted += (message) => Console.WriteLine($"Началась обработка сообщения '{message}'");
            OnHandleUpdateCompleted += (message) => Console.WriteLine($"Закончилась обработка сообщения '{message}'");
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
                var command = message.Text.Split(' ').First();

                var user = await _userService.GetUserAsync(message.From.Id, cancellationToken);
                if (user == null)
                {
                   await _userService.RegisterUserAsync(message.From.Id, message.From.Username, cancellationToken);
                   user = await _userService.GetUserAsync(message.From.Id, cancellationToken);
                }
                switch (command)
                {
                    case "/start":
                        await StartCommand(_botClient, update, chatId, user, cancellationToken);
                        break;
                    case "/addtask":
                        await AddTaskCommand(_botClient, update, chatId, user, message.Text, cancellationToken);
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
                        await botClient.SendMessage(update.Message.Chat, "Неизвестная команда", cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                botClient.SendMessage(update.Message.Chat, $"Произошла ошибка: {ex.Message}", cancellationToken);
            }
        }

        private async Task StartCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {

            botClient.SendMessage(update.Message.Chat, $"Привет, {user.TelegramUserName}! Я твой помощник по управлению задачами.", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "Добро пожаловать в консольное приложение, имитирующее работу " +
                                  "Телеграмм-бота!" + "\n\nСписок команд:", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/start - начать работу", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/addtask - добавить задачу в список", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/showtasks - отображение списка активных задач", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/completetask - отметить задачу как завершенную", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/showalltasks - показать все задачи", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/removetask - удалить задачу из списка", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/report - отображение всех задач пользователя", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/find - поиск задачи", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/help - справка по использованию", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/info - информация о программе", cancellationToken);
            botClient.SendMessage(update.Message.Chat, "/exit - завершение работы", cancellationToken);
        }

        private async Task AddTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {

            var taskName = update.Message.Text;
            if (taskName.Length < 2)
            {
                await botClient.SendMessage(update.Message.Chat, "Формат команды: /addtask <название>", cancellationToken);
                return;
            }

            //проверка на максимальную длину задачи

            taskName = string.Join("", taskName.Skip(9));
            var taskLength = taskName.Length.ToString();
            if (taskName.Length > _toDoService.ParseAndValidateInt(taskLength, 1, 100, cancellationToken))
            {
                throw new TaskLengthLimitException(1, 100);
            }

            //проверка на максимальное количество задач
            var tasks = message.Split(' ');
            if (int.TryParse(tasks[1], out var taskIndex))
            {
                var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
                var _task = allTasks.ElementAt(taskIndex - 1);

                if (allTasks.Count >= 100)
                {
                    throw new TaskCountLimitException(100);
                }
            }
            // Проверка на дубликаты
            if ((await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken)).Any(t => t.Name == taskName))
            {
                await botClient.SendMessage(update.Message.Chat, $"Задача с названием '{taskName}' уже существует.", cancellationToken);
                return;
            }

            var task = await _toDoService.AddAsync(user, taskName, cancellationToken);
            await botClient.SendMessage(update.Message.Chat, $"Задача '{task.Name}' добавлена.", cancellationToken);
        }

        public async Task ShowTasksCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var tasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            var output = string.Join("\n", tasks.Select(t => $"{t.Name} - {t.CreatedAt}"));
            await botClient.SendMessage(update.Message.Chat, output, cancellationToken);
        }

        public async Task CompleteTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {
            var parts = message.Split(' ');
            if (int.TryParse(parts[1], out var taskIndex))
            {
                var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
                var task = allTasks.ElementAt(taskIndex - 1);
                await _toDoService.MarkCompletedAsync(task.Id, cancellationToken);
                await botClient.SendMessage(update.Message.Chat, $"Задача с ID {taskIndex} отмечена как выполненная.", cancellationToken);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Некорректный номер задачи.", cancellationToken);
            }
        }
        public async Task ShowAllTasksCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string command, CancellationToken cancellationToken)
        {
            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (allTasks.Any())
            {
                var output = string.Join(Environment.NewLine, allTasks.Select((t, i) =>
                    $"{i + 1}. {t.Name} (создана {t.CreatedAt}, {(t.State == ToDoItemState.Active ? "активна" : "завершена")})"));
                await botClient.SendMessage(update.Message.Chat, $"Все задачи:\n{output}", cancellationToken);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Задач нет.", cancellationToken);
            }
        }
        private async Task RemoveTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message, CancellationToken cancellationToken)
        {
            var parts = message.Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[1], out var taskIndex))
            {
                await botClient.SendMessage(update.Message.Chat, "Формат команды: /removetask <номер задачи>", cancellationToken);
                return;
            }

            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId, cancellationToken);
            if (taskIndex > 0 && taskIndex <= allTasks.Count)
            {
                var task = allTasks.ElementAt(taskIndex - 1);
                _toDoService.Delete(task.Id);
                await botClient.SendMessage(update.Message.Chat, $"Задача #{taskIndex} удалена.", cancellationToken);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Задача с таким номером не найдена.", cancellationToken);
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
               "/info - информация о программе", cancellationToken);
        }

        private async Task InfoCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(update.Message.Chat, $"Консольная версия бота для управления задачами, версия {Program.version}." +
                $"\nСоздано {Program.created_date}, обновлено {Program.updated_date}." +
                $"\nНовые функции: {Program.whatsNew_text}.", cancellationToken);
        }

        public async Task <IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _toDoService.GetAllByUserIdAsync(userId, cancellationToken);
        }

        public async Task <IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _toDoService.GetActiveByUserIdAsync(userId, cancellationToken);
        }

        public async Task <ToDoItem> AddAsync(ToDoUser user, string name, CancellationToken cancellationToken)
        {
            return await _toDoService.AddAsync(user, name, cancellationToken);
        }   

        public async Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken)
        {
            await _toDoService.MarkCompletedAsync(id, cancellationToken);
        }

        public void Delete(Guid id, CancellationToken cancellationToken)
        {
            _toDoService.Delete(id);
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

            await botClient.SendMessage(update.Message.Chat, reportMessage, cancellationToken);
        }
        private async Task FindCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, CancellationToken cancellationToken)
        {
            var parts = update.Message.Text.Split(' ');
            if (parts.Length < 2)
            {
                await botClient.SendMessage(update.Message.Chat, "Формат команды: /find <префикс>", cancellationToken);
                return;
            }

            var namePrefix = parts[1];
            var foundTasks = await _toDoService.FindAsync(user, namePrefix, cancellationToken);

            if (foundTasks.Any())
            {
                var output = string.Join("\n", foundTasks.Select((task, idx) =>
                    $"{idx + 1}. {task.Name} - создана {task.CreatedAt}"));
                await botClient.SendMessage(update.Message.Chat, $"Найденные задачи:\n{output}", cancellationToken);
            }
            else
            {
                await botClient.SendMessage(update.Message.Chat, "Задачи не найдены.", cancellationToken);
            }
        }

    }
}