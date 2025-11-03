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
            var existingUser = await _repository.GetUserByTelegramUserIdAsync(telegramUserId, telegramUserName, cancellationToken);
            if (existingUser != null)
            {
                return;
            }
            var newUser = new ToDoUser(telegramUserId, telegramUserName, cancellationToken);
            await _repository.AddAsync(newUser, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, string telagramUserName, CancellationToken cancellationToken)
        {
            return await _repository.GetUserAsync(userId, telagramUserName, cancellationToken);
        }

        public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, string telegramUserName,  CancellationToken cancellationToken)
        {
            return await _repository.GetUserByTelegramUserIdAsync(telegramUserId, telegramUserName, cancellationToken);
        }

        public ToDoUser? Add(long telegramUserId, string telegramUserName, CancellationToken cancellationToken) 
        {
            var newUser = new ToDoUser(telegramUserId, telegramUserName, cancellationToken);
            _repository.AddAsync(newUser, cancellationToken);
            return newUser;
        }

        public async Task<ToDoUser?>? GetUserAsync(long userId, string telagramUserName, CancellationToken cancellationToken)
        {
            return await _repository.GetUserAsync(userId, telagramUserName,cancellationToken);
        }
        public async Task <bool> IsUserRegistered(long telegramUserId, string telegramUserName, CancellationToken cancellationToken)
        {
            var user = await _repository.GetUserByTelegramUserIdAsync(telegramUserId, telegramUserName, cancellationToken);
            return user != null;
        }

    }
}