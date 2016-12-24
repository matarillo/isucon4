using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace App.Infrastructure
{
    public class MySqlDbFactory : IDbFactory
    {
        public MySqlDbFactory(IConfiguration config)
        {
            var host = config["ISU4_DB_HOST"] ?? "localhost";
            var port = uint.TryParse(config["ISU4_DB_PORT"], out uint port_) ? port_ : 3306u;
            var dbname = config["ISU4_DB_NAME"] ?? "isu4_qualifier";
            var username = config["ISU4_DB_USER"] ?? "root";
            var password = config["ISU4_DB_PASSWORD"] ?? "";
            var builder = new MySqlConnectionStringBuilder();
            builder.UserID = username;
            builder.Password = password;
            builder.Database = dbname;
            builder.Server = host;
            builder.Port = port;
            this.ConnectionString = builder.ConnectionString;
        }

        public MySqlDbFactory(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public string ConnectionString { get; private set; }

        public DbConnection CreateDbConnection()
        {
            return new MySqlConnection(this.ConnectionString);
        }
    }
}
