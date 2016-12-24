using System.Data.Common;

namespace App.Infrastructure
{
    public interface IDbFactory
    {
        string ConnectionString { get; }

        DbConnection CreateDbConnection();
    }
}
