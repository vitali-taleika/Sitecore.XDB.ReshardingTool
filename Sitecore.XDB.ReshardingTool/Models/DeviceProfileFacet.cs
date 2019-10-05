using System;
using System.Data.SqlTypes;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class DeviceProfileFacet : IEntity
    {
        public Guid DeviceProfileId { get; set; }
        public string FacetKey { get; set; }
        public DateTime LastModified { get; set; }
        public Guid ConcurrencyToken { get; set; }
        public string FacetData { get; set; }
        public byte[] GetKey()
        {
            return DeviceProfileId.ToByteArray();
        }

        public SqlGuid GetOrderFieldValue()
        {
            return new SqlGuid(DeviceProfileId);
        }
    }
}