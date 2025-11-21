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
            return await _repository.GetAllByUserIdAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _repository.GetActiveByUserIdAsync(userId, cancellationToken);
        }


        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, CancellationToken cancellationToken)
        {
            if (await _repository.ExistsByNameAsync(user.UserId, name, cancellationToken))
            {
                throw new DuplicateTaskException(name);
            }
            var item = new ToDoItem(user, name, deadline, cancellationToken);
            await _repository.AddAsync(item, cancellationToken);
            return item;
        }

        public async Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken)
        {
         
            var task = await _repository.GetAsync(id, cancellationToken);
            if (task != null)
            {   
                task.State = ToDoItemState.Completed;
                task.StateChangedAt = DateTime.Now;
                await _repository.UpdateAsync(task, cancellationToken);
            }
        }

        public void Delete(Guid id, CancellationToken cancellationToken)
        {
            _repository.Delete(id, cancellationToken);
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
            return await _repository.Find(
            user.UserId,
            task => task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        }

        public async Task<bool>ExistsByNameAsync(ToDoUser user, string name, CancellationToken cancellationToken)
        {
            return await _repository.ExistsByNameAsync(user.UserId, name, cancellationToken);
        }

        public async Task<int>CountActiveAsync(ToDoUser user, CancellationToken cancellationToken)
        {
            return await _repository.CountActiveAsync(user.UserId, cancellationToken);
        }

    }
}