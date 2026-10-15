using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 스테이지 300개의 난이도 계획과 패 배치. 결과는 Tools/StageTool 이 stages.json 으로 굳혀서 게임에 넣는다
    /// (게임은 실행 중에 만들지 않는다 → 모두에게 같은 판, 같은 광 기준).
    ///
    /// 12장(챕터) × 25스테이지. 챕터마다 화투의 달(1월 송학 ~ 12월 비)을 테마로 한다.
    /// 난이도를 올리는 손잡이:
    ///  ① 패 수 12장 → 100장 남짓 (챕터 안에서 늘고, 다음 챕터는 조금 쉬운 데서 다시 시작하는 톱니 모양)
    ///  ② 층 수 (위에 덮인 패가 많을수록 열린 패가 적다): 1~2층 → 5층
    ///  ③ 판 모양 (Layouts): 피라미드·계단 → 마름모 → 쌍봉 → 십자 → 테두리 → 탑
    ///  ④ 쓰는 달 수 (적으면 짝 찾기가 쉽다)
    ///  ⑤ "쌌다" 시간(짝 뒤 셋째 패를 얹을 수 있는 시간)이 2.0초 → 1.2초로 짧아진다
    /// 배치는 Rules.Deal 로 거꾸로 만들어 늘 끝까지 풀린다. 그 가운데 무작위로 두어도 잘 막히지 않는 배치를 고른다.
    /// </summary>
    public static class StageGenerator
    {
        public const int ChapterCount = 12;
        public const int StagesPerChapter = 25;
        public const int StageCount = ChapterCount * StagesPerChapter;

        /// <summary>세로 화면에 들어가는 판 크기 한도 (칸).</summary>
        public const float MaxColumns = 7f, MaxRows = 6f;

        public sealed class Plan
        {
            public int Number;
            public int Chapter;      // 0부터
            public string Shape;
            public List<Tile> Tiles;
            public int MonthCount;
            public float StackWindow;
            public int Layers;
        }

        public static int ChapterOf(int number) => (number - 1) / StagesPerChapter;

        public static Plan MakePlan(int number)
        {
            if (number < 1 || number > StageCount) throw new ArgumentOutOfRangeException(nameof(number));
            int chapter = ChapterOf(number);
            int k = (number - 1) % StagesPerChapter;
            float p = k / (float)(StagesPerChapter - 1);
            var rng = new Rng(number * 7919 + 17);

            // ① 목표 장수: 챕터마다 12+6c → 36+6.5c, 다섯 판마다 한 번 숨 돌리는 작은 판
            float lo = 12 + chapter * 6f, hi = 36 + chapter * 6.5f;
            float target = lo + (hi - lo) * p;
            if (k % 5 == 4 && k != StagesPerChapter - 1) target -= 8;

            // ③ 판 모양: 그 챕터까지 열린 모양 가운데 돌아가며 고르되, 그 모양으로는 목표 장수에 한참 못 미치면 다른 열린 모양도 본다.
            int unlocked = Math.Min(Layouts.Names.Length, 2 + chapter);
            string preferred = Layouts.Names[(k + chapter) % unlocked];
            string shape = preferred;

            // ② 층 수 범위
            int minL = chapter >= 4 ? 2 : 1;
            int maxL = Math.Min(5, 2 + chapter / 2);

            // 목표 장수에 가장 가까운 (가로, 세로, 층) 고르기
            List<Tile> best = null;
            int bestLayers = 0;
            float bestScore = float.MaxValue;
            for (int si = 0; si < unlocked; si++)
            {
            string name = Layouts.Names[si];
            float shapeBias = name == preferred ? 0f : 6f;
            for (int w = 3; w <= 7; w++)
                for (int h = 2; h <= 6; h++)
                    for (int l = minL; l <= maxL; l++)
                    {
                        var tiles = Layouts.Build(name, w, h, l);
                        if (tiles.Count < 8) continue;
                        var (fw, fh) = Layouts.Footprint(tiles);
                        if (fw > MaxColumns || fh > MaxRows) continue;
                        int maxZ = 0;
                        foreach (var t in tiles) maxZ = Math.Max(maxZ, t.Z);
                        if (maxZ + 1 < minL) continue;
                        int count = tiles.Count - tiles.Count % 2;
                        // 장수가 가까운 것 + 층이 그 챕터에 어울리는 것, 화면 모양에 맞는 비율을 조금 더 좋아한다.
                        float score = Math.Abs(count - target) + Math.Abs((maxZ + 1) - (minL + (maxL - minL) * p)) * 2f
                                      + Math.Abs(fh / Math.Max(1f, fw) - 0.85f) * 3f + (float)rng.NextDouble() * 0.5f + shapeBias;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = tiles;
                            bestLayers = maxZ + 1;
                            shape = name;
                        }
                    }
            }
            if (best == null) throw new InvalidOperationException("no layout for stage " + number);

            // 홀수면 맨 위층 패 하나를 뺀다.
            if (best.Count % 2 == 1)
            {
                int top = 0;
                for (int i = 1; i < best.Count; i++) if (best[i].Z >= best[top].Z) top = i;
                best.RemoveAt(top);
            }

            var plan = new Plan { Number = number, Chapter = chapter, Shape = shape, Tiles = best, Layers = bestLayers };

            // ④ 쓰는 달 수: 처음엔 한 달에 4장씩 꽉 채워 적은 달로, 뒤로 갈수록 12달을 다 쓰고 달마다 장수를 들쭉날쭉하게.
            int cards = best.Count;
            int pairsTotal = cards / 2;
            int full = Math.Max(1, (int)Math.Ceiling(cards / 4.0));
            int months = chapter == 0 ? full : full + chapter / 2 + (int)(p * 2);
            plan.MonthCount = Math.Max(2, Math.Min(Math.Min(Board.Months, pairsTotal), months));

            // ⑤ 쌌다 시간 2.0초 → 1.2초
            float tt = (number - 1) / (float)(StageCount - 1);
            plan.StackWindow = (float)Math.Round(2.0 - tt * 0.8, 2);
            return plan;
        }

        /// <summary>무작위로 끝까지 두어 보는 횟수. 이 가운데 몇 번이나 다 지워지는지로 "막히기 쉬운 판"을 걸러 낸다.</summary>
        public const int Trials = 12;

        public static StageDef Build(int number)
        {
            var plan = MakePlan(number);
            var layout = new Layout(plan.Tiles);
            var slots = new List<int>();
            for (int i = 0; i < layout.Count; i++) slots.Add(i);
            int need = plan.Chapter < 2 ? 6 : plan.Chapter < 6 ? 3 : 2;
            int[] best = null;
            int bestScore = -1;
            for (int attempt = 0; attempt < 200 && bestScore < need; attempt++)
            {
                var rng = new Rng(number * 104729 + attempt * 31 + 3);
                var cards = PickCards(plan, layout.Count, rng);
                var dealt = Rules.Deal(layout, slots, cards, rng);
                if (dealt == null) continue;
                var board = new Board(layout, dealt);
                int wins = Solver.CountClears(board, rng.Next(int.MaxValue), Trials);
                if (wins > bestScore)
                {
                    bestScore = wins;
                    best = dealt;
                }
            }
            if (best == null) throw new InvalidOperationException("could not deal stage " + number);
            return new StageDef
            {
                number = number,
                shape = plan.Shape,
                tiles = layout.Flatten(),
                cells = best,
                stackWindow = plan.StackWindow,
            };
        }

        /// <summary>판에 놓을 패 고르기: 쓸 달을 정하고, 달마다 한 쌍씩 먼저, 남은 쌍은 4장을 채운 뒤 두 벌째(최대 8장)로.</summary>
        public static List<int> PickCards(Plan plan, int total, Rng rng)
        {
            int pairs = total / 2;
            var monthPool = new List<int>();
            for (int m = 0; m < Board.Months; m++) monthPool.Add(m);
            rng.Shuffle(monthPool);
            var months = monthPool.GetRange(0, Math.Min(plan.MonthCount, pairs));

            var count = new int[Board.Months];
            foreach (int m in months) count[m] = 2;
            int left = pairs - months.Count;
            int cap = 4;
            while (left > 0)
            {
                var open = months.FindAll(m => count[m] < cap);
                if (open.Count == 0) { cap += 2; continue; }
                count[open[rng.Next(open.Count)]] += 2;
                left--;
            }

            var cards = new List<int>();
            foreach (int m in months)
            {
                var kinds = new List<int> { 0, 1, 2, 3 };
                rng.Shuffle(kinds);
                for (int i = 0; i < count[m]; i++) cards.Add(m * Board.CardsPerMonth + kinds[i % 4]);
            }
            return cards;
        }
    }
}
