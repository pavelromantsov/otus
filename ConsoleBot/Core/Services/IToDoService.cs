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
        Task<ToDoItem> AddAsync(ToDoUser user, string name, CancellationToken cancellationToken);
        Task <int>CountActiveAsync(ToDoUser user, CancellationToken cancellationToken);
        Task Delete(Guid id, CancellationToken cancellationToken);
        Task <bool>ExistsByNameAsync(ToDoUser user, string name, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
        Task MarkCompletedAsync(long telegramUserId, Guid id, CancellationToken cancellationToken);
        int ParseAndValidateInt(string? str, int min, int max, CancellationToken cancellationToken);
        Task ValidateStringAsync(string? str, CancellationToken cancellationToken);
    }

}