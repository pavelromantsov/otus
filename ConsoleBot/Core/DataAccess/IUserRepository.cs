using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.DataAccess
{
    public interface IUserRepository
    {
        Task <ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
        Task <ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
        void Add(ToDoUser user);
        Task<ToDoUser?> GetUserAsync(long userId, CancellationToken cancellationToken);
    }
}
