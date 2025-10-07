using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace ConsoleBotCommands
{
    class Program
    {
        public static ToDoUser? currentUser = null;
        public const string version = "2.0";
        public const string created_date = "20-08-2025";
        public const string updated_date = "29-09-2025";
        public const string whatsNew_text = "Подключены классы и реализованы интерфейсы";
        public static List<ToDoItem> tasks = new List<ToDoItem>();

        public static void Main(string[] args)
        {
            try
            {
                var userService = new UserService();
                var todoService = new ToDoService();
                var botClient = new ConsoleBotClient();
                var updateHandler = new UpdateHandler(botClient, userService, todoService);
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