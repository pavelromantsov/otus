using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.DataAccess
{
    public interface IToDoRepository
    {
        Task <IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
        //Возвращает ToDoItem для UserId со статусом Active
        Task <IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
        Task AddAsync(ToDoItem item, CancellationToken cancellationToken);
        Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken);
        Task Delete(Guid id, CancellationToken cancellationToken);
        //Проверяет есть ли задача с таким именем у пользователя
        Task<bool>ExistsByNameAsync(long telegramUserId, string name, CancellationToken cancellationToken);
        //Возвращает количество активных задач у пользователя
        Task<int> CountActiveAsync(long telegramUserId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> Find(long telegramUserId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken);
        Task<ToDoItem> GetAsync(long telegramUserId, Guid userId, CancellationToken cancellationToken);
    }
}
