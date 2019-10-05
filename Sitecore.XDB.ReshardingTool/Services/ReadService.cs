using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;

namespace Sitecore.XDB.ReshardingTool.Services
{
    public class ReadService : BaseDataService
    {
        private int _connectionTimeout;
        private int _retryCount;
        private int _retryDelay;

        public ReadService(int connectionTimeout, int retryCount, int retryDelay)
        {
            _connectionTimeout = connectionTimeout;
            _retryCount = retryCount;
            _retryDelay = retryDelay;
        }

        public async Task<int> GetEntityesCountAsync<T>(IEnumerable<Shard> shards, string shardMapManagerConnectionString, string tableName, string where = null, string tableShortName = null) where T : new()
        {
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            int result = 0;
            var query = $"SELECT COUNT(*) as count FROM {tableName} {tableShortName} {where}";

            using (MultiShardConnection conn = new MultiShardConnection(shards, Credentials(shardMapManagerConnectionString)))
            {
                using (var cmd = CreateMultipleShardsCommand(conn, CommandType.Text, query))
                {
                    cmd.CommandTimeout = _connectionTimeout;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result += (int)reader["count"];
                        }
                    }
                }
            }
#if DEBUG
            stopwatch.Stop();
            Console.WriteLine($"GetEntityesCountAsync => Time elapsed: {stopwatch.Elapsed}");
#endif
            return result;
        }

        public async Task<IList<T>> GetEntityesAsync<T>(Shard shard, string shardMapManagerConnectionString, string tableName, string orderFieldName, Guid? lastSubmitted, int pageSize, string where = null, string tableShortName = null) where T : new()
        {
            var properties = typeof(T).GetProperties().ToList();
            var fieldPrefix = !string.IsNullOrWhiteSpace(tableShortName) ? $"{tableShortName}." : "";
            var select = string.Join(",", properties.Select(x => $"{fieldPrefix}[{x.Name}]"));

            if (lastSubmitted != null)
            {
                if (where != null)
                {
                    where += $"AND {fieldPrefix}{orderFieldName} > '{lastSubmitted:D}'";
                }
                else
                {
                    where += $"WHERE {fieldPrefix}{orderFieldName} > '{lastSubmitted:D}'";
                }
            }

            var query = $"SELECT TOP {pageSize} {select} FROM {tableName} {tableShortName} {where} ORDER BY {fieldPrefix}{orderFieldName}";

            var connectionString = new SqlConnectionStringBuilder(Credentials(shardMapManagerConnectionString))
            {
                DataSource = shard.Location.DataSource,
                InitialCatalog = shard.Location.Database
            }.ConnectionString;

            IList<T> result;

            result = await ExecuteWithRetry<T>(connectionString, query, properties);


            return result;
        }

        private async Task<IList<T>> ExecuteWithRetry<T>(string connectionString, string query, IList<PropertyInfo> properties) where T : new()
        {
            var result = new List<T>();
            var isSucceeded = false;
            var retryCountCurrent = 0;

            while (!isSucceeded && retryCountCurrent < _retryCount)
            {
                try
                {
#if DEBUG
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
#endif
                    using (var conn = new SqlConnection(connectionString))
                    {
                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.CommandTimeout = _connectionTimeout;
                            await conn.OpenAsync();
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    result.Add(Map<T>(properties, reader));
                                }
                            }
                        }
                    }

                    isSucceeded = true;
#if DEBUG
                    stopwatch.Stop();
                    Console.WriteLine($"GetEntityesAsync => Time elapsed: {stopwatch.Elapsed}");
#endif

                }
                catch (Exception e)
                {
                    retryCountCurrent++;
#if DEBUG
                    Console.WriteLine($"retry #: {retryCountCurrent}");
                    Console.WriteLine(e);
#endif
                    if (retryCountCurrent < _retryCount)
                    {
                        await Task.Delay(_retryDelay * retryCountCurrent);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return result;
        }

        private T Map<T>(IList<PropertyInfo> properties, DbDataReader reader) where T : new()
        {
            var entity = new T();
            foreach (var prop in properties)
            {
                SetValue(prop, entity, reader[prop.Name]);
            }

            return entity;
        }

        private void SetValue(PropertyInfo prop, object entity, object value)
        {
            if (prop.PropertyType == typeof(Guid?))
            {
                prop.SetValue(entity, value == DBNull.Value ? null : (Guid?)value, null);
            }
            else if (prop.PropertyType == typeof(DateTime))
            {
                prop.SetValue(entity, DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc), null);
            }
            else if (prop.PropertyType == typeof(DateTime?))
            {
                prop.SetValue(entity, value == DBNull.Value ? (DateTime?)null : DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc), null);
            }
            else
            {
                prop.SetValue(entity, value, null);
            }
        }
    }
}