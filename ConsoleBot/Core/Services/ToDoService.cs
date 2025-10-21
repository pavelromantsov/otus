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

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return _repository.GetAllByUserId(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return _repository.GetAllByUserId(userId, cancellationToken);
        }


        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, CancellationToken cancellationToken)
        {
            if (_repository.ExistsByName(user.UserId, name))
            {
                throw new DuplicateTaskException(name);
            }
            var item = new ToDoItem(user, name, cancellationToken);
            _repository.Add(item);
            return item;
        }

        public async Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken)
        {
            var task = _repository.Get(id, cancellationToken);
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

        public int ParseAndValidateInt(string? str, int min, int max, CancellationToken cancellationToken)
        {
            if (int.TryParse(str, out int number) && number >= min && number <= max)
            {
                return number;
            }
            throw new ArgumentException($"Значение должно быть числом от {min} до {max}.");
        }

        public async Task ValidateStringAsync(string? str, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("Строка не должна быть пустой или содержать только пробелы");
            }
        }
        public async Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken)
        {
            return _repository.Find(
            user.UserId,
            task => task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        }

        public bool ExistsByName(Guid userId, string name)
        {
            return _repository.ExistsByName(userId, name);
        }

        public int CountActiveAsync(Guid userId)
        {
            return _repository.CountActive(userId);
        }

    }
}