using System;
using System.Data.SqlTypes;
using Sitecore.XDB.ReshardingTool.Utilities;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class Interaction : IEntity
    {
        public Guid InteractionId { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
        public Guid ConcurrencyToken { get; set; }
        public Guid ContactId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int Initiator { get; set; }
        public Guid? DeviceProfileId { get; set; }
        public Guid ChannelId { get; set; }
        public Guid? VenueId { get; set; }
        public Guid? CampaignId { get; set; }
        public string Events { get; set; }
        public string UserAgent { get; set; }
        public int EngagementValue { get; set; }
        public double Percentile { get; set; }
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