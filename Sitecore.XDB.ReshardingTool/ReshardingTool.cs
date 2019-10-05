using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using Serilog;
using Sitecore.XDB.ReshardingTool.Models;
using Sitecore.XDB.ReshardingTool.Services;

namespace Sitecore.XDB.ReshardingTool
{
    public class ReshardingTool
    {
        private readonly ILogger _logger;

        private readonly ReadService _readService;
        private readonly WriteService _writeService;
        private readonly LogService _logService;

        private readonly int _batchSize;
        private readonly bool _isResumeMode;

        private readonly string _sourceConn;
        private readonly string _targetConn;

        private readonly string _contactMap;
        private readonly string _deviceMap;
        private readonly string _identifiersMap;


        private readonly ShardMapManager _sourceShardMapManager;
        private readonly ShardMapManager _targetShardMapManager;
        public ReshardingTool(
            ILogger logger,
            string sourceConn,
            string targetConn,
            string logConn,
            string contactMap,
            string deviceMap,
            string identifiersMap,
            bool isResumeMode,
            int connectionTimeout,
            int batchSize = 10000,
            int retryCount = 3,
            int retryDelay = 1000
            )
        {
            _logger = logger;

            _readService = new ReadService(connectionTimeout, retryCount, retryDelay);
            _writeService = new WriteService(connectionTimeout);

            _sourceConn = sourceConn;
            _targetConn = targetConn;

            _contactMap = contactMap;
            _deviceMap = deviceMap;
            _identifiersMap = identifiersMap;

            _batchSize = batchSize;
            _isResumeMode = isResumeMode;
            _logService = new LogService(logConn, _isResumeMode);

            _sourceShardMapManager = InitializeShardMapManager(_sourceConn);
            _targetShardMapManager = InitializeShardMapManager(_targetConn);
        }

        public async Task RunAsync(DateTime? interactionsFromDate, IList<string> interactionsFilterByEventDefinitions, CancellationToken token)
        {
            _logger.Information($"Batch Size: {_batchSize}");

            _logger.Information("Contacts:");
            await ProcessEnttity<Contact>(_contactMap, "[xdb_collection].[Contacts]", nameof(Contact.ContactId), token);

            _logger.Information("ContactFacets:");
            await ProcessEnttity<ContactFacet>(_contactMap, "[xdb_collection].[ContactFacets]", nameof(ContactFacet.ContactId), token);

            _logger.Information("Interactions");
            string interactionsWhere = null;
            if (interactionsFromDate != null)
                interactionsWhere = $" WHERE i.Created > '{interactionsFromDate:yyyy-MM-dd HH:mm:ss.fff}' ";

            if (interactionsFilterByEventDefinitions != null && interactionsFilterByEventDefinitions.Any())
            {
                var term = $"({string.Join(" or ", interactionsFilterByEventDefinitions.Select(x => $"[Events] like '%\"DefinitionId\":\"{x}\"%'"))})";
                interactionsWhere += !string.IsNullOrWhiteSpace(interactionsWhere) ? $" AND {term}" : $" WHERE {term}";
            }

            await ProcessEnttity<Interaction>(_contactMap, "[xdb_collection].[Interactions]", nameof(Interaction.InteractionId), token, interactionsWhere, "i");
            _logger.Information("InteractionFacet:");
            string interactionsJoin = null;
            if (interactionsFromDate != null || interactionsFilterByEventDefinitions != null && interactionsFilterByEventDefinitions.Any())
                interactionsJoin = " JOIN [xdb_collection].[Interactions] i on i.InteractionId = f.InteractionId ";
            await ProcessEnttity<InteractionFacet>(_contactMap, "[xdb_collection].[InteractionFacets]", nameof(InteractionFacet.InteractionId), token, interactionsJoin + interactionsWhere, "f");

            _logger.Information("ContactIdentifiers:");
            await ProcessEnttity<ContactIdentifier>(_identifiersMap, "[xdb_collection].[ContactIdentifiers]", nameof(ContactIdentifier.ContactId), token);
            _logger.Information("ContactIdentifiersIndex:");
            await ProcessEnttity<ContactIdentifiersIndex>(_identifiersMap, "[xdb_collection].[ContactIdentifiersIndex]", nameof(ContactIdentifiersIndex.ContactId), token);

            _logger.Information("DeviceProfiles:");
            await ProcessEnttity<DeviceProfile>(_deviceMap, "[xdb_collection].[DeviceProfiles]", nameof(DeviceProfile.DeviceProfileId), token);
            _logger.Information("DeviceProfileFacets:");
            await ProcessEnttity<DeviceProfileFacet>(_deviceMap, "[xdb_collection].[DeviceProfileFacets]", nameof(DeviceProfileFacet.DeviceProfileId), token);
        }

