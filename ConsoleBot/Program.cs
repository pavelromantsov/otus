using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.TelegramBot;
using Otus.ToDoList.ConsoleBot;
using ConsoleBot.Infrastructure.DataAccess;

namespace ConsoleBot
{
    class Program
    {
        public static ToDoUser? currentUser = null;
        public const string version = "4.0";
        public const string created_date = "20-08-2025";
        public const string updated_date = "20-10-2025";
        public const string whatsNew_text = "Асинхронное выполнение";
        public static List<ToDoItem> tasks = new List<ToDoItem>();

        public static void Main(string[] args)
        {
            try
            {
                var inMemoryUserRepository = new InMemoryUserRepository();
                var inMemoryTodoRepository = new InMemoryToDoRepository();
                var userService = new UserService(inMemoryUserRepository);
                var todoService = new ToDoService(inMemoryTodoRepository);
                var todoReportService = new ToDoReportService(inMemoryTodoRepository);
                var botClient = new ConsoleBotClient();
                var updateHandler = new UpdateHandler(botClient, userService, todoService, todoReportService);
                var cts = new CancellationTokenSource();
                botClient.StartReceiving(updateHandler, cts.Token);
                Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}