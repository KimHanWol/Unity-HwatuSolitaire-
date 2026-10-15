using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 무작위로 끝까지 두어 보기. 같은 달이 4장 이상이면 짝을 잘못 지어 막힐 수 있어서,
    /// 여러 번 두어 보고 다 지운 수순이 있으면 돌려준다 (배치 자체는 Rules.Deal 덕분에 늘 풀린다).
    /// </summary>
    public static class Solver
    {
        public static List<Move> Solve(Board start, int seed, int playouts)
        {
            var rng = new Rng(seed);
            for (int p = 0; p < playouts; p++)
            {
                var board = start.Clone();
                var seq = new List<Move>();
                while (true)
                {
                    if (board.Cleared) return seq;
                    var moves = Rules.FindMoves(board);
                    if (moves.Count == 0) break;
                    var m = Pick(board, moves, rng);
                    seq.Add(m);
                    Rules.Remove(board, m.A, m.B);
                }
            }
            return null;
        }

        /// <summary>무작위로 끝까지 두어 보기를 trials 번 해서 다 지운 횟수 (판이 얼마나 너그러운지).</summary>
        public static int CountClears(Board start, int seed, int trials)
        {
            var rng = new Rng(seed);
            int wins = 0;
            for (int t = 0; t < trials; t++)
                if (Solve(start, rng.Next(int.MaxValue), 1) != null) wins++;
            return wins;
        }

        /// <summary>
        /// 사람처럼: 그 달이 판에 두 장뿐이라 어느 쪽이든 같은 짝, 또는 위층 패(더 많이 덮고 있는 패)를 조금 더 먼저 고른다.
        /// </summary>
        static Move Pick(Board board, List<Move> moves, Rng rng)
        {
            if (rng.NextDouble() < 0.5)
            {
                var good = new List<Move>();
                foreach (var m in moves)
                {
                    var t = board.Layout.Tiles;
                    if (board.CountMonth(Board.Month(board[m.A])) == 2 || t[m.A].Z + t[m.B].Z > 0) good.Add(m);
                }
                if (good.Count > 0) return good[rng.Next(good.Count)];
            }
            return moves[rng.Next(moves.Count)];
        }
    }
}