        private async Task ProcessEnttity<T>(
            string rangeMapName,
            string tableName,
            string orderFieldName,
            CancellationToken token,
            string where = null,
            string tableShortName = null)
            where T : IEntity, new()
        {
            var sourceShardMap = GetRangeShardMap(_sourceShardMapManager, rangeMapName);
            var sourceShards = sourceShardMap.GetShards().ToList();
            var targetShardMap = GetRangeShardMap(_targetShardMapManager, rangeMapName);
            var mappings = targetShardMap.GetMappings();

#if DEBUG
            var counter = 0;
            var count = await _readService.GetEntityesCountAsync<T>(sourceShards, _sourceConn, tableName, where, tableShortName);
            LogInfo($"{typeof(T).Name} count: {count}");
#endif

            foreach (var sourceShard in sourceShards)
            {
                var sourceShardId = (Guid)GetPropValue(sourceShard, "Id", BindingFlags.Instance | BindingFlags.NonPublic);
                LogInfo($"Reading data from shard: {sourceShard.Location.Database}");

                var lastSubmittedIds = new List<SqlGuid>();
                if (_isResumeMode)
                {
                    var lastSubmittedId = await _logService.GetLastSubmittedId<T>(sourceShardId);
                    if (lastSubmittedId != null)
                        lastSubmittedIds.Add(new SqlGuid(lastSubmittedId.Value));
                }

                var page = 0;
                var isProcessed = false;

                while (!isProcessed)
                {
                    var entities = await _readService.GetEntityesAsync<T>(sourceShard, _sourceConn, tableName, orderFieldName, lastSubmittedIds.Any() ? lastSubmittedIds.OrderBy(x => x).LastOrDefault().Value : (Guid?)null, _batchSize, where, tableShortName);
                    lastSubmittedIds.Clear();
                    if (entities.Any())
                    {
                        var splitedToShards = SplitToShards(mappings, entities);
                        foreach (var splitedToShard in splitedToShards)
                        {
                            var sortedEntities = splitedToShard.Value.OrderBy(x => x.GetOrderFieldValue()).ToList();
                            var last = sortedEntities.Last();
                            await _writeService.BulkInsertAsync(conn => splitedToShard.Key.OpenConnection(conn), sortedEntities, _targetConn, tableName, _batchSize);
#if DEBUG
                            counter += sortedEntities.Count;
                            LogInfo($"{typeof(T).Name} counter: {counter}");
#endif
                            lastSubmittedIds.Add(last.GetOrderFieldValue());
                            if (_isResumeMode)
                                await _logService.Log<T>((Guid)GetPropValue(last, orderFieldName), sourceShardId);
                        }

                        LogInfo($"{typeof(T).Name} bulk insert #{page}");
                        page++;
                    }

                    if (!entities.Any() || entities.Count < _batchSize)
                        isProcessed = true;

                    token.ThrowIfCancellationRequested();
                }
            }
        }

        private object GetPropValue(object src, string propName, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return src.GetType().GetProperty(propName, flags)?.GetValue(src, null);
        }

        private ShardMapManager InitializeShardMapManager(string shardMapManagerConnectionString)
        {
            if (ShardMapManagerFactory.TryGetSqlShardMapManager(shardMapManagerConnectionString, ShardMapManagerLoadPolicy.Eager, out var shardMapManager))
                return shardMapManager;
            throw new Exception("The shard map manager has to be configured.");
        }

        private RangeShardMap<byte[]> GetRangeShardMap(ShardMapManager shardMapManager, string rangeShardMapName)
        {
            if (!shardMapManager.TryGetRangeShardMap(rangeShardMapName, out RangeShardMap<byte[]> shardMap))
                throw new Exception("Scaled database was not configured properly: the range shard map has to be configured.");
            return shardMap;
        }

        private IDictionary<Shard, IEnumerable<T>> SplitToShards<T>(IReadOnlyList<RangeMapping<byte[]>> mappings, IEnumerable<T> entities) where T : IEntity
        {
            var result = new List<Tuple<Shard, T>>();
            foreach (var entity in entities)
            {
                var key = entity.GetKey();
                var rangeMapping = mappings.First(x => RangeContains(x.Value, key));
                result.Add(new Tuple<Shard, T>(rangeMapping.Shard, entity));
            }
            return result.GroupBy(x => x.Item1).ToDictionary(x => x.Key, x => x.Select(v => v.Item2));
        }

        private static bool RangeContains<T>(Range<T> range, T key)
        {
            ShardKey key1 = new ShardKey(ShardKey.ShardKeyTypeFromType(typeof(T)), key);
            return new ShardRange(new ShardKey(ShardKey.ShardKeyTypeFromType(typeof(T)), range.Low), new ShardKey(ShardKey.ShardKeyTypeFromType(typeof(T)), range.High)).Contains(key1);
        }

        private void LogInfo(string msg)
        {
            _logger.Information(msg);
#if DEBUG
            Console.WriteLine(msg);
#endif
        }
    }
}