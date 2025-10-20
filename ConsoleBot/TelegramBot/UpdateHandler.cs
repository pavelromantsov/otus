using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;
using ConsoleBot.Core.Services;
using ConsoleBotCommands;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;


namespace ConsoleBot.TelegramBot
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUserService _userService;
        private readonly IToDoService _toDoService;
        private readonly IToDoReportService _toDoReportService;

        public UpdateHandler(ITelegramBotClient botClient, IUserService userService, IToDoService toDoService, IToDoReportService toDoReportService)
        {
            _botClient = botClient;
            _userService = userService;
            _toDoService = toDoService;
            _toDoReportService = toDoReportService;

        }
        public void HandleUpdateAsync(ITelegramBotClient botClient, Update update)
        {
            try
            {
                var message = update.Message;
                if (message == null)
                    return;

                var chatId = message.Chat.Id;
                var command = message.Text.Split(' ').First();

                var user = _userService.GetUser(message.From.Id);
                if (user == null)
                {
                    user = _userService.RegisterUser(message.From.Id, message.From.Username);
                }
                switch (command)
                {
                    case "/start":
                        StartCommand(_botClient, update, chatId, user);
                        break;
                    case "/addtask":
                        AddTaskCommand(_botClient, update, chatId, user, message.Text);
                        break;
                    case "/showtasks":
                        ShowTasksCommand(_botClient, update, chatId, user);
                        break;
                    case "/completetask":
                        CompleteTaskCommand(_botClient, update, chatId, user, message.Text);
                        break;
                    case "/showalltasks":
                        ShowAllTasksCommand(_botClient, update, chatId, user, message.Text);
                        break;
                    case "/removetask":
                        RemoveTaskCommand(_botClient, update, chatId, user, message.Text);
                        break;
                    case "/report":
                        ReportCommand(_botClient, update, chatId, user, _toDoReportService);
                        break;
                    case "/find":
                        FindCommand(_botClient, update, chatId, user );
                        break;
                    case "/help":
                        HelpCommand(_botClient, update, chatId, user);
                        break;
                    case "/info":
                        InfoCommand(_botClient, update, chatId, user);
                        break;
                    default:
                        botClient.SendMessage(update.Message.Chat, "Неизвестная команда");
                        break;
                }
            }
            catch (Exception ex)
            {
                botClient.SendMessage(update.Message.Chat, $"Произошла ошибка: {ex.Message}");
            }
        }

        private void StartCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user)
        {

            botClient.SendMessage(update.Message.Chat, $"Привет, {user.TelegramUserName}! Я твой помощник по управлению задачами.");
            botClient.SendMessage(update.Message.Chat, "Добро пожаловать в консольное приложение, имитирующее работу " +
                                  "Телеграмм-бота!" + "\n\nСписок команд:");
            botClient.SendMessage(update.Message.Chat, "/start - начать работу");
            botClient.SendMessage(update.Message.Chat, "/addtask - добавить задачу в список");
            botClient.SendMessage(update.Message.Chat, "/showtasks - отображение списка активных задач");
            botClient.SendMessage(update.Message.Chat, "/completetask - отметить задачу как завершенную");
            botClient.SendMessage(update.Message.Chat, "/showalltasks - показать все задачи");
            botClient.SendMessage(update.Message.Chat, "/removetask - удалить задачу из списка");
            botClient.SendMessage(update.Message.Chat, "/report - отображение всех задач пользователя");
            botClient.SendMessage(update.Message.Chat, "/find - поиск задачи");
            botClient.SendMessage(update.Message.Chat, "/help - справка по использованию");
            botClient.SendMessage(update.Message.Chat, "/info - информация о программе");
            botClient.SendMessage(update.Message.Chat, "/exit - завершение работы");
        }

        private void AddTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message)
        {

            var taskName = update.Message.Text;
            if (taskName.Length < 2)
            {
                botClient.SendMessage(update.Message.Chat, "Формат команды: /addtask <название>");
                return;
            }

            //проверка на максимальную длину задачи

            taskName = string.Join("", taskName.Skip(9));
            var taskLength = taskName.Length.ToString();
            if (taskName.Length > _toDoService.ParseAndValidateInt(taskLength, 1, 100))
            {
                throw new TaskLengthLimitException(1, 100);
            }

            //проверка на максимальное количество задач
            var tasks = message.Split(' ');
            if (int.TryParse(tasks[1], out var taskIndex))
            {
                var allTasks = _toDoService.GetAllByUserId(user.UserId);
                var _task = allTasks.ElementAt(taskIndex - 1);

                if (allTasks.Count >= 100)
                {
                    throw new TaskCountLimitException(100);
                }
            }
            // Проверка на дубликаты
            if (_toDoService.GetAllByUserId(user.UserId).Any(t => t.Name == taskName))
            {
                botClient.SendMessage(update.Message.Chat, $"Задача с названием '{taskName}' уже существует.");
                return;
            }

            var task = _toDoService.Add(user, taskName);
            botClient.SendMessage(update.Message.Chat, $"Задача '{task.Name}' добавлена.");
        }

        public void ShowTasksCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user)
        {
            var tasks = _toDoService.GetAllByUserId(user.UserId);
            var output = string.Join("\n", tasks.Select(t => $"{t.Name} - {t.CreatedAt}"));
            botClient.SendMessage(update.Message.Chat, output);
        }

        public void CompleteTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message)
        {
            var parts = message.Split(' ');
            if (int.TryParse(parts[1], out var taskIndex))
            {
                var allTasks = _toDoService.GetAllByUserId(user.UserId);
                var task = allTasks.ElementAt(taskIndex - 1);
                _toDoService.MarkCompleted(task.Id);
                botClient.SendMessage(update.Message.Chat, $"Задача с ID {taskIndex} отмечена как выполненная.");
            }
            else
            {
                botClient.SendMessage(update.Message.Chat, "Некорректный номер задачи.");
            }
        }
        public void ShowAllTasksCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string command)
        {
            var allTasks = _toDoService.GetAllByUserId(user.UserId);
            if (allTasks.Any())
            {
                var output = string.Join(Environment.NewLine, allTasks.Select((t, i) =>
                    $"{i + 1}. {t.Name} (создана {t.CreatedAt}, {(t.State == ToDoItemState.Active ? "активна" : "завершена")})"));
                botClient.SendMessage(update.Message.Chat, $"Все задачи:\n{output}");
            }
            else
            {
                botClient.SendMessage(update.Message.Chat, "Задач нет.");
            }
        }
        private void RemoveTaskCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, string message)
        {
            var parts = message.Split(' ');
            if (parts.Length < 2 || !int.TryParse(parts[1], out var taskIndex))
            {
                botClient.SendMessage(update.Message.Chat, "Формат команды: /removetask <номер задачи>");
                return;
            }

            var allTasks = _toDoService.GetAllByUserId(user.UserId);
            if (taskIndex > 0 && taskIndex <= allTasks.Count)
            {
                var task = allTasks.ElementAt(taskIndex - 1);
                _toDoService.Delete(task.Id);
                botClient.SendMessage(update.Message.Chat, $"Задача #{taskIndex} удалена.");
            }
            else
            {
                botClient.SendMessage(update.Message.Chat, "Задача с таким номером не найдена.");
            }
        }
        private void HelpCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user)
        {
            botClient.SendMessage(update.Message.Chat, "Доступные команды:\n" +
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

        private void InfoCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user)
        {
            botClient.SendMessage(update.Message.Chat, $"Консольная версия бота для управления задачами, версия {Program.version}." +
                $"\nСоздано {Program.created_date}, обновлено {Program.updated_date}." +
                $"\nНовые функции: {Program.whatsNew_text}.");
        }

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _toDoService.GetAllByUserId(userId);
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _toDoService.GetActiveByUserId(userId);
        }

        public ToDoItem Add(ToDoUser user, string name)
        {
            return _toDoService.Add(user, name);
        }

        public void MarkCompleted(Guid id)
        {
            _toDoService.MarkCompleted(id);
        }

        public void Delete(Guid id)
        {
            _toDoService.Delete(id);
        }

        public int ParseAndValidateInt(string? str, int min, int max)
        {
            return _toDoService.ParseAndValidateInt(str, min, max);
        }

        public void ValidateString(string? str)
        {
            _toDoService.ValidateString(str);
        }

        private void ReportCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user, IToDoReportService toDoReportService)
        {
            var stats = _toDoReportService.GetUserStats(user.UserId);

            var reportMessage =
                $"Статистика по задачам на {stats.generatedAt:yyyy-MM-dd HH:mm:ss}:\n" +
                $"Всего: {stats.total};\n" +
                $"Завершенных: {stats.completed};\n" +
                $"Активных: {stats.active};";

            botClient.SendMessage(update.Message.Chat, reportMessage);
        }
        private void FindCommand(ITelegramBotClient botClient, Update update, long chat, ToDoUser user)
        {
            var parts = update.Message.Text.Split(' ');
            if (parts.Length < 2)
            {
                botClient.SendMessage(update.Message.Chat, "Формат команды: /find <префикс>");
                return;
            }

            var namePrefix = parts[1];
            var foundTasks = _toDoService.Find(user, namePrefix);

            if (foundTasks.Any())
            {
                var output = string.Join("\n", foundTasks.Select((task, idx) =>
                    $"{idx + 1}. {task.Name} - создана {task.CreatedAt}"));
                botClient.SendMessage(update.Message.Chat, $"Найденные задачи:\n{output}");
            }
            else
            {
                botClient.SendMessage(update.Message.Chat, "Задачи не найдены.");
            }
        }

    }
}