using ConsoleBot.Core.DataAccess;
using ConsoleBot.Core.Services;
using Telegram.Bot;

namespace ConsoleBot.BackgroundTasks
{
    public class NotificationBackgroundTask : BackgroundTask
    {
        private readonly INotificationService _notificationService;
        private readonly ITelegramBotClient _bot;
        private readonly IUserRepository _userRepository;  

        public NotificationBackgroundTask(
            INotificationService notificationService,
            IUserRepository userRepository, 
            ITelegramBotClient bot)
            : base(TimeSpan.FromMinutes(1), nameof(NotificationBackgroundTask))
        {
            _notificationService = notificationService;
            _userRepository = userRepository;
            _bot = bot;
        }

        protected override async Task Execute(CancellationToken ct)
        {
            var notifications = await _notificationService.GetScheduledNotification(DateTime.UtcNow, ct);

            foreach (var notification in notifications)
            {
                try
                {
                    var user = await _userRepository.GetUserAsync(notification.UserId, ct);
                    if (user == null) continue;

                    await _bot.SendMessage(  
                        chatId: user.TelegramUserId,  
                        text: notification.Text,
                        cancellationToken: ct);

                    await _notificationService.MarkNotified(notification.Id, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось отправить уведомление {notification.Id}: {ex.Message}");
                }
            }
        }
    }
}
