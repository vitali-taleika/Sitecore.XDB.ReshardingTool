# Sitecore.XDB.ReshardingTool
This is the simple resharding tool for migrating Sitecore XDB data form 'n' shards to 'm' shards. Implemented and tested on Sitecore 9.1 version.
## Configurations
In the `appsettings.json` file: 
- add connections strings for source and target shard map managers
- if need change batch size
- if need change shard map names
- if need configure from what date need to move interactions (format:`yyyy-MM-dd`)
- if need `RESUME` mode need to configure `resharding.log` connection string 

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
When the `appsettings.json` file is configured just run `...\ToolReleases\win-x64> .\Sitecore.XDB.ReshardingTool.exe` and choose a command that you want.
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/Sitecore.XDB.ReshardingTool_use.png)
**NOTE:** for see details open `log{date}.txt` log file.

## How to install `m` shards
Get [files](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/tree/master/Shards.Install) and configure in the `CreateXDB.ps1` installation params and if want more than 4 shards configure in the `CreateXDB.json` file : 
- add shard variables
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/shards_variables.png)
- add shard database user tasks
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/shards_db_user.png)
- if need update database remove task
![alt text](https://github.com/pblrok/Sitecore.XDB.ReshardingTool/blob/master/shard_remove_dbs.png)
 
