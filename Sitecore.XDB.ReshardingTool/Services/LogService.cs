using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Sitecore.XDB.ReshardingTool.Services
{
    public class LogService
    {
        private readonly string _connectionString;
        private readonly bool _isEnabled;
        public LogService(string connectionString, bool isEnabled)
        {
            _connectionString = connectionString;
            _isEnabled = isEnabled;
        }

        public async Task Log<T>(Guid id, Guid shardId)
        {
            if (!_isEnabled)
                return;

            var query = "INSERT INTO [dbo].[Log] (Entity, LastSubmittedId, ShardId) VALUES (@Entity, @LastSubmittedId, @ShardId)";
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Entity", typeof(T).Name);
                    cmd.Parameters.AddWithValue("@LastSubmittedId", id);
                    cmd.Parameters.AddWithValue("@ShardId", shardId);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Guid?> GetLastSubmittedId<T>(Guid shardId)
        {
            if (!_isEnabled)
                return null;

            Guid? result = null;
            var query = @"SELECT TOP 1 * FROM [dbo].[Log] WHERE [Entity] = @Entity AND ShardId = @ShardId ORDER BY LastSubmittedId DESC";
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Entity", typeof(T).Name);
                    cmd.Parameters.AddWithValue("@ShardId", shardId);

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result = (Guid?)reader["LastSubmittedId"];
                        }
                    }
                }
            }

            return result;
        }
    }
}