using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.Services
{
    public class UserService : IUserService, IUserRepository
    {
        //private readonly IDictionary<long, ToDoUser> _users = new Dictionary<long, ToDoUser>();
        private readonly IUserRepository _repository;

        public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
        {
            var existingUser = _repository.GetUserByTelegramUserId(telegramUserId);
            if (existingUser != null)
            {
                return existingUser;
            }

            var newUser = new ToDoUser(telegramUserId, telegramUserName);
            _repository.Add(newUser);
            return newUser;
        }

        public ToDoUser? GetUser(long telegramUserId)
        {
            return _repository.GetUserByTelegramUserId(telegramUserId);
        }

        public ToDoUser? GetUser(Guid userId)
        {
            throw new NotImplementedException();
        }

        public ToDoUser? GetUserByTelegramUserId(long telegramUserId)
        {
            throw new NotImplementedException();
        }

        public void Add(ToDoUser user)
        {
            throw new NotImplementedException();
        }

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

    }
}