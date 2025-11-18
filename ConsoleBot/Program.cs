using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.Infrastructure.DataAccess;
using ConsoleBot.Scenarios;
using ConsoleBot.TelegramBot;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;



namespace ConsoleBot
{
    class Program
    {
        public static ToDoUser? currentUser = null;
        public const string version = "5.2";
        public const string created_date = "20-08-2025";
        public const string updated_date = "18-11-2025";
        public const string whatsNew_text = "Реализация сценария AddTask, добавлен срок исполнения задач";
        private static IScenarioContextRepository contextRepository;
        public static List<ToDoItem> tasks = new List<ToDoItem>();
        private static IEnumerable<IScenario> scenarios = new List<IScenario>();

        public static async Task Main()
        {
            try
            {
                //чтение настроек
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true, true).Build();
                string botKey = configuration.GetSection("Telegram_key").Value;
                
                //репозитории и сервисы
                var userRepo = new FileUserRepository("data/users");
                var todoRepo = new FileToDoRepository("data/todos");
                var userService = new UserService(userRepo);
                var todoService = new ToDoService(todoRepo);
                var todoReportService = new ToDoReportService(todoRepo);
                
                // Добавляем сценарий
                var scenarios = new List<IScenario>
                {
                    new AddTaskScenario(userService, todoService),
                    
                };
                // Создаем контекст-хранилище сценариев
                var contextRepository = new InMemoryScenarioContextRepository();

                // Бот и обработчики
                var botClient = new TelegramBotClient(botKey);
                var updateHandler = new UpdateHandler(botClient, userService, todoService, todoReportService, scenarios, contextRepository);
                
                // Конфигурация и старт бота
                var cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = [ UpdateType.Message ],
                    DropPendingUpdates = true
                };
                botClient.StartReceiving(updateHandler, receiverOptions, cts.Token);
                
                // Получаем информацию о боте
                var me = await botClient.GetMe();
                Console.WriteLine($"{me.FirstName} запущен!");

                // Выход при нажатии клавиши A
                Console.WriteLine("Нажмите клавишу A для выхода");
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.A)
                    {
                        cts.Cancel();
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"{me.Username} работает");
                    }
                }
                // Очистка
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
