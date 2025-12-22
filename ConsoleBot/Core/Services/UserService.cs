using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task RegisterUserAsync(long telegramUserId, CancellationToken ct)
        {
            var existingUser = await _repository.GetUserByTelegramUserIdAsync(telegramUserId, ct);
            if (existingUser != null)
            {
                return;
            }
            var newUser = new ToDoUser(telegramUserId, ct);
            await _repository.AddAsync(newUser, ct);
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken ct)
        {
            return await _repository.GetUserAsync(userId, ct);
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId,  CancellationToken ct)
        {
            return await _repository.GetUserByTelegramUserIdAsync(telegramUserId, ct);
        }

        public ToDoUser? Add(long telegramUserId, CancellationToken ct) 
        {
            var newUser = new ToDoUser(telegramUserId, ct);
            _repository.AddAsync(newUser, ct);
            return newUser;
        }

        public async Task<ToDoUser?>? GetUserAsync(long userId, string telegramUserName, CancellationToken ct)
        {
            return await _repository.GetUserAsync(userId, ct);
        }
        public async Task <bool> IsUserRegistered(long telegramUserId, CancellationToken ct)
        {
            var user = await _repository.GetUserByTelegramUserIdAsync(telegramUserId,  ct);
            return user != null;
        }

    }
}