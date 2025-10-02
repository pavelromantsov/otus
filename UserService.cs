using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBotCommands
{
    public class UserService : IUserService
    {
        private readonly IDictionary<long, ToDoUser> _users = new Dictionary<long, ToDoUser>();

        public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
        {
            var user = new ToDoUser(telegramUserId, telegramUserName);
            _users.Add(telegramUserId, user);
            return user;
        }

        public ToDoUser? GetUser(long telegramUserId)
        {
            return _users.TryGetValue(telegramUserId, out var user) ? user : null;
        }
    }
}
