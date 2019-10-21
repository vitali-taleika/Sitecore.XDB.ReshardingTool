using System;
using System.Data.SqlTypes;
using Sitecore.XDB.ReshardingTool.Utilities;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class InteractionFacet : IEntity
    {
        public Guid InteractionId { get; set; }
        public string FacetKey { get; set; }
        public Guid ContactId { get; set; }
        public DateTime LastModified { get; set; }
        public Guid ConcurrencyToken { get; set; }
        public string FacetData { get; set; }
        public byte[] GetKey()
        {
            return PartitionKeyGenerator.Generate(InteractionId);
        }

        public SqlGuid GetOrderFieldValue()
        {
            return new SqlGuid(InteractionId);
        }
    }
}