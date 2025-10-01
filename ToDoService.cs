using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBotCommands;

namespace ConsoleBotProgramm
{
    public class ToDoService : IToDoService
    {
        private readonly IList<ToDoItem> _tasks = new List<ToDoItem>();

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _tasks.Where(t => t.User.UserId == userId).ToList();
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _tasks.Where(t => t.User.UserId == userId && t.State == ToDoItemState.Active).ToList();
        }

        public ToDoItem Add(ToDoUser user, string name)
        {
            var item = new ToDoItem(user, name);
            _tasks.Add(item);
            return item;
        }

        public void MarkCompleted(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                task.State = ToDoItemState.Completed;
            }
        }

        public void Delete(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }

        public int ParseAndValidateInt(string? str, int min, int max)
        {
            if (int.TryParse(str, out int number) && number >= min && number <= max)
            {
                return number;
            }
            throw new ArgumentException($"Значение должно быть числом от {min} до {max}.");
        }

        public void ValidateString(string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("Строка не должна быть пустой или содержать только пробелы");
            }
        }

    }
}
