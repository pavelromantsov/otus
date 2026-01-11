using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleBot.Core.DataAccess.Models;
using LinqToDB.Data;
using LinqToDB;
using LinqToDB.Linq;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class ToDoDataContext : DataConnection
    {
        public ToDoDataContext(string connectionString)
            : base(ProviderName.PostgreSQL, connectionString)
        {
        }

        public ITable<ToDoUserModel> ToDoUsers => this.GetTable<ToDoUserModel>();
        public ITable<ToDoListModel> ToDoLists => this.GetTable<ToDoListModel>();
        public ITable<ToDoItemModel> ToDoItems => this.GetTable<ToDoItemModel>();
    }
}
