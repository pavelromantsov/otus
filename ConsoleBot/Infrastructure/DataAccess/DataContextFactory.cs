using Microsoft.Extensions.Configuration;

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
