using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 별 기준 시간 정하기. 사람이 한 짝을 찾는 데 걸리는 시간을 판 상태로 어림하는 모형으로
    /// 스테이지를 여러 번 끝까지 풀어 보고(모의 풀이), 그 가운데값에 배수를 곱해 별 3·2개 기준과 제한 시간을 정한다.
    ///
    /// 한 짝에 드는 시간(초) = 두 번 누르고 패가 사라지는 시간(Tap)
    ///                     + 찾는 시간: Search × √(남은 패 / 둘 수 있는 짝) × (달 수가 많을수록 조금 더)
    ///                     + 중력이 있으면 패가 움직여 다시 살펴보는 시간(GravityLook)
    /// 막히면 자동 섞기로 Reshuffle 초가 더 든다. 판을 처음 훑어보는 시간(FirstLook)도 더한다.
    /// 장수가 많을수록, 짝이 드물수록, 달이 많을수록 시간이 길어지므로 "총 장수에 비례"하면서도 판의 성격이 반영된다.
    /// </summary>
    public static class StarTimer
    {
        public const double Tap = 0.85;
        public const double Search = 0.55;
        public const double GravityLook = 0.25;
        public const double Reshuffle = 2.0;
        public const double FirstLook = 2.0;

        public const double Star3Factor = 1.25, Star3Extra = 4;
        public const double Star2Factor = 1.9, Star2Extra = 10;
        public const double LimitFactor = 3.0, LimitExtra = 20;

        public const int Runs = 15;

        /// <summary>모의 풀이로 예상 시간을 재고 별 기준을 채워 넣는다.</summary>
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
            var gravity = stage.Gravity;
            double time = FirstLook;
            int guard = 0;
            while (!board.Cleared && guard++ < 1000)
            {
                var moves = Rules.FindMoves(board);
                if (moves.Count == 0)
                {
                    time += Reshuffle;
                    Rules.Shuffle(board, gravity, rng, 10);
                    continue;
                }
                int remaining = board.CardCount;
                int months = 0;
                for (int m = 0; m < Board.Months; m++) if (board.CountMonth(m) > 0) months++;
                double search = Search * Math.Sqrt(remaining / (double)moves.Count) * (0.75 + 0.25 * months / Board.Months);
                time += Tap + search + (gravity != GravityMode.None ? GravityLook : 0);

                var m0 = moves[rng.Next(moves.Count)];
                Rules.Remove(board, m0.A, m0.B, gravity);
            }
            return time;
        }
    }
}
