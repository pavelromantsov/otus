using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using LinqToDB.Async;
using LinqToDB;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class SqlToDoListRepository : IToDoListRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlToDoListRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var listModel = await db.ToDoLists
                .LoadWith(l => l.User) 
                .FirstOrDefaultAsync(l => l.Id == id, ct);

            return listModel != null ? ModelMapper.MapFromModel(listModel) : null;
        }

        public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var lists = await db.ToDoLists
                .Where(l => l.UserId == userId)
                .LoadWith(l => l.User)
                .OrderBy(l => l.Name)
                .ToListAsync(ct);

            return lists.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task Add(ToDoList list, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var listModel = ModelMapper.MapToModel(list);
            listModel.Id = Guid.NewGuid();
            await db.InsertAsync(listModel);
        }

        public async Task Delete(Guid id, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            await db.ToDoLists
                .Where(l => l.Id == id)
                .DeleteAsync(ct);
        }

        public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            return await db.ToDoLists
                .AnyAsync(l => l.UserId == userId && l.Name == name, ct);
        }
    }
}
