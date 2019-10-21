using System;
using System.Collections.Generic;

namespace Sitecore.XDB.ReshardingTool.Utilities
{
    public static class PartitionKeyGenerator
    {
        private static readonly Fnv1AHashFunction HashFunction;
        static PartitionKeyGenerator()
        {
            HashFunction = new Fnv1AHashFunction();
        }

        static Dictionary<Type, Func<object, byte[]>> Convertors
        {
            get
            {
                Dictionary<Type, Func<object, byte[]>> dictionary = new Dictionary<Type, Func<object, byte[]>>();
                Type index1 = typeof(Guid);
                dictionary[index1] = value => SequentialSqlGuidGenerator.GetSqlGuidByteArray((Guid)value);
                Type index2 = typeof(Guid?);
                dictionary[index2] = value =>
                {
                    if (!((Guid?)value).HasValue)
                        return null;
                    return SequentialSqlGuidGenerator.GetSqlGuidByteArray((Guid)value);
                };
                Type index3 = typeof(byte[]);
                dictionary[index3] = value => (byte[])value;
                return dictionary;
            }
        }

        public static byte[] Generate<T>(T key)
        {
            if (!Convertors.TryGetValue(typeof(T), out var func))
                throw new InvalidOperationException($"Unable to convert {typeof(T)} to byte array.");
            return GetHashedKey(func(key));
        }

        static byte[] GetHashedKey(byte[] value)
        {
            return HashFunction.GetHashedKey(value);
        }
    }
}
