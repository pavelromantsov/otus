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
        IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId, CancellationToken cancellationToken);
        //Возвращает ToDoItem для UserId со статусом Active
        IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId, CancellationToken cancellationToken);
        ToDoItem? Get(Guid id, CancellationToken cancellationToken);
        void Add(ToDoItem item);
        void Update(ToDoItem item);
        void Delete(Guid id);
        //Проверяет есть ли задача с таким именем у пользователя
        bool ExistsByName(Guid userId, string name);
        //Возвращает количество активных задач у пользователя
        int CountActive(Guid userId);
        IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken);

    }
}
