namespace ConsoleBot.Core.Entities
{
    public class ToDoItem
    {
        public Guid Id { get; set; }
        public ToDoUser User { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public ToDoItemState State { get; set; }
        public DateTime? StateChangedAt { get; set; }

        public ToDoItem(ToDoUser user, string name, DateTime deadline, CancellationToken cancellationToken)
        {
            Id = Guid.NewGuid();
            User = user;
            Name = name;
            CreatedAt = DateTime.UtcNow;
            Deadline = deadline;
            State = ToDoItemState.Active;
            StateChangedAt = null;
        }

        public ToDoItem()
        {
        }
    }
    public enum ToDoItemState
    {
        Active,
        Completed
    }
}