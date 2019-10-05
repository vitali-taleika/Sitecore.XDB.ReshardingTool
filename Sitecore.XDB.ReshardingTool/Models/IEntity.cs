using System.Data.SqlTypes;

namespace Sitecore.XDB.ReshardingTool.Models
{
    public interface IEntity
    {
        byte[] GetKey();
        SqlGuid GetOrderFieldValue();
    }
}