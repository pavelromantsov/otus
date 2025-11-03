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
        Task <ToDoUser?> GetUserAsync(Guid userId, string telagramUserName,CancellationToken cancellationToken);
        Task <ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken);
        Task AddAsync(ToDoUser user, CancellationToken cancellationToken);
        Task<ToDoUser?> GetUserAsync(long userId, string telagramUserName,CancellationToken cancellationToken);
    }
}
