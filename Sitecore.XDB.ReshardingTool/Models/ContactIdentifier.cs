using System;
using System.Data.SqlTypes;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public class ContactIdentifier : IEntity
    {
        public Guid ContactId { get; set; }
        public string Source { get; set; }
        public byte[] Identifier { get; set; }
        public int IdentifierHash { get; set; }
        public int IdentifierType { get; set; }
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