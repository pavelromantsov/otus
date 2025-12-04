namespace ConsoleBot.TelegramBot.Dto
{
    public class ToDoListCallbackDto : CallbackDto
    {
        public Guid? ToDoListId { get; set; }

        public ToDoListCallbackDto(string action, Guid? toDoListId) : base(action)
        {
            ToDoListId = toDoListId;
        }
        public static new ToDoListCallbackDto FromString(string input)
        {
            if (input.Contains("|"))
            {
                var parts = input.Split('|');
                return new ToDoListCallbackDto(parts[0], Guid.TryParse(parts[1], out var guid) ? guid : (Guid?)null);
            }
            else
            {
                return new ToDoListCallbackDto(input, null);
            }
        }
        public override string ToString()
        {
            return $"{base.ToString()}|{(ToDoListId.HasValue ? ToDoListId.Value.ToString() : "")}";
        }

    }

}