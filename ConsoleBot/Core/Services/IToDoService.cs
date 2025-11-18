using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;
using ConsoleBot.Core.Services;

namespace ConsoleBot.Core.Services
{
    public interface IToDoService
    {
        Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, CancellationToken cancellationToken);
        //Task AddAsync(ToDoUser toDoUser, string? name, DateTime deadline, CancellationToken ct);
        Task <int>CountActiveAsync(ToDoUser user, CancellationToken cancellationToken);
        void Delete(Guid id, CancellationToken cancellationToken);
        Task <bool>ExistsByNameAsync(ToDoUser user, string name, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task MarkCompletedAsync(Guid id, CancellationToken cancellationToken);
        int ParseAndValidateInt(string? str, int min, int max, CancellationToken cancellationToken);
        Task ValidateStringAsync(string? str, CancellationToken cancellationToken);
    }

}