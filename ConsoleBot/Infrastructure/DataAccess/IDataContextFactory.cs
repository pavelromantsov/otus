using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace ConsoleBot.Infrastructure.DataAccess
{
    public interface IDataContextFactory<TDataContext> where TDataContext : DataConnection
    {
        TDataContext CreateDataContext();
    }
}
