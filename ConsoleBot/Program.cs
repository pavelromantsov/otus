using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.TelegramBot;
using Otus.ToDoList.ConsoleBot;
using ConsoleBot.Infrastructure.DataAccess;

namespace ConsoleBotCommands
{
    class Program
    {
        public static ToDoUser? currentUser = null;
        public const string version = "3.0";
        public const string created_date = "20-08-2025";
        public const string updated_date = "09-10-2025";
        public const string whatsNew_text = "Реализован репозиторий, добавлен поиск задач";
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
                botClient.StartReceiving(updateHandler);
                Task.Delay(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }
    }
}