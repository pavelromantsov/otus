using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.TelegramBot.Dto
{
    public class ToDoListCallbackDto : CallbackDto
    {
        public Guid? ToDoListId { get; set; }

        public ToDoListCallbackDto(string action, Guid? toDoListId) : base(action)
        {
            ToDoListId = toDoListId;
        }
        public static new ToDoListCallbackDto FromString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            var parts = input.Split('|');

            if (parts.Length < 1)
            {
                return null;
            }

            var action = parts[0];
            Guid? toDoListId = null;

            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
            {
                if (Guid.TryParse(parts[1], out var guid))
                {
                    toDoListId = guid;
                }
            }

            return new ToDoListCallbackDto(action, toDoListId);
        }
        public override string ToString()
        {
            return $"{base.ToString()}|{ToDoListId?.ToString() ?? ""}";
        }

    }

}