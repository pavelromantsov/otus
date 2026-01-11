using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;



namespace ConsoleBot.Core.Services
{
    public class ToDoService : IToDoService
    {
        private readonly IToDoRepository _repository;
        private readonly IToDoListService _toDoListService;

        public ToDoService(IToDoRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _repository.GetAllByUserIdAsync(userId, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _repository.GetActiveByUserIdAsync(userId, ct);
        }


        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, ToDoList? list, CancellationToken ct)
        {
            // проверка на дубликат
            if (await _repository.ExistsByNameAsync(user.UserId, name, ct))
                throw new DuplicateTaskException(name);

            var item = new ToDoItem
            {
                Id = Guid.NewGuid(),
                Name = name,              
                User = user,              
                List = list,              
                Deadline = deadline,
                State = ToDoItemState.Active,
                CreatedAt = DateTime.UtcNow,
                StateChangedAt = null
            };

            await _repository.AddAsync(item, ct);
            return item;
        }

        public async Task MarkCompletedAsync(Guid id, CancellationToken ct)
        {

            var task = await _repository.GetAsync(id, ct);
            if (task != null)
            {
                task.State = ToDoItemState.Completed;
                task.StateChangedAt = DateTime.Now;
                await _repository.UpdateAsync(task, ct);
            }
        }

        public void Delete(Guid id, CancellationToken ct)
        {
            _repository.Delete(id, ct);
        }

        public int ParseAndValidateInt(string? str, int min, int max, CancellationToken ct)
        {
            if (int.TryParse(str, out int number) && number >= min && number <= max)
            {
                return number;
            }
            throw new ArgumentException($"Значение должно быть числом от {min} до {max}.");
        }

        public async Task ValidateStringAsync(string? str, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentException("Строка не должна быть пустой или содержать только пробелы");
            }
        }
        public async Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct)
        {
            return await _repository.Find(
            user.UserId,
            task => task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase),
            ct);
        }

        public async Task<bool> ExistsByNameAsync(ToDoUser user, string name, CancellationToken ct)
        {
            return await _repository.ExistsByNameAsync(user.UserId, name, ct);
        }

        public async Task<int> CountActiveAsync(ToDoUser user, CancellationToken ct)
        {
            return await _repository.CountActiveAsync(user.UserId, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(
                    Guid userId,
                    Guid? listId,
                    CancellationToken cancellationToken)
        {
            var items = await _repository.GetAllByUserIdAsync(userId, cancellationToken);
            IEnumerable<ToDoItem> filtered = items;
            if (listId.HasValue)
            {
                // задачи конкретного списка
                filtered = filtered.Where(item => item.List != null && item.List.Id == listId.Value);
            }
            else 
            {
                // задачи "без списка"
                filtered = filtered.Where(item => item.List == null);
            }
            return filtered.ToList();
        }

        public async Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct)
        {
            return await _repository.GetAsync(toDoItemId, ct);
        }
    }
}