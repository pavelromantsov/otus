using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public class DataContextFactory : IDataContextFactory<ToDoDataContext>
    {
        private readonly string _connectionString;

        public DataContextFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ToDoList")
                ?? throw new InvalidOperationException("Connection string 'ToDoList' not found in configuration.");
        }

        public ToDoDataContext CreateDataContext()
        {
            try
            {
                var context = new ToDoDataContext(_connectionString);
                return context;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
