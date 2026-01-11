using LinqToDB.Mapping;

namespace ConsoleBot.Core.DataAccess.Models
{
    [Table("ToDoItem")]
    public class ToDoItemModel
    {
        [PrimaryKey, Column("Id")]
        public Guid Id { get; set; }

        [Column("UserId"), NotNull]
        public Guid UserId { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId))]
        public ToDoUserModel User { get; set; } = null!;

        [Column("ListId")]
        public Guid? ListId { get; set; }

        [Association(ThisKey = nameof(ListId), OtherKey = nameof(ToDoListModel.Id))]
        public ToDoListModel? List { get; set; }

        [Column("Name", Length = 500), NotNull]
        public string Name { get; set; } = null!;

        [Column("CreatedAt"), NotNull]
        public DateTime CreatedAt { get; set; }

        [Column("Deadline"), NotNull]
        public DateTime Deadline { get; set; }

        [Column("State"), NotNull]
        public int State { get; set; }

        [Column("StateChangedAt")]
        public DateTime? StateChangedAt { get; set; }
    }
}
