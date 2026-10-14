using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    public readonly struct Move
    {
        public readonly Cell A, B;
        public Move(Cell a, Cell b) { A = a; B = b; }
        public override string ToString() => A + "-" + B;
    }

    /// <summary>둘 수 있는 짝 찾기, 짝 없애기, 섞기.</summary>
    public static class Rules
    {
        /// <summary>
        /// 지금 이을 수 있는 짝을 모두 찾는다. skipMonth가 true인 달은 제외한다 (폭탄이 걸린 달).
        /// </summary>
        public static List<Move> FindMoves(Board board, bool[] skipMonth = null, int limit = int.MaxValue)
        {
            var moves = new List<Move>();
            var byMonth = GroupByMonth(board);
            for (int m = 0; m < Board.Months; m++)
            {
                if (skipMonth != null && skipMonth[m]) continue;
                var cells = byMonth[m];
                for (int i = 0; i < cells.Count; i++)
                    for (int j = i + 1; j < cells.Count; j++)
                    {
                        if (PathFinder.Connect(board, cells[i], cells[j]) == null) continue;
                        moves.Add(new Move(cells[i], cells[j]));
                        if (moves.Count >= limit) return moves;
                    }
            }
            return moves;
        }

        public static bool HasMove(Board board, bool[] skipMonth = null) => FindMoves(board, skipMonth, 1).Count > 0;

        public static List<Cell>[] GroupByMonth(Board board)
        {
            var byMonth = new List<Cell>[Board.Months];
            for (int m = 0; m < Board.Months; m++) byMonth[m] = new List<Cell>();
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    int v = board[x, y];
                    if (v >= 0) byMonth[Board.Month(v)].Add(new Cell(x, y));
                }
            return byMonth;
        }

        /// <summary>두 패를 없애고 중력을 적용한다. 미끄러진 패 목록을 돌려준다.</summary>
        public static List<Slide> Remove(Board board, Cell a, Cell b, GravityMode gravity)
        {
            board[a] = Board.Empty;
            board[b] = Board.Empty;
            return Gravity.Apply(board, gravity);
        }

        /// <summary>
        /// 남은 패를 지금 자리들 안에서 섞는다. 둘 수 있는 짝이 생기고, 가능하면 끝까지 풀리는 배치를 고른다.
        /// 섞은 결과(자리 → 새 패) 대신 판을 직접 바꾼다.
        /// </summary>
        public static void Shuffle(Board board, GravityMode gravity, Rng rng, int attempts = 40)
        {
            var cells = board.CardCells();
            if (cells.Count < 2) return;
            var values = new List<int>();
            foreach (var c in cells) values.Add(board[c]);

            int[] fallback = null;
            for (int t = 0; t < attempts; t++)
            {
                rng.Shuffle(values);
                for (int i = 0; i < cells.Count; i++) board[cells[i]] = values[i];
                if (!HasMove(board)) continue;
                if (fallback == null) fallback = values.ToArray();
                if (Solver.Solve(board, gravity, rng.Next(int.MaxValue), 6) != null) return;
            }
            if (fallback != null)
                for (int i = 0; i < cells.Count; i++) board[cells[i]] = fallback[i];
        }
    }
}
