using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    public readonly struct Move
    {
        public readonly int A, B;
        public Move(int a, int b) { A = a; B = b; }
        public override string ToString() => A + "-" + B;
    }

    /// <summary>둘 수 있는 짝 찾기, 짝 지우기, 섞기, 풀리는 배치 만들기.</summary>
    public static class Rules
    {
        /// <summary>열린 패끼리 같은 달인 짝을 모두 찾는다. skipMonth가 true인 달은 뺀다 (폭탄이 걸린 달).</summary>
        public static List<Move> FindMoves(Board board, bool[] skipMonth = null, int limit = int.MaxValue)
        {
            var moves = new List<Move>();
            var byMonth = new List<int>[Board.Months];
            for (int i = 0; i < board.Count; i++)
            {
                if (!board.Free(i)) continue;
                int m = Board.Month(board[i]);
                if (skipMonth != null && skipMonth[m]) continue;
                (byMonth[m] ?? (byMonth[m] = new List<int>())).Add(i);
            }
            for (int m = 0; m < Board.Months; m++)
            {
                var list = byMonth[m];
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        moves.Add(new Move(list[i], list[j]));
                        if (moves.Count >= limit) return moves;
                    }
            }
            return moves;
        }

        public static bool HasMove(Board board, bool[] skipMonth = null) => FindMoves(board, skipMonth, 1).Count > 0;

        public static bool CanMatch(Board board, int a, int b)
            => a != b && board.Free(a) && board.Free(b) && Board.Month(board[a]) == Board.Month(board[b]);

        public static void Remove(Board board, int a, int b)
        {
            board[a] = Board.Empty;
            board[b] = Board.Empty;
        }

        /// <summary>
        /// 끝까지 풀리는 배치를 거꾸로 만든다: 남은 자리 가운데 "열린" 두 자리를 골라 짝 하나를 넣고 그 자리를 비우기를 되풀이한다.
        /// 이렇게 고른 순서를 그대로 거꾸로(넣은 순서대로) 지우면 판이 다 지워지므로, 만든 배치는 항상 풀린다.
        /// slots: 패를 놓을 자리(나머지는 빈 자리로 본다), cards: 놓을 패 (달마다 짝수 장).
        /// 막히면(열린 자리가 2개 미만) null.
        /// </summary>
        public static int[] Deal(Layout layout, IList<int> slots, IList<int> cards, Rng rng)
        {
            var result = new int[layout.Count];
            var probe = new int[layout.Count];
            for (int i = 0; i < probe.Length; i++) result[i] = probe[i] = Board.Empty;
            foreach (int s in slots) probe[s] = 0; // 아직 채울 자리 = "있는 패"로 본다
            var board = new Board(layout, probe);

            // 달마다 짝을 지어 놓고 순서를 섞는다.
            var byMonth = new List<int>[Board.Months];
            foreach (int c in cards) (byMonth[Board.Month(c)] ?? (byMonth[Board.Month(c)] = new List<int>())).Add(c);
            var pairs = new List<(int, int)>();
            foreach (var list in byMonth)
            {
                if (list == null) continue;
                if (list.Count % 2 == 1) return null;
                rng.Shuffle(list);
                for (int i = 0; i < list.Count; i += 2) pairs.Add((list[i], list[i + 1]));
            }
            rng.Shuffle(pairs);

            var open = new List<int>();
            int left = slots.Count;
            foreach (var (c1, c2) in pairs)
            {
                open.Clear();
                for (int i = 0; i < layout.Count; i++) if (board.Free(i)) open.Add(i);
                if (open.Count < 2) return null;
                // 열린 자리 가운데 아무 데나 두 자리를 고르되, 고른 뒤 남은 판에 열린 자리가 두 개 이상 남는 쪽으로
                // (한 장이 다른 한 장을 바로 덮은 채 둘만 남으면 더 이상 짝을 넣을 수 없다). 몇 번 해 보고 안 되면 그냥 간다.
                int a = -1, b = -1;
                for (int tries = 0; tries < 12; tries++)
                {
                    a = open[rng.Next(open.Count)];
                    do b = open[rng.Next(open.Count)]; while (b == a);
                    if (left == 2) break;
                    board[a] = Board.Empty;
                    board[b] = Board.Empty;
                    int nowOpen = 0;
                    for (int i = 0; i < layout.Count && nowOpen < 2; i++) if (board.Free(i)) nowOpen++;
                    board[a] = 0;
                    board[b] = 0;
                    if (nowOpen >= 2) break;
                }
                result[a] = c1;
                result[b] = c2;
                board[a] = Board.Empty;
                board[b] = Board.Empty;
                left -= 2;
            }
            return result;
        }

        /// <summary>
        /// 싹쓸이가 반드시 되는 배치 만들기. Deal과 같이 "지우는 순서"를 거꾸로 따라가며 놓되, 그 순서 중간에
        /// 싹쓸이 달(sweepMonths, 각 4장)마다 "짝 + 짝을 지운 뒤 열리는 셋째 패" 묶음을 한 번씩 끼워 넣고(= 쌌다),
        /// 맨 마지막에 남는 세 자리에 그 달들의 넷째 패(= 폭탄을 쓸 패)를 둔다.
        /// 이 순서대로 두면 쌌다 셋 → 폭탄 달 패만 남음 → 폭탄 마무리 → 싹쓸이가 된다.
        /// order 를 주면 지우는 순서(묶음마다 자리 번호들, 마지막은 남는 세 자리)를 채워 준다 (테스트용).
        /// 막히면 null.
        /// </summary>
        public static int[] DealWithSweep(Layout layout, IList<int> slots, IList<int> cards, IList<int> sweepMonths, Rng rng, List<int[]> order = null)
        {
            var result = new int[layout.Count];
            var probe = new int[layout.Count];
            for (int i = 0; i < probe.Length; i++) result[i] = probe[i] = Board.Empty;
            foreach (int s in slots) probe[s] = 0;
            var board = new Board(layout, probe);

            var byMonth = new List<int>[Board.Months];
            foreach (int c in cards) (byMonth[Board.Month(c)] ?? (byMonth[Board.Month(c)] = new List<int>())).Add(c);
            var groups = new List<int[]>();
            var singles = new List<int>();
            for (int m = 0; m < Board.Months; m++)
            {
                var list = byMonth[m];
                if (list == null) continue;
                rng.Shuffle(list);
                if (sweepMonths.Contains(m))
                {
                    if (list.Count != 4) return null;
                    singles.Add(list[3]);
                    continue;
                }
                if (list.Count % 2 == 1) return null;
                for (int i = 0; i < list.Count; i += 2) groups.Add(new[] { list[i], list[i + 1] });
            }
            rng.Shuffle(groups);
            // 쌌다 묶음(셋)을 아무 자리에나 끼워 넣는다.
            foreach (int m in sweepMonths)
            {
                var list = byMonth[m];
                groups.Insert(rng.Next(groups.Count + 1), new[] { list[0], list[1], list[2] });
            }

            int left = slots.Count;
            foreach (var group in groups)
            {
                int[] picked = null;
                for (int tries = 0; tries < 16 && picked == null; tries++)
                {
                    var open = FreeSlots(board);
                    if (open.Count < 2) return null;
                    int a = open[rng.Next(open.Count)];
                    int b;
                    do b = open[rng.Next(open.Count)]; while (b == a);
                    board[a] = Board.Empty;
                    board[b] = Board.Empty;
                    int c = -1;
                    if (group.Length == 3)
                    {
                        // 셋째는 짝을 지운 뒤에 열려 있어야 한다.
                        var after = FreeSlots(board);
                        if (after.Count > 0) c = after[rng.Next(after.Count)];
                        if (c >= 0) board[c] = Board.Empty;
                    }
                    bool ok = group.Length == 2 || c >= 0;
                    // 남은 판에 열린 자리가 둘 이상 남아야 다음 짝을 넣을 수 있다 (마지막 남는 세 자리는 상관없다).
                    int remain = left - group.Length;
                    if (ok && remain > singles.Count && FreeSlots(board).Count < 2 && tries < 15) ok = false;
                    if (ok) picked = c >= 0 ? new[] { a, b, c } : new[] { a, b };
                    else
                    {
                        board[a] = 0;
                        board[b] = 0;
                        if (c >= 0) board[c] = 0;
                    }
                }
                if (picked == null) return null;
                for (int k = 0; k < picked.Length; k++) result[picked[k]] = group[k];
                left -= picked.Length;
                order?.Add(picked);
            }

            // 남은 자리 = 폭탄을 쓸 넷째 패들
            var rest = new List<int>();
            for (int i = 0; i < layout.Count; i++) if (board.Present(i)) rest.Add(i);
            if (rest.Count != singles.Count) return null;
            for (int k = 0; k < rest.Count; k++) result[rest[k]] = singles[k];
            order?.Add(rest.ToArray());
            return result;
        }

        static List<int> FreeSlots(Board board)
        {
            var list = new List<int>();
            for (int i = 0; i < board.Count; i++) if (board.Free(i)) list.Add(i);
            return list;
        }

        /// <summary>
        /// 막혔을 때 자동 섞기. 남은 패를 남은 자리 안에서 Deal과 같은 방법으로 다시 나눠, 섞은 판도 끝까지 풀리게 한다.
        /// 남은 패가 위아래로 포개진 채 몇 장 안 남으면(예: 두 장이 서로 덮고 있으면) 그 자리로는 풀 수가 없어서,
        /// 그때는 남은 패를 서로 겹치지 않는 맨 아래층 자리로 옮겨 펼친다.
        /// 돌려주는 값: 자리 옮김표 move[원래 자리] = 새 자리 (안 옮긴 패는 자기 자리, 패가 없던 자리는 -1).
        /// </summary>
        public static int[] Shuffle(Board board, Rng rng)
        {
            var slots = board.PresentTiles();
            var cards = new List<int>();
            foreach (int s in slots) cards.Add(board[s]);
            var move = new int[board.Count];
            for (int i = 0; i < move.Length; i++) move[i] = board.Present(i) ? i : -1;

            for (int t = 0; t < 60; t++)
            {
                var dealt = Deal(board.Layout, slots, cards, rng);
                if (dealt == null) continue;
                foreach (int s in slots) board[s] = dealt[s];
                return move;
            }

            // 맨 아래층(겹치지 않는 자리) 가운데 판 가운데에 가까운 자리부터 골라 펼친다.
            var layout = board.Layout;
            var floor = new List<int>();
            for (int i = 0; i < layout.Count; i++) if (layout.Tiles[i].Z == 0) floor.Add(i);
            float cx = (layout.MinX + layout.MaxX) / 2f - 1f, cy = (layout.MinY + layout.MaxY) / 2f - 1f;
            floor.Sort((p, q) =>
            {
                float dp = (layout.Tiles[p].X - cx) * (layout.Tiles[p].X - cx) + (layout.Tiles[p].Y - cy) * (layout.Tiles[p].Y - cy);
                float dq = (layout.Tiles[q].X - cx) * (layout.Tiles[q].X - cx) + (layout.Tiles[q].Y - cy) * (layout.Tiles[q].Y - cy);
                return dp.CompareTo(dq);
            });
            if (floor.Count >= slots.Count)
            {
                var target = floor.GetRange(0, slots.Count);
                rng.Shuffle(cards);
                for (int i = 0; i < move.Length; i++) { board[i] = Board.Empty; move[i] = -1; }
                for (int i = 0; i < slots.Count; i++)
                {
                    move[slots[i]] = target[i];
                    board[target[i]] = cards[i];
                }
                return move;
            }

            // 그래도 안 되면(남은 패가 바닥 자리보다 많을 때) 그냥 뒤섞는다.
            rng.Shuffle(cards);
            for (int i = 0; i < slots.Count; i++) board[slots[i]] = cards[i];
            return move;
        }
    }
}
