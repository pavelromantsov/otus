using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.Infrastructure.DataAccess;
using ConsoleBot.TelegramBot;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ConsoleBot.Scenarios;



namespace ConsoleBot
{
    class Program
    {
        public static ToDoUser? currentUser = null;
        public const string version = "5.1";
        public const string created_date = "20-08-2025";
        public const string updated_date = "03-11-2025";
        public const string whatsNew_text = "Переход с InMemmory на хранение в файлах, индексирование задач";
        private static IScenarioContextRepository contextRepository;
        public static List<ToDoItem> tasks = new List<ToDoItem>();
        private static IEnumerable<IScenario> scenarios = new List<IScenario>();

        public static async Task Main()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true, true).Build();
                string botKey = configuration.GetSection("Telegram_key").Value;
                var userRepo = new FileUserRepository("data/users");
                var todoRepo = new FileToDoRepository("data/todos");
                var userService = new UserService(userRepo);
                var todoService = new ToDoService(todoRepo);
                var todoReportService = new ToDoReportService(todoRepo);
                var botClient = new TelegramBotClient(botKey);

                var updateHandler = new UpdateHandler(botClient, userService, todoService, todoReportService, scenarios, contextRepository);
                var cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions
                
                {
                    AllowedUpdates = [UpdateType.Message],
                    DropPendingUpdates = true
                };
                botClient.StartReceiving(updateHandler, receiverOptions, cts.Token);
                var me = await botClient.GetMe();
                Console.WriteLine($"{me.FirstName} запущен!");
                // Отображение команд в меню
                var commands = new BotCommand[]
                {
                new BotCommand("/start", "Начало работы"),
                new BotCommand("/showalltasks", "Показать все задачи"),
                new BotCommand("/showtasks", "Показать активные задачи"),
                new BotCommand("/report", "Получить отчёт по задачам"),
                new BotCommand("/help", "Справка по командам")
                };
                await botClient.SetMyCommands(commands);

                // выход при нажатии клавиши А
                Console.WriteLine("Нажмите клавишу A для выхода");
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.A)
                    {
                        await Task.Delay(1);
                        cts.Cancel();
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"{me.Username} работает");
                    }
                }
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
