using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class SqlUserRepository : IUserRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlUserRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var userModel = await db.ToDoUsers
                .FirstOrDefaultAsync(u => u.UserId == userId, ct);

            return userModel != null ? ModelMapper.MapFromModel(userModel) : null;
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var userModel = await db.ToDoUsers
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

            return userModel != null ? ModelMapper.MapFromModel(userModel) : null;
        }

        public async Task AddAsync(ToDoUser? user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.TelegramUserId <= 0)
                return;

            using var db = _factory.CreateDataContext();

            var existing = await db.ToDoUsers
                .FirstOrDefaultAsync(u => u.TelegramUserId == user.TelegramUserId, ct);

            if (existing != null)
            {
                existing.TelegramUserName ??= user.TelegramUserName;
                await db.UpdateAsync(existing);
                return;
            }

            var userModel = ModelMapper.MapToModel(user);
            userModel.UserId = Guid.NewGuid();
            userModel.RegisteredAt = DateTime.UtcNow;

            await db.InsertAsync(userModel);
        }

        public async Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken ct)
        {
            return await GetUserByTelegramUserIdAsync(telegramUserId, ct);
        }

        public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            ct.ThrowIfCancellationRequested();

            var userModels = await db.ToDoUsers.ToListAsync(ct);
            var users = userModels.Select(ModelMapper.MapFromModel).ToList();
            return users.AsReadOnly();
        }
    }
}
