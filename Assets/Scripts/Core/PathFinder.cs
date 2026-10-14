using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 사천성 규칙의 연결 검사: 두 패 사이를 꺾임 2번 이하(직선 세 토막 이하)의 선으로,
    /// 빈칸과 판 바깥 테두리만 지나서 이을 수 있어야 한다.
    /// </summary>
    public static class PathFinder
    {
        public const int MaxTurns = 2;

        static readonly int[] Dx = { 1, -1, 0, 0 };
        static readonly int[] Dy = { 0, 0, 1, -1 };

        /// <summary>같은 월 두 패가 이어지는지.</summary>
        public static bool CanMatch(Board board, Cell a, Cell b) => FindPath(board, a, b) != null;

        /// <summary>
        /// 두 칸을 잇는 경로의 꺾이는 점들(시작, 꺾임..., 끝)을 돌려준다. 짝이 아니거나 이을 수 없으면 null.
        /// 꺾임이 가장 적은 경로를 고른다.
        /// </summary>
        public static List<Cell> FindPath(Board board, Cell a, Cell b)
        {
            if (a == b || !board.IsCard(a) || !board.IsCard(b)) return null;
            if (Board.Month(board[a]) != Board.Month(board[b])) return null;
            return Connect(board, a, b);
        }

        /// <summary>패 종류와 상관없이 두 칸 사이 연결만 본다.</summary>
        public static List<Cell> Connect(Board board, Cell a, Cell b)
        {
            int pw = board.Width + 2, ph = board.Height + 2;
            int size = pw * ph;
            var turns = new int[size];
            var prev = new int[size];
            for (int i = 0; i < size; i++) { turns[i] = int.MaxValue; prev[i] = -1; }

            int Index(int x, int y) => (y + 1) * pw + (x + 1);
            int start = Index(a.X, a.Y), goal = Index(b.X, b.Y);
            turns[start] = -1;

            var frontier = new List<int> { start };
            for (int t = 0; t <= MaxTurns && frontier.Count > 0; t++)
            {
                var next = new List<int>();
                foreach (int s in frontier)
                {
                    int sx = s % pw - 1, sy = s / pw - 1;
                    for (int d = 0; d < 4; d++)
                    {
                        int x = sx + Dx[d], y = sy + Dy[d];
                        while (x >= -1 && y >= -1 && x <= board.Width && y <= board.Height)
                        {
                            int i = Index(x, y);
                            if (i == goal)
                            {
                                prev[goal] = s;
                                return Build(prev, goal, start, pw);
                            }
                            if (!board.Passable(x, y)) break;
                            if (turns[i] > t)
                            {
                                turns[i] = t;
                                prev[i] = s;
                                next.Add(i);
                            }
                            x += Dx[d];
                            y += Dy[d];
                        }
                    }
                }
                frontier = next;
            }
            return null;
        }

        static List<Cell> Build(int[] prev, int goal, int start, int pw)
        {
            var points = new List<Cell>();
            int cur = goal;
            while (cur != -1)
            {
                points.Add(new Cell(cur % pw - 1, cur / pw - 1));
                if (cur == start) break;
                cur = prev[cur];
            }
            points.Reverse();
            return points;
        }
    }
}
