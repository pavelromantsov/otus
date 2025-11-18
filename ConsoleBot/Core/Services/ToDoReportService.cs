using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Entities;

namespace ConsoleBot.Core.Services
{
    public class ToDoReportService : IToDoReportService
    {
        private readonly IToDoRepository _todoRepository;

        public ToDoReportService(IToDoRepository todoRepository)
        {
            _todoRepository = todoRepository;
        }

        public async Task< (int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var allTasks = await _todoRepository.GetAllByUserIdAsync(userId, cancellationToken);
            var total = allTasks.Count;
            var completed = allTasks.Count(t => t.State == ToDoItemState.Completed);
            var active = allTasks.Count(t => t.State == ToDoItemState.Active);
            var now = DateTime.Now;

            return (total, completed, active, now);
        }
    }
}
