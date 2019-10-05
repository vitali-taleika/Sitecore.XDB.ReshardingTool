using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Sitecore.XDB.ReshardingTool.Services
{
    public class WriteService : BaseDataService
    {
        private int _connectionTimeout;

        public WriteService(int connectionTimeout)
        {
            _connectionTimeout = connectionTimeout;
        }

        public async Task BulkInsertAsync<T>(Func<string, SqlConnection> sqlConnection, IList<T> entities, string shardMapManagerConnectionString, string tableName, int batchSize)
        {
            using (var conn = sqlConnection(Credentials(shardMapManagerConnectionString)))
            {
                var numberOfPages = entities.Count / batchSize + (entities.Count % batchSize == 0 ? 0 : 1);
                for (var pageIndex = 0; pageIndex < numberOfPages; pageIndex++)
                {
                    var dataTable = ToDataTable(entities.Skip(pageIndex * batchSize).Take(batchSize));

                    var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction, null)
                    {
                        DestinationTableName = tableName,
                        BulkCopyTimeout = _connectionTimeout
                    };

                    await bulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }

        public static DataTable ToDataTable<T>(IEnumerable<T> data)
        {
            PropertyDescriptorCollection properties =
                TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }
    }
}