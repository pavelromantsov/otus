using ConsoleBot.Core.DataAccess.Models;
using ConsoleBot.Core.Entities;
using ConsoleBot.Core.Services;
using ConsoleBot.Infrastructure.DataAccess;
using LinqToDB;
using LinqToDB.Async;

namespace ConsoleBot.Infrastructure
{
    public class NotificationService : INotificationService
    {
        private readonly IDataContextFactory<ToDoDataContext> _factory;

        public NotificationService(IDataContextFactory<ToDoDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<bool> ScheduleNotification(Guid userId, string type, string text, DateTime scheduledAt, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            ct.ThrowIfCancellationRequested();

            var exists = await db.Notifications
                .AnyAsync(n => n.UserId == userId && n.Type == type && !n.IsNotified, ct);

            if (exists) return false;

            var model = new NotificationModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Text = text,
                ScheduledAt = scheduledAt,
                IsNotified = false
            };

            await db.InsertAsync(model);
            return true;
        }

        public async Task<IReadOnlyList<Notification>> GetScheduledNotification(
            DateTime scheduledBefore,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var db = _factory.CreateDataContext();

            var models = await db.Notifications
                .LoadWith(n => n.User) 
                .Where(n => !n.IsNotified && n.ScheduledAt <= scheduledBefore)
                .ToListAsync(ct);

            return models
                .Select(ModelMapper.MapFromModel) 
                .ToList()
                .AsReadOnly();
        }

        public async Task MarkNotified(Guid notificationId, CancellationToken ct)
        {
            using var db = _factory.CreateDataContext();
            ct.ThrowIfCancellationRequested();

            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, ct)
                ?? throw new InvalidOperationException("Notification not found");

            notification.IsNotified = true;
            notification.NotifiedAt = DateTime.UtcNow;
            await db.UpdateAsync(notification);
        }
    }
}
