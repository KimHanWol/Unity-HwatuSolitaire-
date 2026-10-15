using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 스테이지 300개의 난이도 계획과 패 배치. 결과는 Tools/StageTool 이 stages.json 으로 굳혀서 게임에 넣는다
    /// (게임은 실행 중에 만들지 않는다 → 모두에게 같은 판, 같은 별 기준).
    ///
    /// 12장(챕터) × 25스테이지. 챕터마다 화투의 달(1월 송학 ~ 12월 비)을 테마로 한다.
    /// 난이도를 올리는 손잡이:
    ///  ① 판 크기 16장 → 80장 (챕터 안에서 커지고, 다음 챕터는 조금 쉬운 데서 다시 시작하는 톱니 모양)
    ///  ② 쓰는 달 수 (적으면 짝 찾기가 쉽다)
    ///  ③ 모양 구멍(1~2장 이후, 중력 없는 판에서만)
    ///  ④ 중력(3장부터): 패를 지우면 남은 패가 한쪽으로 쏠린다
    ///  ⑤ 막힌 칸(5장부터): 지울 수 없는 뒤집힌 패가 길을 막는다
    ///  ⑥ "쌌다" 시간(짝 뒤 셋째 패를 얹을 수 있는 시간)이 2.0초 → 1.2초로 짧아진다
    /// </summary>
    public static class StageGenerator
    {
        public const int ChapterCount = 12;
        public const int StagesPerChapter = 25;
        public const int StageCount = ChapterCount * StagesPerChapter;

        /// <summary>판 크기 단계 (가로, 세로). 세로 화면이라 세로 칸 수가 패 크기를 정한다.</summary>
        public static readonly int[,] Sizes =
        {
            { 4, 4 }, { 4, 5 }, { 6, 4 }, { 6, 5 }, { 6, 6 }, { 7, 6 },
            { 8, 6 }, { 8, 7 }, { 8, 8 }, { 8, 9 }, { 8, 10 },
        };

        /// <summary>챕터별 판 크기 단계의 처음과 끝.</summary>
        static readonly int[,] ChapterSizeRange =
        {
            { 0, 3 }, { 1, 4 }, { 2, 5 }, { 3, 6 }, { 4, 7 }, { 5, 8 },
            { 5, 8 }, { 6, 9 }, { 6, 9 }, { 7, 10 }, { 8, 10 }, { 8, 10 },
        };

        public sealed class Plan
        {
            public int Number;
            public int Chapter;      // 0부터
            public int Width, Height;
            public GravityMode Gravity;
            public bool[] Hole;      // true면 처음부터 빈칸
            public bool[] Blocked;   // true면 막힌 칸
            public int MonthCount;
            public float StackWindow;
            public string Shape = "rect";
        }

        public static int ChapterOf(int number) => (number - 1) / StagesPerChapter;

        public static Plan MakePlan(int number)
        {
            if (number < 1 || number > StageCount) throw new ArgumentOutOfRangeException(nameof(number));
            int chapter = ChapterOf(number);
            int k = (number - 1) % StagesPerChapter;
            float p = k / (float)(StagesPerChapter - 1);
            var rng = new Rng(number * 7919 + 17);

            // ① 판 크기: 챕터 안에서 점점 커진다. 다섯 판마다 한 번 숨 돌리는 작은 판.
            int lo = ChapterSizeRange[chapter, 0], hi = ChapterSizeRange[chapter, 1];
            int sizeIndex = lo + (int)Math.Round((hi - lo) * p);
            if (k % 5 == 4 && k != StagesPerChapter - 1) sizeIndex = Math.Max(lo, sizeIndex - 1);
            var plan = new Plan
            {
                Number = number,
                Chapter = chapter,
                Width = Sizes[sizeIndex, 0],
                Height = Sizes[sizeIndex, 1],
            };
            int cellCount = plan.Width * plan.Height;
            plan.Hole = new bool[cellCount];
            plan.Blocked = new bool[cellCount];

            // ④ 중력
            plan.Gravity = PickGravity(chapter, k, rng);

            // ⑤ 막힌 칸: 좌우 대칭 한 쌍씩 (패 수가 짝수로 남게)
            if (chapter >= 4 && plan.Width >= 6 && k % 4 != 0)
            {
                int pairs = Math.Min(1 + (chapter - 4) / 2 + (int)(p * 2), 4);
                PlaceBlocks(plan, pairs, rng);
            }

            // ③ 모양 구멍: 중력이 없는 판에만 (중력이 있으면 패가 구멍을 메워 버린다)
            if (chapter >= 1 && plan.Gravity == GravityMode.None && k % 3 != 0)
                ApplyShape(plan, rng);

            int cards = 0;
            for (int i = 0; i < cellCount; i++) if (!plan.Hole[i] && !plan.Blocked[i]) cards++;
            if (cards % 2 == 1) throw new InvalidOperationException("odd card count at stage " + number);

            // ② 쓰는 달 수: 처음엔 한 달에 4장씩 꽉 채워 적은 달로, 뒤로 갈수록 12달을 다 쓰고 달마다 장수를 들쭉날쭉하게.
            int pairsTotal = cards / 2;
            int full = Math.Max(1, (int)Math.Ceiling(cards / 4.0));
            int months = chapter == 0 ? full : full + chapter / 2 + (int)(p * 2);
            plan.MonthCount = Math.Max(2, Math.Min(Math.Min(Board.Months, pairsTotal), months));

            // ⑥ 쌌다 시간 2.0초 → 1.2초
            float t = (number - 1) / (float)(StageCount - 1);
            plan.StackWindow = (float)Math.Round(2.0 - t * 0.8, 2);
            return plan;
        }

        static GravityMode PickGravity(int chapter, int k, Rng rng)
        {
            if (chapter < 2) return GravityMode.None;
            if (chapter == 2) return k % 2 == 1 ? GravityMode.Down : GravityMode.None;
            if (chapter == 3)
            {
                GravityMode[] cycle = { GravityMode.None, GravityMode.Down, GravityMode.Left, GravityMode.Right };
                return cycle[k % cycle.Length];
            }
            if (chapter == 4)
            {
                GravityMode[] cycle = { GravityMode.Down, GravityMode.Up, GravityMode.None, GravityMode.Left, GravityMode.Right };
                return cycle[k % cycle.Length];
            }
            if (k % 6 == 0) return GravityMode.None;
            GravityMode[] all =
            {
                GravityMode.Down, GravityMode.Up, GravityMode.Left, GravityMode.Right,
                GravityMode.SplitOut, GravityMode.SplitIn, GravityMode.SplitVertical,
            };
            return all[rng.Next(all.Length)];
        }

        static void PlaceBlocks(Plan plan, int pairs, Rng rng)
        {
            int w = plan.Width, h = plan.Height;
            int placed = 0, guard = 0;
            while (placed < pairs && guard++ < 200)
            {
                int x = rng.Next(1, w / 2);
                int y = rng.Next(1, h - 1);
                int mx = w - 1 - x;
                if (mx == x) continue;
                int a = y * w + x, b = y * w + mx;
                if (plan.Blocked[a] || plan.Blocked[b]) continue;
                // 막힌 칸끼리 붙어서 벽이 되지 않게 한다.
                if (Near(plan, x, y) || Near(plan, mx, y)) continue;
                plan.Blocked[a] = plan.Blocked[b] = true;
                placed++;
            }
        }

        static bool Near(Plan plan, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= plan.Width || ny >= plan.Height) continue;
                    if (plan.Blocked[ny * plan.Width + nx]) return true;
                }
            return false;
        }

        static readonly string[] Shapes = { "corners", "diamond", "center", "windows", "stairs" };

        static void ApplyShape(Plan plan, Rng rng)
        {
            int w = plan.Width, h = plan.Height;
            if (w < 5 || h < 5) return;
            string shape = Shapes[rng.Next(Shapes.Length)];
            plan.Shape = shape;
            void Hole(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                int i = y * w + x;
                if (!plan.Blocked[i]) plan.Hole[i] = true;
            }
            switch (shape)
            {
                case "corners":
                    Hole(0, 0); Hole(w - 1, 0); Hole(0, h - 1); Hole(w - 1, h - 1);
                    break;
                case "diamond":
                    int cut = Math.Min(w, h) >= 7 ? 2 : 1;
                    for (int y = 0; y < cut; y++)
                        for (int x = 0; x < cut - y; x++)
                        {
                            Hole(x, y); Hole(w - 1 - x, y); Hole(x, h - 1 - y); Hole(w - 1 - x, h - 1 - y);
                        }
                    break;
                case "center":
                    int cx = w / 2, cy = h / 2;
                    Hole(cx - 1, cy - 1); Hole(w - cx, cy - 1);
                    if (h % 2 == 0) { Hole(cx - 1, cy); Hole(w - cx, cy); }
                    break;
                case "windows":
                    for (int y = 1; y < h - 1; y += 3)
                    {
                        Hole(1, y); Hole(w - 2, y);
                    }
                    break;
                case "stairs":
                    for (int y = 0; y < Math.Min(3, h / 2); y++)
                    {
                        Hole(y, h - 1 - y); Hole(w - 1 - y, y);
                    }
                    break;
            }
            // 대칭이 아닌 모양에서 홀수가 되면 하나를 되돌린다.
            int cards = 0, last = -1;
            for (int i = 0; i < plan.Hole.Length; i++)
            {
                if (!plan.Hole[i] && !plan.Blocked[i]) cards++;
                if (plan.Hole[i]) last = i;
            }
            if (cards % 2 == 1 && last >= 0) plan.Hole[last] = false;
        }

        /// <summary>무작위로 끝까지 두어 보는 횟수. 이 가운데 몇 번이나 다 지워지는지로 "막히기 쉬운 판"을 걸러 낸다.</summary>
        public const int Trials = 12;

        /// <summary>
        /// 계획대로 패를 깔고, 끝까지 풀리면서 아무렇게나 두어도 잘 막히지 않는 배치가 나올 때까지 다시 깐다.
        /// 앞쪽 챕터일수록 너그러운 판(무작위로 두어도 자주 다 지워지는 판)을 고른다.
        /// </summary>
        public static StageDef Build(int number)
        {
            var plan = MakePlan(number);
            int need = plan.Chapter < 2 ? 6 : plan.Chapter < 6 ? 3 : 2;
            Board best = null;
            int bestScore = 0;
            for (int attempt = 0; attempt < 300 && bestScore < need; attempt++)
            {
                var rng = new Rng(number * 104729 + attempt * 31 + 3);
                var board = Deal(plan, rng);
                if (!Rules.HasMove(board)) continue;
                int wins = Solver.CountClears(board, plan.Gravity, rng.Next(int.MaxValue), Trials);
                if (wins > bestScore)
                {
                    bestScore = wins;
                    best = board;
                }
            }
            if (best == null) throw new InvalidOperationException("could not deal a solvable board for stage " + number);
            return new StageDef
            {
                number = number,
                width = plan.Width,
                height = plan.Height,
                cells = best.ToArray(),
                gravity = (int)plan.Gravity,
                stackWindow = plan.StackWindow,
            };
        }

        public static Board Deal(Plan plan, Rng rng)
        {
            var slots = new List<int>();
            for (int i = 0; i < plan.Width * plan.Height; i++)
                if (!plan.Hole[i] && !plan.Blocked[i]) slots.Add(i);
            int pairs = slots.Count / 2;

            // 쓸 달 고르기
            var monthPool = new List<int>();
            for (int m = 0; m < Board.Months; m++) monthPool.Add(m);
            rng.Shuffle(monthPool);
            var months = monthPool.GetRange(0, plan.MonthCount);

            // 달마다 한 쌍씩 먼저, 남은 쌍은 4장을 채운 뒤 두 벌째(최대 8장)로
            var count = new int[Board.Months];
            foreach (int m in months) count[m] = 2;
            int left = pairs - months.Count;
            int cap = 4;
            while (left > 0)
            {
                var open = months.FindAll(m => count[m] < cap);
                if (open.Count == 0)
                {
                    cap += 2;
                    continue;
                }
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
            rng.Shuffle(cards);

            var values = new int[plan.Width * plan.Height];
            for (int i = 0; i < values.Length; i++) values[i] = plan.Blocked[i] ? Board.Block : Board.Empty;
            for (int i = 0; i < slots.Count; i++) values[slots[i]] = cards[i];
            return new Board(plan.Width, plan.Height, values);
        }
    }
}
