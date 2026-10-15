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
            foreach (var (c1, c2) in pairs)
            {
                open.Clear();
                for (int i = 0; i < layout.Count; i++) if (board.Free(i)) open.Add(i);
                if (open.Count < 2) return null;
                int a = open[rng.Next(open.Count)];
                open.Remove(a);
                // 바로 옆 자리끼리만 짝이 되면 너무 쉬워서, 열린 자리 가운데 아무 데나 고른다.
                int b = open[rng.Next(open.Count)];
                result[a] = c1;
                result[b] = c2;
                board[a] = Board.Empty;
                board[b] = Board.Empty;
            }
            return result;
        }

        /// <summary>
        /// 남은 패를 남은 자리 안에서 다시 나눈다 (막혔을 때 자동 섞기). Deal과 같은 방법이라 섞은 판도 항상 끝까지 풀린다.
        /// </summary>
        public static void Shuffle(Board board, Rng rng)
        {
            var slots = board.PresentTiles();
            var cards = new List<int>();
            foreach (int s in slots) cards.Add(board[s]);
            for (int t = 0; t < 60; t++)
            {
                var dealt = Deal(board.Layout, slots, cards, rng);
                if (dealt == null) continue;
                foreach (int s in slots) board[s] = dealt[s];
                return;
            }
            // 아주 드물게 실패하면 그냥 뒤섞는다.
            rng.Shuffle(cards);
            for (int i = 0; i < slots.Count; i++) board[slots[i]] = cards[i];
        }
    }
}
