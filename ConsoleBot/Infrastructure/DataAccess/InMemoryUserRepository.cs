using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;


namespace ConsoleBot.Infrastructure.DataAccess
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<ToDoUser> _users = new();
        public void Add(ToDoUser user)
        {
            _users.Add(user);
        }

        public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return _users.FirstOrDefault(u => u.UserId == userId);
        }

        public async Task<ToDoUser?> GetUserAsync(long userId, CancellationToken cancellationToken)
        {
            return _users.FirstOrDefault(u => u.TelegramUserId == userId);
        }

        public async Task <ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            return _users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
        }

    }
}
