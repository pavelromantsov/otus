using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.TelegramBot.Dto
{
    public class ToDoItemCallbackDto:CallbackDto
    {
        public Guid? ToDoListId { get; set; }

        public Guid ToDoItemId { get; set; }

        public ToDoItemCallbackDto (string action, Guid toDoItemId) : base (action)
        {
            ToDoItemId = toDoItemId;
        }

        public ToDoItemCallbackDto(string action, Guid? toDoListId) : base(action)
        {
            ToDoListId = toDoListId;
        }

        public static new ToDoItemCallbackDto FromString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new ToDoItemCallbackDto(string.Empty, Guid.Empty);

            if (input.Contains("|"))
            {
                var parts = input.Split('|');
                return new ToDoItemCallbackDto(
                    parts[0],
                    Guid.TryParse(parts[1], out var guid) ? guid : Guid.Empty);
            }

            return new ToDoItemCallbackDto(input, Guid.Empty);
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{ToDoItemId}";
        }
    }
}
