using System;
using System.Collections.Generic;
using System.Linq;


namespace ConsoleBotApp
{
    class Program
    {

        private static string username = "";
        private const string version = "1.1";
        private const string created_date = "20-08-2025";
        private const string updated_date = "04-09-2025";
        private static List<string> tasks = new List<string>();

        static void Main()
        {
            bool run = true;

            Console.WriteLine("Добро пожаловать в консольное приложение, имитирующее работу Телеграмм-бота!" +
                               "\n\nСписок команд:");
            Console.WriteLine("/start - начать работу");
            Console.WriteLine("/addtask - добавить задачу в список");
            Console.WriteLine("/showtasks - отображение списка всех добавленных задач");
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
                        AddTaskCommand();
                        break;

                    case "/showtasks":
                        ShowTaskCommand();
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

        // Реализация каждой команды
        private static void StartCommand()
        {
            Console.Write("Введите ваше имя:");
            username = Console.ReadLine();

            bool IsAllLetters(string username)
            {
                return username.All(char.IsLetter);
            }
            while (true)
            {
                if (IsAllLetters(username))
                {
                    Console.WriteLine($"Здравствуйте, {username}. Чем могу помочь?");
                    return;
                }
                else
                {
                    Console.WriteLine("Имя должно состоять из букв. Попробуйте еще раз\n");
                    Console.Write("Введите ваше имя:");
                    username = Console.ReadLine();
                }
            }
        }

        private static void HelpCommand()
        {
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("/start - Начало взаимодействия с программой");
            Console.WriteLine("/help - Показать данную справку");
            Console.WriteLine("/addtask - Команда для добавления задачи в список");
            Console.WriteLine("/showtasks - Команда для отображения списка задач");
            Console.WriteLine("/removetask - Команда для удаления задач из списка");
            Console.WriteLine("/info - Информация о версии и дате создания программы");
            Console.WriteLine("/echo <текст> - Повторяет введённый вами текст");
            Console.WriteLine("/exit - Завершение работы программы");
        }

        private static void InfoCommand()
        {
            Console.WriteLine($"Консольное приложение для имитации работы бота версия {version}, " +
                              $"создано {created_date}, обновлено {updated_date}");
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
                Console.WriteLine("Такой команды нет. Используйте команды (/start, /addtask, /showtasks," +
                    " /removetask, /help, /info, /exit)");
            }
        }

        private static void EchoCommand(string text)
        {
            if (username != "")
            {
                Console.WriteLine($"{username}, вы ввели: {text}");
            }
            else
            {
                Console.WriteLine($"Вы ввели: {text}");
            }
        }
        private static void AddTaskCommand()
        {
            Console.Write("Введите описание задачи: ");
            string taskDescription = Console.ReadLine();
            tasks.Add(taskDescription);
            Console.WriteLine($"Задача \"{taskDescription}\" добавлена");
        }

        private static bool ShowTaskCommand()
        {
            //проверяем, что список содержит элементы
            bool isNotEmpty = tasks.Any();
            
            //если есть задачи, по выводим на экран
            if (isNotEmpty)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. " + tasks[i]);
                }
            }  
            else
            {
                Console.WriteLine("Ваш список пуст");
            }
            return isNotEmpty;
        }

        private static void RemoveTaskCommand()
        {
            ShowTaskCommand();
            //пытаемся удалить задачу
            Console.Write("Введите номер задачи, которую нужно удалить: ");
            while (true)
            {     
                if (int.TryParse(Console.ReadLine(), out int index))
                {   
                    if (ShowTaskCommand())// проверяем, что список не пустой, затем удаляем задачу
                    {
                        Console.WriteLine($"Задача \"{tasks[index - 1]}\"  удалена\n");
                        tasks.RemoveAt(index - 1);
                        Console.WriteLine("Cписок задач после удаления");
                        ShowTaskCommand();
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Нет задачи с указанным номером." +
                            " Введите корректный номер задачи");
                    }
                }
                else
                {
                    Console.WriteLine("Данные введены неверно");
                }
            }
        }

    }
}