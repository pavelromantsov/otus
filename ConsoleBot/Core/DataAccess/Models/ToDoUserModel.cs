using LinqToDB.Mapping;

namespace ConsoleBot.Core.DataAccess.Models
{
    [Table("ToDoUser")]
    public class ToDoUserModel
    {
        [PrimaryKey, Column("UserId")]
        public Guid UserId { get; set; }

        [Column("TelegramUserId"), NotNull]
        public long TelegramUserId { get; set; }

        [Column("TelegramUserName", Length = 255)]
        public string? TelegramUserName { get; set; }

        [Column("RegisteredAt"), NotNull]
        public DateTime RegisteredAt { get; set; }
    }
}
