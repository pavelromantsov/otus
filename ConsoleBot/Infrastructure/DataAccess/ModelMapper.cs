using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess.Models;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public static class ModelMapper
    {
     
        public static ToDoUser MapFromModel(ToDoUserModel model)
        {
            if (model == null) return null!;

            return new ToDoUser
            {
                UserId = model.UserId,
                TelegramUserId = model.TelegramUserId,
                TelegramUserName = model.TelegramUserName,
                RegisteredAt = model.RegisteredAt
            };
        }

        public static ToDoUserModel MapToModel(ToDoUser entity)
        {
            if (entity == null) return null!;

            return new ToDoUserModel
            {
                UserId = entity.UserId,
                TelegramUserId = entity.TelegramUserId,
                TelegramUserName = entity.TelegramUserName,
                RegisteredAt = entity.RegisteredAt
            };
        }

        public static ToDoList MapFromModel(ToDoListModel model)
        {
            if (model == null) return null!;

            return new ToDoList
            {
                Id = model.Id,
                Name = model.Name,
                User = model.User != null ? MapFromModel(model.User) : null,  
                CreatedAt = model.CreatedAt
            };
        }

        public static ToDoListModel MapToModel(ToDoList entity)
        {
            if (entity == null) return null!;

            return new ToDoListModel
            {
                Id = entity.Id,
                UserId = entity.User?.UserId ?? Guid.Empty,  
                Name = entity.Name,
                CreatedAt = entity.CreatedAt
            };
        }

        public static ToDoItem MapFromModel(ToDoItemModel model)
        {
            if (model == null) return null!;

            return new ToDoItem
            {
                Id = model.Id,
                Name = model.Name,
                CreatedAt = model.CreatedAt,
                Deadline = model.Deadline,
                State = (ToDoItemState)model.State,  
                StateChangedAt = model.StateChangedAt,
                User = ModelMapper.MapFromModel(model.User), 
                List = model.List != null ? ModelMapper.MapFromModel(model.List) : null
            };
        }

        public static ToDoItemModel MapToModel(ToDoItem entity)
        {
            if (entity == null) return null!;

            return new ToDoItemModel
            {
                Id = entity.Id,
                Name = entity.Name,
                CreatedAt = entity.CreatedAt,
                Deadline = entity.Deadline,
                State = (int)entity.State,
                StateChangedAt = entity.StateChangedAt,
                UserId = entity.User.UserId,    
                ListId = entity.List?.Id
            };
        }

    }
}
