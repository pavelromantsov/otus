using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.DataAccess.Models;
using ConsoleBot.Core.Entities;
using ConsoleBot.Infrastructure.DataAccess;
using LinqToDB.Data;

namespace ConsoleBot.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken ct)
        {
            if (telegramUserId <= 0) return;

            var user = new ToDoUser
            {
                TelegramUserId = telegramUserId,
                TelegramUserName = telegramUserName
            };

            await _repository.AddAsync(user, ct);
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
            var newUser = new ToDoUser();
            _repository.AddAsync(newUser, ct);
            return newUser;
        }

        public async Task<ToDoUser?>? GetUserAsync(long userId, string telegramUserName, CancellationToken ct)
        {
            var user = await _repository.GetUserByTelegramUserIdAsync(userId, ct);
            if (user != null) return user;

            return null; 
        }

        public async Task <bool> IsUserRegistered(long telegramUserId, CancellationToken ct)
        {
            var user = await _repository.GetUserByTelegramUserIdAsync(telegramUserId,  ct);
            return user != null;
        }

    }
}