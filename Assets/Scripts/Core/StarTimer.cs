using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 광 기준 시간 정하기. 사람이 한 짝을 찾는 데 걸리는 시간을 판 상태로 어림하는 모형으로
    /// 스테이지를 여러 번 끝까지 풀어 보고(모의 풀이), 그 가운데값에 배수를 곱해 3광·2광 기준과 제한 시간을 정한다.
    ///
    /// 한 짝에 드는 시간(초) = 두 번 누르고 패가 사라지는 시간(Tap)
    ///                     + 찾는 시간: Search × √(남은 패 / 둘 수 있는 짝) × (달 수가 많을수록 조금 더)
    ///                     + 막힌 패를 가려내는 시간: BlockedLook × (열리지 않은 패 비율)
    /// 막히면 자동 섞기로 Reshuffle 초가 더 든다. 판을 처음 훑어보는 시간(FirstLook)도 더한다.
    /// </summary>
    public static class StarTimer
    {
        public const double Tap = 0.85;
        public const double Search = 0.55;
        public const double BlockedLook = 0.5;
        public const double Reshuffle = 2.0;
        public const double FirstLook = 2.0;

        public const double Star3Factor = 1.25, Star3Extra = 4;
        public const double Star2Factor = 1.9, Star2Extra = 10;
        public const double LimitFactor = 3.0, LimitExtra = 20;

        public const int Runs = 15;

        public static void Assign(StageDef stage)
        {
            var times = new List<double>();
            for (int r = 0; r < Runs; r++) times.Add(Simulate(stage, stage.number * 1000 + r));
            times.Sort();
            double median = times[times.Count / 2];
            stage.simSeconds = (float)Math.Round(median, 1);
            stage.star3 = RoundUp(median * Star3Factor + Star3Extra, 5);
            stage.star2 = Math.Max(stage.star3 + 10, RoundUp(median * Star2Factor + Star2Extra, 5));
            stage.timeLimit = Math.Max(stage.star2 + 20, RoundUp(median * LimitFactor + LimitExtra, 10));
        }

        static int RoundUp(double v, int step) => (int)(Math.Ceiling(v / step) * step);

        public static double Simulate(StageDef stage, int seed)
        {
            var rng = new Rng(seed);
            var board = stage.CreateBoard();
            double time = FirstLook;
            int guard = 0;
            while (!board.Cleared && guard++ < 1000)
            {
                var moves = Rules.FindMoves(board);
                if (moves.Count == 0)
                {
                    time += Reshuffle;
                    Rules.Shuffle(board, rng);
                    continue;
                }
                int remaining = board.CardCount;
                int free = 0, months = 0;
                for (int i = 0; i < board.Count; i++) if (board.Free(i)) free++;
                for (int m = 0; m < Board.Months; m++) if (board.CountMonth(m) > 0) months++;
                double search = Search * Math.Sqrt(remaining / (double)moves.Count) * (0.75 + 0.25 * months / Board.Months);
                double blocked = BlockedLook * (1.0 - free / (double)remaining);
                time += Tap + search + blocked;

                var m0 = moves[rng.Next(moves.Count)];
                Rules.Remove(board, m0.A, m0.B);
            }
            return time;
        }
    }
}
