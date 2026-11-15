using System;

namespace BTH.Core.Run
{
    /// <summary>XorShift32: nhanh, tất định, state chỉ 4 byte nên snapshot dễ.</summary>
    public sealed class XorShiftRandom : IRandomSource
    {
        private uint _state;

        public XorShiftRandom(int seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : (uint)seed;
        }

        public uint State
        {
            get => _state;
            set => _state = value == 0 ? 0x9E3779B9u : value;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat()
        {
            return (NextUInt() & 0xFFFFFF) / 16777216f;
        }

        public override string ToString()
        {
            return "xorshift:" + _state.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
