using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 끝까지 풀리는지 확인한다. 같은 달 패가 4장 이상이면 짝을 잘못 지어 막힐 수 있어서,
    /// 무작위로 끝까지 두어 보는 시도를 여러 번 하고, 하나라도 다 지우면 풀리는 판으로 본다.
    /// </summary>
    public static class Solver
    {
        /// <summary>다 지우는 수순을 찾으면 돌려주고, 못 찾으면 null.</summary>
        public static List<Move> Solve(Board start, GravityMode gravity, int seed, int playouts)
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
                    Rules.Remove(board, m.A, m.B, gravity);
                }
            }
            return null;
        }

        /// <summary>무작위로 끝까지 두어 보기를 trials 번 해서 다 지운 횟수.</summary>
        public static int CountClears(Board start, GravityMode gravity, int seed, int trials)
        {
            var rng = new Rng(seed);
            int wins = 0;
            for (int t = 0; t < trials; t++)
                if (Solve(start, gravity, rng.Next(int.MaxValue), 1) != null) wins++;
            return wins;
        }

        /// <summary>
        /// 짝이 둘뿐인 달(어느 쪽을 골라도 같은)을 조금 더 먼저 두고, 나머지는 무작위로 고른다.
        /// 사람이 두는 방식과 비슷하게, 완전히 똑똑하지도 엉망이지도 않게.
        /// </summary>
        static Move Pick(Board board, List<Move> moves, Rng rng)
        {
            if (rng.NextDouble() < 0.5)
            {
                var safe = new List<Move>();
                foreach (var m in moves)
                    if (board.CountMonth(Board.Month(board[m.A])) == 2) safe.Add(m);
                if (safe.Count > 0) return safe[rng.Next(safe.Count)];
            }
            return moves[rng.Next(moves.Count)];
        }

        /// <summary>
        /// 힌트용: 지금 둘 수 있는 짝 가운데 그 뒤로도 끝까지 풀리는 짝을 고른다. 시간 안에 못 찾으면 아무 짝이나.
        /// skipMonth에 표시된 달(폭탄이 걸린 달)은 고르지 않는다.
        /// </summary>
        public static Move? Hint(Board board, GravityMode gravity, bool[] skipMonth, int seed)
        {
            var moves = Rules.FindMoves(board, skipMonth);
            if (moves.Count == 0) return null;
            var rng = new Rng(seed);
            rng.Shuffle(moves);
            int checks = System.Math.Min(moves.Count, 8);
            for (int i = 0; i < checks; i++)
            {
                var b = board.Clone();
                Rules.Remove(b, moves[i].A, moves[i].B, gravity);
                if (b.Cleared || Solve(b, gravity, rng.Next(int.MaxValue), 3) != null) return moves[i];
            }
            return moves[0];
        }
    }
}
