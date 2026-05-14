using Npgsql;

namespace LashAccountingSystem.Database
{
    public static class DbConnection
    {
        private static string connectionString = "Host=localhost;Port=5432;Database=LashMasterDatabase;Username=postgres;Password=Anna21032517";

        public static string ConnectionString => connectionString;

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }

        public static bool HasUsers()
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM users";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}