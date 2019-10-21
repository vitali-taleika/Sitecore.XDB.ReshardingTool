using System;
using System.Data.SqlTypes;
using Sitecore.XDB.ReshardingTool.Utilities;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class DeviceProfile : IEntity
    {
        public Guid DeviceProfileId { get; set; }
        public DateTime LastModified { get; set; }
        public Guid ConcurrencyToken { get; set; }
        public Guid LastKnownContactId { get; set; }
        public byte[] GetKey()
        {
            return PartitionKeyGenerator.Generate(DeviceProfileId);
        }

        public SqlGuid GetOrderFieldValue()
        {
            return new SqlGuid(DeviceProfileId);
        }
    }
}