using System;
using System.Data.SqlTypes;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class ContactIdentifiersIndex : IEntity
    {
        public byte[] Identifier { get; set; }
        public int IdentifierHash { get; set; }
        public string Source { get; set; }
        public Guid ContactId { get; set; }
        public DateTime? LockTime { get; set; }
        public Guid Version { get; set; }
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
