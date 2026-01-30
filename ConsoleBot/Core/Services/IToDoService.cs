using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.Services
{
    public interface IToDoService
    {
        Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, ToDoList? list, CancellationToken ct);
        Task<int> CountActiveAsync(ToDoUser user, CancellationToken ct);
        void Delete(Guid id, CancellationToken ct);
        Task<bool> ExistsByNameAsync(ToDoUser user, string name, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken ct);
        Task MarkCompletedAsync(Guid id, CancellationToken ct);
        int ParseAndValidateInt(string? str, int min, int max, CancellationToken ct);
        Task ValidateStringAsync(string? str, CancellationToken ct);
        Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct);
        Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct);
    }

}