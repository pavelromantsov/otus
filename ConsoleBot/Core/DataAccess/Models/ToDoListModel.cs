using LinqToDB.Mapping;

namespace ConsoleBot.Core.DataAccess.Models
{
    [Table("ToDoList")]
    public class ToDoListModel
    {
        [PrimaryKey, Column("Id")]
        public Guid Id { get; set; }

        [Column("UserId"), NotNull]
        public Guid UserId { get; set; }

        [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId))]
        public ToDoUserModel User { get; set; } = null!;

        [Column("Name", Length = 500), NotNull]
        public string Name { get; set; } = null!;

        [Column("CreatedAt"), NotNull]
        public DateTime CreatedAt { get; set; }
    }
}
