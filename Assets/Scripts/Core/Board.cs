using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 패 한 장이 놓이는 자리. X, Y는 반 칸 단위(패 하나가 가로 2, 세로 2를 차지), Z는 층(0이 맨 아래).
    /// 반 칸 단위라서 위층 패를 아래 두 장 사이에 걸쳐 놓을 수 있다 (상하이의 피라미드 모양).
    /// </summary>
    public readonly struct Tile
    {
        public readonly int X, Y, Z;
        public Tile(int x, int y, int z) { X = x; Y = y; Z = z; }
        public override string ToString() => "(" + X + "," + Y + "," + Z + ")";

        /// <summary>두 패가 위에서 내려다볼 때 겹치는지 (층은 보지 않는다).</summary>
        public bool Overlaps(Tile o) => Math.Abs(X - o.X) < 2 && Math.Abs(Y - o.Y) < 2;
    }

    /// <summary>
    /// 판 모양: 자리 목록과 자리끼리의 관계(위를 덮는 자리, 왼쪽·오른쪽에 붙은 자리)를 미리 계산해 둔다.
    /// </summary>
    public sealed class Layout
    {
        public readonly Tile[] Tiles;
        /// <summary>i 위로 겹쳐 덮는 더 높은 층의 자리들.</summary>
        public readonly int[][] Above;
        public readonly int MinX, MinY, MaxX, MaxY, MaxZ;

        public Layout(IList<Tile> tiles)
        {
            int n = tiles.Count;
            Tiles = new Tile[n];
            tiles.CopyTo(Tiles, 0);
            var above = new List<int>[n];
            for (int i = 0; i < n; i++) above[i] = new List<int>();
            MinX = MinY = int.MaxValue;
            MaxX = MaxY = MaxZ = int.MinValue;
            for (int i = 0; i < n; i++)
            {
                var a = Tiles[i];
                MinX = Math.Min(MinX, a.X); MinY = Math.Min(MinY, a.Y);
                MaxX = Math.Max(MaxX, a.X + 2); MaxY = Math.Max(MaxY, a.Y + 2);
                MaxZ = Math.Max(MaxZ, a.Z);
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var b = Tiles[j];
                    if (b.Z > a.Z && a.Overlaps(b)) above[i].Add(j);
                }
            }
            Above = new int[n][];
            for (int i = 0; i < n; i++) Above[i] = above[i].ToArray();
        }

        public int Count => Tiles.Length;

        /// <summary>[x0,y0,z0, x1,y1,z1, ...] (JSON 저장용)</summary>
        public int[] Flatten()
        {
            var f = new int[Tiles.Length * 3];
            for (int i = 0; i < Tiles.Length; i++) { f[i * 3] = Tiles[i].X; f[i * 3 + 1] = Tiles[i].Y; f[i * 3 + 2] = Tiles[i].Z; }
            return f;
        }

        public static Layout FromFlat(int[] f)
        {
            var list = new List<Tile>();
            for (int i = 0; i + 2 < f.Length; i += 3) list.Add(new Tile(f[i], f[i + 1], f[i + 2]));
            return new Layout(list);
        }
    }

    /// <summary>
    /// 화투 사천성(타일 매치 방식) 판. 자리마다 화투패 번호(0~47) 또는 Empty(지워짐).
    /// 화투패 번호 = 월(0~11) * 4 + 그 달의 몇 번째 패(0~3). 같은 월끼리 짝이 된다 (판이 크면 두 벌).
    ///
    /// 열린 패(Free): 위를 덮은 패가 없는 패 (옆은 상관없다 — 정통 상하이의 "양옆 중 한쪽이 비어야" 규칙은 쓰지 않는다).
    /// 열린 패 두 장이 같은 달이면 짝으로 지울 수 있다.
    /// </summary>
    public sealed class Board
    {
        public const int Empty = -1;
        public const int Months = 12;
        public const int CardsPerMonth = 4;

        public readonly Layout Layout;
        readonly int[] values;

        public Board(Layout layout, int[] cards)
        {
            if (cards.Length != layout.Count) throw new ArgumentException("cards length");
            Layout = layout;
            values = (int[])cards.Clone();
        }

        public static int Month(int card) => card / CardsPerMonth;

        public int Count => values.Length;

        public int this[int i]
        {
            get => values[i];
            set => values[i] = value;
        }

        public bool Present(int i) => values[i] >= 0;

        public bool Exposed(int i)
        {
            if (values[i] < 0) return false;
            foreach (int j in Layout.Above[i]) if (values[j] >= 0) return false;
            return true;
        }

        public bool Free(int i) => Exposed(i);

        public int CardCount
        {
            get
            {
                int n = 0;
                foreach (int v in values) if (v >= 0) n++;
                return n;
            }
        }

        public bool Cleared => CardCount == 0;

        public int CountMonth(int month)
        {
            int n = 0;
            foreach (int v in values) if (v >= 0 && Month(v) == month) n++;
            return n;
        }

        public List<int> TilesOfMonth(int month)
        {
            var list = new List<int>();
            for (int i = 0; i < values.Length; i++) if (values[i] >= 0 && Month(values[i]) == month) list.Add(i);
            return list;
        }

        public List<int> PresentTiles()
        {
            var list = new List<int>();
            for (int i = 0; i < values.Length; i++) if (values[i] >= 0) list.Add(i);
            return list;
        }

        public int[] ToArray() => (int[])values.Clone();

        public Board Clone() => new Board(Layout, values);
    }
}
