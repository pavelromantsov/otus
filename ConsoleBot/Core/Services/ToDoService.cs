using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;


namespace ConsoleBot.Core.Services
{
    public class ToDoService : IToDoService
    {
        private readonly IToDoRepository _repository;

        public ToDoService(IToDoRepository repository)
        { 
            _repository = repository;
        }

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _repository.GetAllByUserId(userId);
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _repository.GetAllByUserId(userId);
        }


        public ToDoItem Add(ToDoUser user, string name)
        {
            if (_repository.ExistsByName(user.UserId, name))
            {
                throw new DuplicateTaskException(name);
            }
            var item = new ToDoItem(user, name);
            _repository.Add(item);
            return item;
        }

        public void MarkCompleted(Guid id)
        {
            var task = _repository.Get(id);
            if (task != null)
            {
                task.State = ToDoItemState.Completed;
                task.StateChangedAt = DateTime.Now;
                _repository.Update(task);
            }
        }

        public void Delete(Guid id)
        {
            _repository.Delete(id);
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
        public IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix)
        {
            return _repository.Find(
            user.UserId,
            task => task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase)
        );
        }

        public bool ExistsByName(Guid userId, string name)
        {
            return _repository.ExistsByName(userId, name);
        }

        public int CountActive(Guid userId)
        {
            return _repository.CountActive(userId);
        }

    }
}