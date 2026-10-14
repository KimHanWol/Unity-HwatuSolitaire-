namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 시드가 같으면 어느 기기에서나 같은 수열을 내는 난수 (xorshift32).
    /// System.Random은 런타임마다 구현이 다를 수 있어 스테이지 생성·섞기에 쓰지 않는다.
    /// </summary>
    public sealed class Rng
    {
        uint state;

        public Rng(int seed)
        {
            state = (uint)seed * 2654435761u + 0x9E3779B9u;
            if (state == 0) state = 0x6D2B79F5u;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>0 이상 max 미만.</summary>
        public int Next(int max) => max <= 1 ? 0 : (int)(NextUInt() % (uint)max);

        /// <summary>min 이상 max 미만.</summary>
        public int Next(int min, int max) => min + Next(max - min);

        public double NextDouble() => (NextUInt() >> 8) / 16777216.0;

        public void Shuffle<T>(System.Collections.Generic.IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }
    }
}
