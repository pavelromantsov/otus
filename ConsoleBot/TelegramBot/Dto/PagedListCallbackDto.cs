using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.TelegramBot.Dto
{
    public class PagedListCallbackDto:ToDoItemCallbackDto
    {
        public int Page { get; set; }
        public PagedListCallbackDto(string action, Guid? toDoListId, int page)
        : base(action, toDoListId)
        {
            Page = page;
        }

        public static new PagedListCallbackDto FromString(string input)
        {
            var parts = input.Split('|');
            if (parts.Length < 3)
                throw new ArgumentException("Некорректный формат callbackData для PagedListCallbackDto.");

            var action = parts[0];

            Guid? listId = null;
            if (!string.IsNullOrEmpty(parts[1]) && Guid.TryParse(parts[1], out var parsed))
                listId = parsed;

            if (!int.TryParse(parts[2], out var page))
                throw new ArgumentException("Некорректный номер страницы в PagedListCallbackDto.");

            return new PagedListCallbackDto(action, listId, page);
        }
        public override string ToString()
        {
            return $"{Action}|{ToDoListId}|{Page}";
        }    
    }
}
