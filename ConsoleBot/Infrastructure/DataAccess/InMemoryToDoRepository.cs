using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class InMemoryToDoRepository : IToDoRepository
    {
        private readonly List<ToDoItem> _items = new List<ToDoItem>();

        public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
        {
            return _items.Where(i => i.User.UserId == userId).ToList();
        }

        public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
        {
            return _items.Where(i => i.User.UserId == userId && i.State == ToDoItemState.Active).ToList();
        }

        public ToDoItem? Get(Guid id)
        {
            return _items.FirstOrDefault(i => i.Id == id);
        }

        public void Add(ToDoItem item)
        {
            _items.Add(item);
        }

        public void Update(ToDoItem item)
        {
            var existingItem = _items.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                existingItem.Name = item.Name;
                existingItem.State = item.State;
                existingItem.StateChangedAt = item.StateChangedAt;
            }
        }

        public void Delete(Guid id)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _items.Remove(item);
            }
        }

        public bool ExistsByName(Guid userId, string name)
        {
            return _items.Any(i => i.User.UserId == userId && i.Name == name);
        }

        public int CountActive(Guid userId)
        {
            return _items.Count(i => i.User.UserId == userId && i.State == ToDoItemState.Active);
        }

        public IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate)
        {
            return _items.Where(task => task.User.UserId == userId && predicate(task)).ToList();
        }

    }
}
