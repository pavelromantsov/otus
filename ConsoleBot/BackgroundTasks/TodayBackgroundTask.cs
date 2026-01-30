using System.Text;
using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Services;

namespace ConsoleBot.BackgroundTasks
{
    public class TodayBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IToDoRepository _toDoRepository;

        public TodayBackgroundTask(
            INotificationService notificationService,
            IUserRepository userRepository,
            IToDoRepository toDoRepository)
            : base(TimeSpan.FromDays(1), nameof(TodayBackgroundTask))
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _toDoRepository = toDoRepository;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var users = await _userRepository.GetUsers(ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = today.ToDateTime(TimeOnly.MinValue);
            var to = today.ToDateTime(TimeOnly.MaxValue);

            foreach (var user in users)
            {
                var tasksForToday = await _toDoRepository.GetActiveWithDeadline(
                    user.UserId,
                    from,
                    to,
                    ct);

                if (tasksForToday.Count == 0)
                    continue;

                var sb = new StringBuilder();
                sb.AppendLine("Ваши задачи на сегодня:");
                foreach (var task in tasksForToday)
                {
                    sb.AppendLine($"• {task.Name}");
                }

                var type = $"Today_{today}";
                var text = sb.ToString();

                await _notificationService.ScheduleNotification(
                    userId: user.UserId,
                    type: type,
                    text: text,
                    scheduledAt: DateTime.UtcNow,
                    ct);
            }
        }
    }
}
