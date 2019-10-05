using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Azure.SqlDatabase.ElasticScale.Query;

namespace Sitecore.XDB.ReshardingTool.Services
{
    public abstract class BaseDataService
    {
        protected string Credentials(string connectionString)
        {
            var source = new SqlConnectionStringBuilder(connectionString);
            var result = new SqlConnectionStringBuilder();
            if (source.IntegratedSecurity)
            {
                result.IntegratedSecurity = source.IntegratedSecurity;
            }
            else
            {
                result.UserID = source.UserID;
                result.Password = source.Password;
            }
            return result.ToString();
        }

        public DbCommand CreateMultipleShardsCommand(MultiShardConnection connection, CommandType commandType, string commandText, Action<MultiShardCommand> parameters)
        {
            MultiShardCommand command = connection.CreateCommand();
            command.CommandType = commandType;
            command.CommandText = commandText;
            command.ExecutionOptions = MultiShardExecutionOptions.None;
            command.ExecutionPolicy = MultiShardExecutionPolicy.CompleteResults;
            parameters?.Invoke(command);
            return command;
        }

        public DbCommand CreateMultipleShardsCommand(MultiShardConnection connection, CommandType commandType, string commandText)
        {
            return CreateMultipleShardsCommand(connection, commandType, commandText, null);
        }
    }
}