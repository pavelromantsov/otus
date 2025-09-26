using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ConsoleBotCommands;

namespace ConsoleBotApp
{
    class Program
    {
        private static ToDoUser currentUser = null;
        private const string version = "1.3";
        private const string created_date = "20-08-2025";
        private const string updated_date = "17-09-2025";
        private const string whatsNew_text = "добавлена обработка исключений";
        private static List<ToDoItem> tasks = new List<ToDoItem>();

        static void Main()
        {   
            bool run = true;
            bool success = false;
            while (!success && run)
            try
            {
                //Ввод ограничения на количество задач
                Console.Write("Введите максимально допустимое количество задач (от 1 до 100): ");
                int maxTaskCount = TaskValidate.ParseAndValidateInt(Console.ReadLine(), 1, 100);

                //Ввод ограничения на длину задачи
                Console.Write("Введите максимально допустимую длину задачи (от 1 до 100): ");
                int maxTaskLength = TaskValidate.ParseAndValidateInt(Console.ReadLine(), 1, 100);
                
                Console.WriteLine("Добро пожаловать в консольное приложение, имитирующее работу " +
                    "Телеграмм-бота!" + "\n\nСписок команд:");
                Console.WriteLine("/start - начать работу");
                Console.WriteLine("/addtask - добавить задачу в список");
                Console.WriteLine("/showtasks - отображение списка активных задач");
                Console.WriteLine("/completetask - отметить задачу как завершенную");
                Console.WriteLine("/showalltasks - показать все задачи");
                Console.WriteLine("/removetask - удалить задачу из списка");
                Console.WriteLine("/help - справка по использованию");
                Console.WriteLine("/info - информация о программе");
                Console.WriteLine("/exit - завершение работы");
                
                while (run)
                {
                    Console.Write("\nВведите команду: ");
                    string input = Console.ReadLine();

                    switch (input.ToLower())
                    {
                        case "/start":
                            StartCommand();
                            break;

                        case "/addtask":
                            AddTaskCommand(maxTaskCount, maxTaskLength);
                            break;

                        case "/showtasks":
                            ShowTaskCommand();
                            break;

                        case "/completetask":
                            CompleteTaskCommand();
                            break;

                        case "/showalltasks":
                            ShowAllTaskCommand();
                            break;

                        case "/removetask":
                            RemoveTaskCommand();
                            break;

                        case "/help":
                            HelpCommand();
                            break;

                        case "/info":
                            InfoCommand();
                            break;

                        case "/exit":
                            ExitCommand(ref run);
                            break;

                        default:
                            CustomCommands(input);
                            break;
                    }
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message); 
                Console.WriteLine("Пожалуйста, попробуйте снова.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла непредвиденная ошибка:");
                Console.WriteLine($"Тип исключения: {ex.GetType().FullName}");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }

        private static void StartCommand()
        {
            Console.Write("Введите ваше имя:");
            string TelegramUserName = Console.ReadLine();

            bool IsAllLetters(string username)
            {
                return username.All(char.IsLetter);
            }
            while (true)
            {
                if (IsAllLetters(TelegramUserName))
                {
                    Console.WriteLine($"Здравствуйте, {TelegramUserName}. Чем могу помочь?");
                    return;
                }
                else
                {
                    Console.WriteLine("Имя должно состоять из букв. Попробуйте еще раз\n");
                    Console.Write("Введите ваше имя:");
                    TelegramUserName = Console.ReadLine();
                }
                currentUser = new ToDoUser(TelegramUserName);
            }
        }

        private static void HelpCommand()
        {
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("/start - Начало взаимодействия с программой");
            Console.WriteLine("/addtask - Команда для добавления задачи в список");
            Console.WriteLine("/showtasks - Команда для отображения списка активных задач");
            Console.WriteLine("/comlpetetasks - Команда для завершения задачи");
            Console.WriteLine("/showalltasks - Команда для отображения списка всех задач");
            Console.WriteLine("/removetask - Команда для удаления задач из списка");
            Console.WriteLine("/help - Показать данную справку");
            Console.WriteLine("/info - Информация о версии и дате создания программы");
            Console.WriteLine("/echo <текст> - Повторяет введённый вами текст");
            Console.WriteLine("/exit - Завершение работы программы");
        }

        private static void InfoCommand()
        {
            Console.WriteLine($"Консольное приложение для имитации работы бота версия {version}, " +
                   $"создано {created_date}, обновлено {updated_date}. \n" +
                   $"В этой версии программы {whatsNew_text}");
        }

        private static void ExitCommand(ref bool running)
        {
            Console.WriteLine("Завершаем работу...");
            running = false;
        }

        private static void CustomCommands(string command)
        {
            if (command.StartsWith("/echo "))
            {
                string message = command.Substring(6); // Убираем "/echo "
                EchoCommand(message);
            }
            else
            {
                Console.WriteLine("Такой команды нет. Используйте команды (/start, /addtask, " +
                    "/showtasks, /comlpetetasks, /showalltasks, /removetask, /help, /info, /exit)");
            }
        }

        private static void EchoCommand(string text)
        {
            if (currentUser != null)
            {
                Console.WriteLine($"{currentUser.TelegramUserName}, вы ввели: {text}");
            }
            else
            {
                Console.WriteLine($"Вы ввели: {text}");
            }
        }

        private static void AddTaskCommand(int maxTaskCount, int maxTaskLength)
        {
            try
            {
                Console.Write("Введите описание задачи: ");
                string taskDescription = Console.ReadLine();

                // Проверка на длину задачи
                if (taskDescription.Length > maxTaskLength)
                {
                    throw new TaskLengthLimitException(taskDescription.Length, maxTaskLength);
                }

                // Проверка на дубликаты
                if (tasks.Any(x => x.Name == taskDescription))
                {
                    throw new DuplicateTaskException(taskDescription);
                }

                // Проверка на достижение лимита задач
                if (tasks.Count >= maxTaskCount)
                {
                    throw new TaskCountLimitException(maxTaskCount);
                }

                // Добавляем задачу
                var newTask = new ToDoItem(currentUser, taskDescription);
                tasks.Add(newTask);
                Console.WriteLine($"Задача \"{taskDescription}\" успешно добавлена");
            }
            catch (TaskCountLimitException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (TaskLengthLimitException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (DuplicateTaskException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка: {ex.Message}");
            }
        }

        private static bool ShowTaskCommand()
        {
            var activeTasks = tasks.Where(t => t.State == ToDoItemState.Active);

            //проверяем, что список содержит элементы
            bool isNotEmpty = tasks.Any();

            //если есть активные задачи, по выводим на экран
            if (activeTasks.Any())
            {
                Console.WriteLine("Список активных задач: ");
                foreach (var task in activeTasks.Select((value, i) => (value, i)))
                {
                    var value = task.value;
                    var index = task.i;
                    Console.WriteLine($"Задача {index + 1}. \"{task.value.Name}\" - " +
                        $"добавлена {task.value.CreatedAt} - ID задачи: {task.value.Id}");
                }
            }
            else
            {
                Console.WriteLine("Активных задач нет");
                return false;
            }
            return true;
        }

        private static void CompleteTaskCommand()
        {
            ShowTaskCommand();
            Console.Write("Введите ID задачи для завершения: ");
            string taskIdInput = Console.ReadLine();

            try
            {
                Guid taskId = Guid.Parse(taskIdInput);
                var task = tasks.FirstOrDefault(x => x.Id == taskId);

                if (task != null)
                {
                    task.State = ToDoItemState.Completed;
                    task.StateChangedAt = DateTime.UtcNow;
                    Console.WriteLine($"Задача \"{task.Name}\" завершена.");
                }
                else
                {
                    Console.WriteLine("Задача с указанным ID не найдена.");
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Введён некорректный ID задачи.");
            }
        }

        private static bool ShowAllTaskCommand()
        {
            if (tasks.Any())
            {
                Console.WriteLine("Список всех задач: ");
                foreach (var task in tasks.Select((value, i) => (value, i)))
                {
                    var value = task.value;
                    var index = task.i;
                    string state = task.value.State == ToDoItemState.Active ? "(Active)" : "(Completed)";
                    if (task.value.State == ToDoItemState.Completed)
                    {
                        Console.WriteLine($"Задача {index + 1}. \"{task.value.Name}\" - " +
                            $"добавлена {task.value.CreatedAt} - ID задачи: {task.value.Id} " +
                            $"- задача выполнена");
                    }
                    else 
                    {
                        Console.WriteLine($"Задача {index + 1}. \"{task.value.Name}\" - " +
                            $"добавлена {task.value.CreatedAt} - ID задачи: {task.value.Id} " +
                            $"- задача активна");
                    }
                }
            }
            else
            {
                Console.WriteLine("Задачи отсутствуют");
                return false;
            }
            return true;
        }

        private static void RemoveTaskCommand()
        {
            //если есть задачи, удаляем
            while (ShowAllTaskCommand()==true)
            {
                Console.WriteLine("Введите ID задачи, которую нужно удалить: ");
                if (Guid.TryParse(Console.ReadLine(), out Guid idInput))
                {
                    var taskToRemove = tasks.Find(task => task.Id == idInput);
                    if (taskToRemove != null)
                    {
                        Console.WriteLine($"Задача \"{taskToRemove.Name}\" удалена");
                        tasks.Remove(taskToRemove);
                        ShowAllTaskCommand();
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Нет задачи с указанным ID." +
                            " Введите корректный ID задачи");
                    }
                }
                else
                {
                    Console.WriteLine("Данные введены неверно. Введите корректный ID задачи: ");
                }
            }
        }
    }
}