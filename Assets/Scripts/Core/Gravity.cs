using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>패를 없앤 뒤 남은 패가 쏠리는 방향.</summary>
    public enum GravityMode
    {
        None = 0,
        Down = 1,
        Up = 2,
        Left = 3,
        Right = 4,
        /// <summary>왼쪽 절반은 왼쪽으로, 오른쪽 절반은 오른쪽으로 벌어진다.</summary>
        SplitOut = 5,
        /// <summary>양쪽 절반이 가운데로 모인다.</summary>
        SplitIn = 6,
        /// <summary>위쪽 절반은 위로, 아래쪽 절반은 아래로 벌어진다.</summary>
        SplitVertical = 7,
    }

    public readonly struct Slide
    {
        public readonly Cell From, To;
        public Slide(Cell from, Cell to) { From = from; To = to; }
    }

    /// <summary>
    /// 중력 적용. 막힌 칸(Block)은 움직이지 않고 벽처럼 줄을 나눈다.
    /// 패는 자기 줄(행 또는 열)의 막힌 칸 사이 구간 안에서만 미끄러진다.
    /// </summary>
    public static class Gravity
    {
        public static List<Slide> Apply(Board board, GravityMode mode)
        {
            var slides = new List<Slide>();
            if (mode == GravityMode.None) return slides;

            if (mode == GravityMode.Down || mode == GravityMode.Up || mode == GravityMode.SplitVertical)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var line = new List<Cell>();
                    for (int y = 0; y < board.Height; y++) line.Add(new Cell(x, y));
                    if (mode == GravityMode.Down) Compact(board, line, false, slides);
                    else if (mode == GravityMode.Up) Compact(board, line, true, slides);
                    else
                    {
                        int half = board.Height / 2;
                        Compact(board, line.GetRange(0, half), true, slides);
                        Compact(board, line.GetRange(half, board.Height - half), false, slides);
                    }
                }
            }
            else
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var line = new List<Cell>();
                    for (int x = 0; x < board.Width; x++) line.Add(new Cell(x, y));
                    int half = board.Width / 2;
                    switch (mode)
                    {
                        case GravityMode.Left: Compact(board, line, true, slides); break;
                        case GravityMode.Right: Compact(board, line, false, slides); break;
                        case GravityMode.SplitOut:
                            Compact(board, line.GetRange(0, half), true, slides);
                            Compact(board, line.GetRange(half, board.Width - half), false, slides);
                            break;
                        case GravityMode.SplitIn:
                            Compact(board, line.GetRange(0, half), false, slides);
                            Compact(board, line.GetRange(half, board.Width - half), true, slides);
                            break;
                    }
                }
            }
            return slides;
        }

        /// <summary>line 순서대로 놓인 칸들을 막힌 칸 기준 구간으로 나눠, 각 구간의 패를 앞쪽(toStart) 또는 뒤쪽으로 모은다.</summary>
        static void Compact(Board board, List<Cell> line, bool toStart, List<Slide> slides)
        {
            int i = 0;
            while (i < line.Count)
            {
                if (board[line[i]] == Board.Block) { i++; continue; }
                int j = i;
                while (j < line.Count && board[line[j]] != Board.Block) j++;
                CompactSegment(board, line, i, j, toStart, slides);
                i = j;
            }
        }

        static void CompactSegment(Board board, List<Cell> line, int from, int to, bool toStart, List<Slide> slides)
        {
            if (toStart)
            {
                int write = from;
                for (int r = from; r < to; r++)
                {
                    int v = board[line[r]];
                    if (v < 0) continue;
                    if (r != write)
                    {
                        board[line[write]] = v;
                        board[line[r]] = Board.Empty;
                        slides.Add(new Slide(line[r], line[write]));
                    }
                    write++;
                }
            }
            else
            {
                int write = to - 1;
                for (int r = to - 1; r >= from; r--)
                {
                    int v = board[line[r]];
                    if (v < 0) continue;
                    if (r != write)
                    {
                        board[line[write]] = v;
                        board[line[r]] = Board.Empty;
                        slides.Add(new Slide(line[r], line[write]));
                    }
                    write--;
                }
            }
        }
    }
}
