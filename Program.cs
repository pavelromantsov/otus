using System;
using System.Linq;

namespace ConsoleBotApp
{
    class Program
    {

        private static string username = "";
        private const string version = "1.0";
        private const string created_date = "20-08-2025";

        static void Main(string[] args)
        {
            bool run = true;

            Console.WriteLine("Добро пожаловать в консольное приложение, имитирующее работу Телеграмм-бота!" +
                               "\n\nСписок команд:");
            Console.WriteLine("/start - начать работу");
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
                    Console.WriteLine($"Здравствуйте, {username}." );
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
            Console.WriteLine("/info - Информация о версии и дате создания программы");
            Console.WriteLine("/echo <текст> - Повторяет введённый вами текст");
            Console.WriteLine("/exit - Завершение работы программы");
        }

        private static void InfoCommand()
        {
            Console.WriteLine($"Консольное приложение для имитации работы бота версия {version}, " +
                              $"создано {created_date}");
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
                Console.WriteLine("Такой команды нет. Используйте команды (/start, /help, /info, /exit)");
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
    }
}