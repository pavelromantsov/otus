using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.Services
{
    public interface IUserService
    {
        Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct);
        Task<ToDoUser?>? GetUserAsync(long userId,string telegramUserName, CancellationToken ct);
        Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct);
        Task <bool> IsUserRegistered(long userId, CancellationToken ct);
        Task RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken ct);
    }
}