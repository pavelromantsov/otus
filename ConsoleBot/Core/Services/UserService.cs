using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleBot.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken)
        {
            var existingUser = await _repository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (existingUser != null)
            {
                return;
            }
            var newUser = new ToDoUser(telegramUserId, telegramUserName, cancellationToken);
            _repository.Add(newUser);
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _repository.GetUserAsync(userId, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            return await _repository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
        }

        public ToDoUser? Add(long telegramUserId, string telegramUserName, CancellationToken cancellationToken) 
        {
            var newUser = new ToDoUser(telegramUserId, telegramUserName, cancellationToken);
            _repository.Add(newUser);
            return newUser;
        }

        public async Task<ToDoUser?>? GetUserAsync(long userId, CancellationToken cancellationToken)
        {
            return await _repository.GetUserAsync(userId, cancellationToken);
        }
        public bool IsUserRegistered(long userId, CancellationToken cancellationToken)
        {
            return _repository.GetUserByTelegramUserIdAsync(userId, cancellationToken) != null;
        }

    }
}