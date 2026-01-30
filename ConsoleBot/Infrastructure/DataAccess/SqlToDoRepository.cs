using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;
using LinqToDB;
using LinqToDB.Async;


namespace ConsoleBot.Infrastructure.DataAccess
{
    public class SqlToDoRepository : IToDoRepository
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public SqlToDoRepository(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var items = await db.ToDoItems
                .Where(i => i.UserId == userId)
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync(ct);

            return items.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var items = await db.ToDoItems
                .Where(i => i.UserId == userId && i.State == 0)
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .OrderBy(i => i.Deadline)
                .ToListAsync(ct);

            return items.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }

        public async Task AddAsync(ToDoItem item, CancellationToken ct)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Имя задачи не может быть пустым.", nameof(item.Name));

            using var db = _factory.CreateDataContext();
            var itemModel = ModelMapper.MapToModel(item);
            itemModel.Id = Guid.NewGuid();

            await db.InsertAsync(itemModel);

        }

        public async Task UpdateAsync(ToDoItem item, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var itemModel = ModelMapper.MapToModel(item);

            await db.ToDoItems
                .Where(i => i.Id == itemModel.Id)
                .Set(i => i.Name, itemModel.Name)
                .Set(i => i.Deadline, itemModel.Deadline)
                .Set(i => i.State, itemModel.State)
                .Set(i => i.StateChangedAt, itemModel.StateChangedAt)
                .UpdateAsync(ct);
        }

        public void Delete(Guid id, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();

            db.ToDoItems
                .Where(i => i.Id == id)
                .Delete();

            db.CommitTransaction();
        }

        public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            return await db.ToDoItems
                .AnyAsync(i => i.UserId == userId && i.Name == name, ct);
        }

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            return await db.ToDoItems
                .CountAsync(i => i.UserId == userId && i.State == 0, ct);
        }

        public async Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
        {
            var allItems = await GetActiveByUserIdAsync(userId, ct);
            var filtered = allItems.Where(predicate).ToList();
            return filtered.AsReadOnly();
        }

        public async Task<ToDoItem> GetAsync(Guid id, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            var item = await db.ToDoItems
                .LoadWith(i => i.User)
                .LoadWith(i => i.List)
                .LoadWith(i => i.List!.User)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            return ModelMapper.MapFromModel(item);
        }

        public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            ct.ThrowIfCancellationRequested();

            var models = await db.ToDoItems
                .Where(t => t.UserId == userId
                         && t.State == (int)ToDoItemState.Active
                         && t.Deadline >= from
                         && t.Deadline < to)
                .ToListAsync(ct);

            return models.Select(ModelMapper.MapFromModel).ToList().AsReadOnly();
        }
    }
}
