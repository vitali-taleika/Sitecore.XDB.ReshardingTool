using System;

namespace Sitecore.XDB.ReshardingTool.Utilities
{
    public class SequentialSqlGuidGenerator
    {
        private static readonly int[] SqlSortByteOrder = new int[16]
        {
              10,
              11,
              12,
              13,
              14,
              15,
              8,
              9,
              6,
              7,
              4,
              5,
              0,
              1,
              2,
              3
        };
        private static readonly long OffsetZero = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        private long _previousOffset = -1;
        private const int TicksPerMilliseconds = 10;
        private readonly byte[] _machineId;
        private uint _nextSequential;

        public SequentialSqlGuidGenerator(byte[] nodeId)
        {
            _machineId = new byte[6];
            nodeId.CopyTo(_machineId, 0);
        }

        public Guid GenerateNextGuid()
        {
            label_0:
            lock (this)
            {
                long num = GetCurrentNumberOfTicks() / 1000L;
                if (num > _previousOffset)
                {
                    _previousOffset = num;
                    ResetSequential();
                }
                uint seq;
                if (TryGetNextSequential(out seq))
                {
                    byte[] numArray1 = EnsureBigEndian(BitConverter.GetBytes(num));
                    byte[] numArray2 = new byte[16];
                    Array.Copy(numArray1, 2, numArray2, 0, 6);
                    Array.Copy(EnsureBigEndian(BitConverter.GetBytes(seq)), 0, numArray2, 6, 4);
                    Array.Copy(_machineId, 0, numArray2, 10, 6);
                    byte[] b = new byte[16];
                    for (int index = 0; index < SqlSortByteOrder.Length; ++index)
                        b[SqlSortByteOrder[index]] = numArray2[index];
                    return new Guid(b);
                }
                goto label_0;
            }
        }

        internal static byte[] GetSqlGuidByteArray(Guid value)
        {
            byte[] byteArray = value.ToByteArray();
            byte[] numArray = new byte[16];
            for (int index = 0; index < 16; ++index)
                numArray[index] = byteArray[SqlSortByteOrder[index]];
            return numArray;
        }

        private static long GetCurrentNumberOfTicks()
        {
            return DateTime.UtcNow.Ticks - OffsetZero;
        }

        private static byte[] EnsureBigEndian(byte[] bs)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse((Array)bs);
            return bs;
        }

        private void ResetSequential()
        {
            _nextSequential = 0U;
        }

        private bool TryGetNextSequential(out uint seq)
        {
            if (_nextSequential == uint.MaxValue)
            {
                seq = 0U;
                return false;
            }
            seq = _nextSequential++;
            return true;
        }
    }
}
