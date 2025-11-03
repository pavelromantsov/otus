using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;
using Telegram.Bot.Types;


namespace ConsoleBot.Core.Services
{
    public class ToDoService : IToDoService
    {
        private readonly IToDoRepository _repository;

        public ToDoService(IToDoRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            return await _repository.GetAllByUserIdAsync(telegramUserId, cancellationToken);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            return await _repository.GetActiveByUserIdAsync(telegramUserId, cancellationToken);
        }


        public async Task<ToDoItem> AddAsync(ToDoUser user, string name, CancellationToken cancellationToken)
        {
            if (await _repository.ExistsByNameAsync(user.TelegramUserId, name, cancellationToken))
            {
                throw new DuplicateTaskException(name);
            }
            var item = new ToDoItem(user, name, cancellationToken);
            await _repository.AddAsync(item, cancellationToken);
            return item;
        }

        public async Task MarkCompletedAsync(long telegramUserId, Guid userId, CancellationToken cancellationToken)
        {
         
            var task = await _repository.GetAsync(telegramUserId, userId, cancellationToken);
            if (task != null)
            {   
                task.State = ToDoItemState.Completed;
                task.StateChangedAt = DateTime.Now;
                await _repository.UpdateAsync(task, cancellationToken);
            }
        }

        public async Task Delete(Guid id, CancellationToken cancellationToken)
        {
            await _repository.Delete(id, cancellationToken);
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
            user.TelegramUserId,
            task => task.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase),
            cancellationToken);
        }

        public async Task<bool>ExistsByNameAsync(ToDoUser user, string name, CancellationToken cancellationToken)
        {
            return await _repository.ExistsByNameAsync(user.TelegramUserId, name, cancellationToken);
        }

        public async Task<int>CountActiveAsync(ToDoUser user, CancellationToken cancellationToken)
        {
            return await _repository.CountActiveAsync(user.TelegramUserId, cancellationToken);
        }

    }
}