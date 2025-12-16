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
            var parts = input.Split('|');
            if (parts.Length < 2)
                throw new ArgumentException("Некорректный формат callbackData для ToDoListCallbackDto.");

            var action = parts[0];

            Guid? listId = null;
            if (!string.IsNullOrEmpty(parts[1]) && Guid.TryParse(parts[1], out var parsed))
                listId = parsed;

            return new ToDoListCallbackDto(action, listId);
        }
        public override string ToString()
        {
            return $"{base.ToString()}|{(ToDoListId.HasValue ? ToDoListId.Value.ToString() : "")}";
        }

    }

}