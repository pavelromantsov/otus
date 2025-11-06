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

        public async Task RegisterUserAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            var existingUser = await _repository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (existingUser != null)
            {
                return;
            }
            var newUser = new ToDoUser(telegramUserId, cancellationToken);
            await _repository.AddAsync(newUser, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _repository.GetUserAsync(userId, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId,  CancellationToken cancellationToken)
        {
            return await _repository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
        }

        public ToDoUser? Add(long telegramUserId, CancellationToken cancellationToken) 
        {
            var newUser = new ToDoUser(telegramUserId, cancellationToken);
            _repository.AddAsync(newUser, cancellationToken);
            return newUser;
        }

        public async Task<ToDoUser?>? GetUserAsync(long userId, string telegramUserName, CancellationToken cancellationToken)
        {
            return await _repository.GetUserAsync(userId, cancellationToken);
        }
        public async Task <bool> IsUserRegistered(long telegramUserId, CancellationToken cancellationToken)
        {
            var user = await _repository.GetUserByTelegramUserIdAsync(telegramUserId,  cancellationToken);
            return user != null;
        }

    }
}