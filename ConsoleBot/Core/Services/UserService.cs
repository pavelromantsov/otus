using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using Otus.ToDoList.ConsoleBot.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBot.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly List<ToDoUser> _users = new List<ToDoUser>();

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

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
            return _repository.GetUser(userId);
        }

        public ToDoUser? GetUserByTelegramUserId(long telegramUserId)
        {
            return _repository.GetUserByTelegramUserId(telegramUserId);
        }

        public void Add(ToDoUser user) 
        {
            {
                if (_users.Any(u => u.UserId == user.UserId))
                {
                    throw new InvalidOperationException("Пользователь с таким идентификатором уже существует.");
                }
                _users.Add(user);
            }
        }
    }
}