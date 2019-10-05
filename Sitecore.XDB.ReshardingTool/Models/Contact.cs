using System;
using System.Data.SqlTypes;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class Contact : IEntity
    {
        public Guid ContactId { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime Created { get; set; }
        public Guid ConcurrencyToken { get; set; }
        public double Percentile { get; set; }
        public byte[] GetKey()
        {
            return ContactId.ToByteArray();
        }

        public SqlGuid GetOrderFieldValue()
        {
            return new SqlGuid(ContactId);
        }
    }
}