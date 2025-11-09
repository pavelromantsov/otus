using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.DataAccess
{
    public interface IToDoRepository
    {
        Task <IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        //Возвращает ToDoItem для UserId со статусом Active
        Task <IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddAsync(ToDoItem item, CancellationToken cancellationToken);
        Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken);
        void Delete(Guid id, CancellationToken cancellationToken);
        //Проверяет есть ли задача с таким именем у пользователя
        Task<bool>ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken);
        //Возвращает количество активных задач у пользователя
        Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken);
        Task<ToDoItem> GetAsync(Guid Id, CancellationToken cancellationToken);
    }
}
