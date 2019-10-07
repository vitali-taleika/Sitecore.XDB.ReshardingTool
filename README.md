# Sitecore.XDB.ReshardingTool
This is the simple resharding tool for moving Sitecore XDB data from 'n' shards to 'm' shards. Tested on **Sitecore 9.1** version.
## Configurations
In the `appsettings.json` file: 
- add connections strings for source and target shard map managers
- Change batch size
- Change shard map names
- Configure from what date need to move interactions (format:`yyyy-MM-dd`)
- For `RESUME` mode need to configure `resharding.log` connection string 

**NOTE:** For create log database for `RESUME` mode use [Sitecore.XDB.ReshardingLog.sql](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/Sitecore.XDB.ReshardingLog.sql) file.
```
{
  "AppSettings": {
    "RetryCount": 3,
    "RetryDelay": 1000,
    "BatchSize": 100000,
    "ConnectionTimeout": 300,
    "ContactIdShardMap": "ContactIdShardMap",
    "DeviceProfileIdShardMap": "DeviceProfileIdShardMap",
    "ContactIdentifiersIndexShardMap": "ContactIdentifiersIndexShardMap",
    "InteractionsFromDate": "2019-09-01",
    "InteractionsFilterByEventDefinitions": [ "2a65acc5-9851-40dd-851b-23f7a6c53092", "0fd3ef44-6c4a-40ce-8f97-6197bd9c61f2" ]
  },
  "ConnectionStrings": {
    "collection.source": "user id=sa;password=Password1!;data source=.\\SQLENTERPRISE;Initial Catalog=test1_Xdb.Collection.ShardMapManager",
    "collection.target": "user id=sa;password=Password1!;data source=.\\SQLENTERPRISE;Initial Catalog=test2_Xdb.Collection.ShardMapManager",
    "resharding.log": "user id=sa;password=Password1!;data source=.\\SQLENTERPRISE;Initial Catalog=Sitecore.XDB.ReshardingLog"
  }
}
```
## How to use
**1.** When the `appsettings.json` file is configured just run `...\ToolReleases\win-x64> .\Sitecore.XDB.ReshardingTool.exe` and choose a command that you want.
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/Sitecore.XDB.ReshardingTool_use.png)

**NOTE:** for see details open `log{date}.txt` log file.

**2.** When the resharding process will be done start reindexing but need to **clean up index cores** before or you will get `IncompatibleSyncTokensException` exceptions.
```
[Error] Failed indexing next set of changes. There will be an attempt to recover from the failure.
Sitecore.Xdb.Collection.Failures.IncompatibleSyncTokensException: Tokens are incompatible, they have different set of shards.
   at Sitecore.Xdb.Collection.Data.SqlServer.Managers.ChangeTracking.SyncToken.IsUpToDate(ISyncToken syncToken)
   at Sitecore.Xdb.Collection.Data.SqlServer.SqlDataProvider.<GetChanges>d__16.MoveNext()
--- End of stack trace from previous location where exception was thrown ---
```

## How to install `m` shards
Download [Installation files](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/tree/master/Shards.Install). Update installation params in `CreateXDB.ps1`.

**NOTE:** Installations files were tested on `SIF 2.1.0`.

`CreateXDB.json` is configured for 4 shards, you only need to update the `CreateXDB.json` file if you want to add more shards.
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/shards_variables.png)
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/shards_db_user.png)
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/shard_remove_dbs.png)

 
