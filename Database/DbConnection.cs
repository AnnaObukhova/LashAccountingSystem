using Npgsql;

namespace LashAccountingSystem.Database
{
    public static class DbConnection
    {
        private static string connectionString = "Host=localhost;Port=5432;Database=LashMasterDatabase;Username=postgres;Password=Anna21032517";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}