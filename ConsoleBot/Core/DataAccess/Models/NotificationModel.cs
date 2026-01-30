using ConsoleBot.Core.Entities;
using LinqToDB.Mapping;

namespace ConsoleBot.Core.DataAccess.Models
{
    [Table("Notifications")]
    public class NotificationModel
    {
        [PrimaryKey, Column("id"), Identity]
        public Guid Id { get; set; }

        [Column("user_id"), NotNull]
        public Guid UserId { get; set; }

        [Column("type"), NotNull]
        public string Type { get; set; } = null!;

        [Column("text")]
        public string Text { get; set; } = null!;

        [Column("scheduled_at"), NotNull]
        public DateTime ScheduledAt { get; set; }

        [Column("is_notified")]
        public bool IsNotified { get; set; }

        [Column("notified_at")]
        public DateTime? NotifiedAt { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUser.UserId))]
        public ToDoUser User { get; set; } = null!;
    }
}
