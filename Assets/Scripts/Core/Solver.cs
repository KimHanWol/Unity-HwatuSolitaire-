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

        public const int SweepBombs = 3;

        /// <summary>
        /// 싹쓸이가 되는 수순이 있는지 무작위로 여러 번 두어 본다. 게임과 같은 규칙으로:
        ///  - 짝을 지운 직후 같은 달 열린 패가 하나 더 있고, 그 달이 판에 아직 남는다면(4장 이상이던 달) 쌌다 → 폭탄 하나
        ///  - 폭탄 달은 짝으로 두지 않는다 (게임에서도 누르면 폭탄이 터진다)
        ///  - 폭탄 셋을 모은 채 판에 폭탄 달 패만 남으면 마무리로 판이 비므로 싹쓸이 성공
        /// 쌌다를 할 수 있는 짝을 대부분 먼저 고르고, 가끔 아무 짝이나 고른다.
        /// </summary>
        public static bool CanSweep(Board start, int seed, int playouts)
        {
            var rng = new Rng(seed);
            for (int p = 0; p < playouts; p++)
                if (SweepPlayout(start.Clone(), rng)) return true;
            return false;
        }

        static bool SweepPlayout(Board board, Rng rng)
        {
            var bombs = new List<int>();
            var mask = new bool[Board.Months];
            while (true)
            {
                if (bombs.Count == SweepBombs && OnlyMonths(board, mask)) return true;
                if (board.Cleared) return false;
                var moves = Rules.FindMoves(board, mask);
                if (moves.Count == 0) return false;

                // 쌌다를 할 수 있는 짝: 지운 뒤에도 그 달이 2장 이상 남고, 그중 하나가 열려 있다.
                var stackable = new List<(Move move, int third)>();
                if (bombs.Count < SweepBombs)
                    foreach (var m in moves)
                    {
                        int month = Board.Month(board[m.A]);
                        if (board.CountMonth(month) < 4) continue;
                        int third = ThirdAfter(board, m, month);
                        if (third >= 0) stackable.Add((m, third));
                    }

                if (stackable.Count > 0 && rng.NextDouble() < 0.85)
                {
                    var (m, _) = stackable[rng.Next(stackable.Count)];
                    int month = Board.Month(board[m.A]);
                    Rules.Remove(board, m.A, m.B);
                    // 지운 뒤 다시 찾는다 (지운 패가 덮고 있던 패가 열렸을 수 있다).
                    int third = -1;
                    foreach (int i in board.TilesOfMonth(month)) if (board.Free(i)) { third = i; break; }
                    board[third] = Board.Empty;
                    bombs.Add(month);
                    mask[month] = true;
                }
                else
                {
                    var m = moves[rng.Next(moves.Count)];
                    Rules.Remove(board, m.A, m.B);
                }
            }
        }

        /// <summary>짝 m을 지운 뒤 같은 달 열린 패(셋째)가 있으면 그 자리, 없으면 -1.</summary>
        static int ThirdAfter(Board board, Move m, int month)
        {
            int a = board[m.A], b = board[m.B];
            board[m.A] = Board.Empty;
            board[m.B] = Board.Empty;
            int found = -1;
            foreach (int i in board.TilesOfMonth(month)) if (board.Free(i)) { found = i; break; }
            board[m.A] = a;
            board[m.B] = b;
            return found;
        }

        static bool OnlyMonths(Board board, bool[] mask)
        {
            for (int i = 0; i < board.Count; i++)
                if (board.Present(i) && !mask[Board.Month(board[i])]) return false;
            return true;
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
