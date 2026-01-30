using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.DataAccess
{
    public interface IUserRepository
    {
        Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct);
        Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct);
        Task AddAsync(ToDoUser user, CancellationToken ct);
        Task<ToDoUser?> GetUserAsync(long userId, CancellationToken ct);
        Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct);
    }
}
