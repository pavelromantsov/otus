namespace ConsoleBot.Core.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ToDoUser User { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Text { get; set; } = null!;
        public DateTime ScheduledAt { get; set; }
        public bool IsNotified { get; set; }
        public DateTime? NotifiedAt { get; set; }
    }
}
