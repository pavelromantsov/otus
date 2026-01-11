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
        public const string version = "6.0";
        public const string created_date = "20-08-2025";
        public const string updated_date = "11-01-2026";
        public const string whatsNew_text = "Подключена база данных PostgeSQL 18";

        public static async Task Main()
        {
            try
            {
                // Чтение настроек
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();

                string botKey = configuration.GetSection("Telegram_key").Value
                    ?? throw new InvalidOperationException("Telegram_key not found");
                
                
                // SQL репозитории вместо File
                var factory = new DataContextFactory(configuration);
                var userRepo = new SqlUserRepository(factory);
                var todoRepo = new SqlToDoRepository(factory);
                var todoListRepo = new SqlToDoListRepository(factory);

                // Сервисы (автоматически используют SQL репозитории)
                var userService = new UserService(userRepo);
                var todoService = new ToDoService(todoRepo);
                var todoReportService = new ToDoReportService(todoRepo);
                var toDoListService = new ToDoListService(todoListRepo);

                // Контекст сценариев
                var contextRepository = new InMemoryScenarioContextRepository();

                // Сценарии (остаются те же)
                var scenarios = new List<IScenario>
            {
                new AddTaskScenario(userService, todoService, contextRepository, toDoListService),
                new AddListScenario(userService, toDoListService, todoService, contextRepository),
                new DeleteListScenario(userService, toDoListService, todoService, contextRepository),
                new DeleteTaskScenario(userService, toDoListService, todoService, contextRepository),
            };

                // Бот
                var botClient = new TelegramBotClient(botKey);
                var updateHandler = new UpdateHandler(botClient, userService, todoService,
                    todoReportService, scenarios, contextRepository, toDoListService);

                // События
                updateHandler.OnHandleUpdateStarted += (message) =>
                    Console.WriteLine($"Началась обработка: '{message}'");
                updateHandler.OnHandleUpdateCompleted += (message) =>
                    Console.WriteLine($"Закончилась обработка: '{message}'");

                // Запуск
                var cts = new CancellationTokenSource();
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
                    DropPendingUpdates = true
                };

                botClient.StartReceiving(updateHandler, receiverOptions, cts.Token);

                var me = await botClient.GetMe();
                Console.WriteLine($"Телеграм-бот {me.FirstName} v{version} создан {created_date}, обновлен {updated_date}. {whatsNew_text}");

                Console.WriteLine("Нажмите 'A' для выхода");
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.A)
                    {
                        cts.Cancel();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
        }
    }
}
