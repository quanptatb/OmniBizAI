using Microsoft.Data.SqlClient;

namespace OmniBizAI.Data;

internal static class ConnectionStringResolver
{
    public static string GetDefaultConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string: ConnectionStrings:DefaultConnection");
        }

        var dbPassword = configuration["DB_PASSWORD"];

        if (string.IsNullOrWhiteSpace(dbPassword))
        {
            return connectionString;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);

        if (!string.IsNullOrEmpty(builder.Password))
        {
            return connectionString;
        }

        builder.Password = dbPassword;
        return builder.ConnectionString;
    }
}
