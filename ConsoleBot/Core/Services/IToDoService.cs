using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Exceptions;
using ConsoleBot.Core.Services;

namespace ConsoleBot.Core.Services
{
    public interface IToDoService
    {
        IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId);
        IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix);
        IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId);
        ToDoItem Add(ToDoUser user, string name);
        void MarkCompleted(Guid id);
        void Delete(Guid id);
        int CountActive(Guid userId);
        int ParseAndValidateInt(string? str, int min, int max);
        void ValidateString(string? str);
    }

}