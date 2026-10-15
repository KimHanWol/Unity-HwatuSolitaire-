using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>판 위의 한 칸. 판 바깥 테두리(-1, Width/Height)도 경로 계산에 쓴다.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => "(" + X + "," + Y + ")";
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
    }

    /// <summary>
    /// 사천성 판. 칸 값은 화투패 번호(0~47), 빈칸(Empty), 막힌 칸(Block) 중 하나다.
    /// 화투패 번호 = 월(0~11) * 4 + 그 달의 몇 번째 패(0~3). 같은 월끼리 짝이 된다.
    /// 판이 크면 두 벌을 써서 같은 번호가 여러 장 있을 수 있다.
    /// y는 위에서 아래로 커진다.
    ///
    /// 2층 판: 칸마다 아래층에 엎어진 패가 한 장 더 있을 수 있다(under). 보이고 누를 수 있는 건 위층(this[])뿐이고,
    /// 위 패를 지우면(Take) 아래 패가 올라와 드러난다. 아래 패가 있는 칸은 비지 않으므로 선이 지나갈 수 없다.
    /// 장수 세기(CardCount, CountMonth)는 두 층을 다 센다. 칸 목록(CellsOfMonth, CardCells)은 위층만.
    /// </summary>
    public sealed class Board
    {
        public const int Empty = -1;
        public const int Block = -2;
        public const int Months = 12;
        public const int CardsPerMonth = 4;
        public const int DeckSize = Months * CardsPerMonth;

        public readonly int Width, Height;
        readonly int[] cells;
        readonly int[] under;

        public Board(int width, int height)
        {
            if (width < 1 || height < 1) throw new ArgumentException("board size");
            Width = width;
            Height = height;
            cells = new int[width * height];
            under = new int[width * height];
            for (int i = 0; i < cells.Length; i++) cells[i] = under[i] = Empty;
        }

        public Board(int width, int height, int[] values, int[] underValues = null) : this(width, height)
        {
            if (values.Length != cells.Length) throw new ArgumentException("cells length");
            Array.Copy(values, cells, cells.Length);
            if (underValues != null)
            {
                if (underValues.Length != cells.Length) throw new ArgumentException("under length");
                Array.Copy(underValues, under, under.Length);
            }
        }

        public static int Month(int card) => card / CardsPerMonth;
        public static bool IsCardValue(int v) => v >= 0;

        public int this[int x, int y]
        {
            get => cells[y * Width + x];
            set => cells[y * Width + x] = value;
        }

        public int this[Cell c]
        {
            get => cells[c.Y * Width + c.X];
            set => cells[c.Y * Width + c.X] = value;
        }

        /// <summary>아래층 패 (없으면 Empty).</summary>
        public int Under(Cell c) => under[c.Y * Width + c.X];

        public void SetUnder(Cell c, int value) => under[c.Y * Width + c.X] = value;

        public bool HasLayers
        {
            get
            {
                foreach (int v in under) if (v >= 0) return true;
                return false;
            }
        }

        /// <summary>위 패를 지운다. 아래층 패가 있으면 올라와 드러나고 그 번호를, 없으면 Empty를 돌려준다.</summary>
        public int Take(Cell c)
        {
            int i = c.Y * Width + c.X;
            cells[i] = under[i];
            under[i] = Empty;
            return cells[i];
        }

        public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool IsCard(Cell c) => Inside(c.X, c.Y) && this[c] >= 0;

        /// <summary>경로가 지나갈 수 있는 칸: 판 바깥 한 줄 테두리이거나 빈칸.</summary>
        public bool Passable(int x, int y)
        {
            if (x < -1 || y < -1 || x > Width || y > Height) return false;
            if (!Inside(x, y)) return true;
            return cells[y * Width + x] == Empty;
        }

        /// <summary>남은 패 수 (아래층 포함).</summary>
        public int CardCount
        {
            get
            {
                int n = 0;
                foreach (int v in cells) if (v >= 0) n++;
                foreach (int v in under) if (v >= 0) n++;
                return n;
            }
        }

        public bool Cleared => CardCount == 0;

        public int[] ToArray() => (int[])cells.Clone();
        public int[] UnderArray() => (int[])under.Clone();

        public Board Clone() => new Board(Width, Height, cells, under);

        /// <summary>그 달의 패가 판에 몇 장 남았는지 (아래층 포함).</summary>
        public int CountMonth(int month)
        {
            int n = 0;
            foreach (int v in cells) if (v >= 0 && Month(v) == month) n++;
            foreach (int v in under) if (v >= 0 && Month(v) == month) n++;
            return n;
        }

        /// <summary>위층에 보이는 그 달 패의 칸.</summary>
        public List<Cell> CellsOfMonth(int month)
        {
            var list = new List<Cell>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int v = this[x, y];
                    if (v >= 0 && Month(v) == month) list.Add(new Cell(x, y));
                }
            return list;
        }

        /// <summary>위층에 패가 있는 칸.</summary>
        public List<Cell> CardCells()
        {
            var list = new List<Cell>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (this[x, y] >= 0) list.Add(new Cell(x, y));
            return list;
        }
    }
}
