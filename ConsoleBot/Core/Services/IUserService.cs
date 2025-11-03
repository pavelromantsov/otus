using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.Services
{
    public interface IUserService
    {
        Task<ToDoUser?> GetUserAsync(Guid userId, string telegramUserName, CancellationToken cancellationToken);
        Task<ToDoUser?>? GetUserAsync(long userId, string telegramUserName, CancellationToken cancellationToken);
        Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken);
        Task RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken);
        Task <bool> IsUserRegistered(long userId, string telegramUserName, CancellationToken cancellationToken);
    }
}