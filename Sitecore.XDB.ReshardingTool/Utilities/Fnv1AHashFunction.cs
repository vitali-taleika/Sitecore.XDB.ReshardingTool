using System.Collections.Generic;

namespace Sitecore.XDB.ReshardingTool.Utilities
{
    public class Fnv1AHashFunction
    {
        public byte[] GetHashedKey(byte[] bytes)
        {
            return new[]
            {
                (byte) (Hash(bytes) >> 56)
            };
        }

        private static ulong Hash(IEnumerable<byte> bytes)
        {
            ulong num1 = 14695981039346656037;
            foreach (byte num2 in bytes)
            {
                num1 ^= num2;
                num1 *= 1099511628211UL;
            }
            return num1;
        }
    }
}
