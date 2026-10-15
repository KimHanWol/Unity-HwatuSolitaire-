using System;
using System.Collections.Generic;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 판 모양 만들기. 모두 가로 W칸, 세로 H칸, L층을 받아 자리 목록을 돌려준다 (반 칸 단위).
    /// 세로 화면에 화투패가 길쭉해서 판은 가로 7칸, 세로 6칸 안쪽으로 쓴다.
    /// </summary>
    public static class Layouts
    {
        public static readonly string[] Names = { "pyramid", "steps", "diamond", "twin", "cross", "frame", "tower" };

        public static List<Tile> Build(string name, int w, int h, int layers)
        {
            var t = new List<Tile>();
            switch (name)
            {
                case "pyramid":
                    // 층마다 반 칸씩 안으로 들어가며 한 칸씩 줄어든다: 위 패가 아래 두 장(또는 네 장) 사이에 걸친다.
                    for (int z = 0; z < layers && w - z > 0 && h - z > 0; z++) Rect(t, z, z, z, w - z, h - z);
                    break;
                case "steps":
                    // 층마다 한 칸씩 안으로 들어가며 두 칸씩 줄어든다: 위 패가 아래 패 바로 위에 포개진다.
                    for (int z = 0; z < layers && w - 2 * z > 0 && h - 2 * z > 0; z++) Rect(t, z, 2 * z, 2 * z, w - 2 * z, h - 2 * z);
                    break;
                case "diamond":
                    for (int z = 0; z < layers && w - 2 * z >= 2 && h - 2 * z >= 2; z++) Diamond(t, z, 2 * z, 2 * z, w - 2 * z, h - 2 * z);
                    break;
                case "twin":
                    // 바닥은 꽉 찬 사각형, 그 위로 왼쪽·오른쪽 두 봉우리
                    Rect(t, 0, 0, 0, w, h);
                    int half = w / 2;
                    for (int z = 1; z < layers; z++)
                    {
                        if (half - z <= 0 || h - z <= 0) break;
                        Rect(t, z, z, z, half - z, h - z);
                        Rect(t, z, 2 * (w - half) + z, z, half - z, h - z);
                    }
                    break;
                case "cross":
                    // 바닥 십자 위로, 가로·세로 띠가 겹치는 가운데에 피라미드
                    var (bx, by, bw, bh) = Cross(t, 0, w, h);
                    for (int z = 1; z < layers && bw - z > 0 && bh - z > 0; z++) Rect(t, z, 2 * bx + z, 2 * by + z, bw - z, bh - z);
                    break;
                case "frame":
                    // 바닥 꽉 찬 사각형 → 반 칸 어긋난 테두리 → 그 네 귀퉁이
                    Rect(t, 0, 0, 0, w, h);
                    if (layers > 1 && w > 2 && h > 2)
                    {
                        for (int i = 0; i < w - 1; i++)
                            for (int j = 0; j < h - 1; j++)
                                if (i == 0 || j == 0 || i == w - 2 || j == h - 2) t.Add(new Tile(1 + 2 * i, 1 + 2 * j, 1));
                    }
                    if (layers > 2 && w > 3 && h > 3)
                    {
                        t.Add(new Tile(1, 1, 2)); t.Add(new Tile(1 + 2 * (w - 2), 1, 2));
                        t.Add(new Tile(1, 1 + 2 * (h - 2), 2)); t.Add(new Tile(1 + 2 * (w - 2), 1 + 2 * (h - 2), 2));
                    }
                    if (layers > 3 && w > 3 && h > 3) Rect(t, 2, 2 * ((w - 2) / 2), 2 * ((h - 2) / 2), 2, 2);
                    break;
                case "tower":
                    // 바닥 사각형 + 한 층 덜어 낸 사각형 + 가운데 2x2 탑이 높이 쌓인다.
                    Rect(t, 0, 0, 0, w, h);
                    if (layers > 1 && w > 2 && h > 2) Rect(t, 1, 1, 1, w - 1, h - 1);
                    int cx = w - 2, cy = h - 2;
                    for (int z = 2; z < layers; z++) Rect(t, z, cx, cy, 2, 2);
                    break;
                default:
                    throw new ArgumentException("unknown layout " + name);
            }
            // 같은 자리 중복 제거
            var seen = new HashSet<(int, int, int)>();
            var result = new List<Tile>();
            foreach (var tile in t)
                if (seen.Add((tile.X, tile.Y, tile.Z))) result.Add(tile);
            // 위층 패는 아래에 받쳐 주는 패가 있어야 한다 (공중에 뜬 패 없애기).
            result.RemoveAll(a => a.Z > 0 && !result.Exists(b => b.Z == a.Z - 1 && a.Overlaps(b)));
            return result;
        }

        static void Rect(List<Tile> t, int z, int x0, int y0, int w, int h)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++) t.Add(new Tile(x0 + 2 * i, y0 + 2 * j, z));
        }

        /// <summary>가운데 줄이 가장 넓고 위아래로 갈수록 좁아지는 마름모. 좁은 줄은 반 칸 어긋나 가운데에 맞춘다.</summary>
        static void Diamond(List<Tile> t, int z, int x0, int y0, int w, int h)
        {
            float mid = (h - 1) / 2f;
            for (int j = 0; j < h; j++)
            {
                int rowW = Math.Max(2, w - (int)Math.Round(Math.Abs(j - mid) * 2));
                int offset = w - rowW; // 반 칸 단위: (w - rowW) 칸 차이의 절반 = (w-rowW) 반 칸
                for (int i = 0; i < rowW; i++) t.Add(new Tile(x0 + offset + 2 * i, y0 + 2 * j, z));
            }
        }

        /// <summary>가운데 세로 띠와 가로 띠가 겹친 십자.</summary>
        static (int bx, int by, int bw, int bh) Cross(List<Tile> t, int z, int w, int h)
        {
            int bandW = Math.Max(2, w / 2 - (w >= 6 ? 1 : 0)), bandH = Math.Max(2, h / 2 - (h >= 6 ? 1 : 0));
            int bx0 = (w - bandW) / 2, by0 = (h - bandH) / 2;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    bool inV = i >= bx0 && i < bx0 + bandW;
                    bool inH = j >= by0 && j < by0 + bandH;
                    if (inV || inH) t.Add(new Tile(2 * i, 2 * j, z));
                }
            return (bx0, by0, bandW, bandH);
        }

        /// <summary>판의 가로·세로 칸 수 (반 칸 걸친 것 포함).</summary>
        public static (float w, float h) Footprint(List<Tile> tiles)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var t in tiles)
            {
                minX = Math.Min(minX, t.X); minY = Math.Min(minY, t.Y);
                maxX = Math.Max(maxX, t.X + 2); maxY = Math.Max(maxY, t.Y + 2);
            }
            return ((maxX - minX) / 2f, (maxY - minY) / 2f);
        }
    }
}
