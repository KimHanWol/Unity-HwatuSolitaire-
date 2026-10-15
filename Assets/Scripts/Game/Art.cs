using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그림 모음. 화투패는 Resources/Cards/hwatu_atlas.png(8열 x 7줄, 마지막 줄 첫 칸은 뒷면)에서 잘라 쓰고,
/// 나머지(둥근 판, 아이콘, 담요 무늬, 파티클)는 실행할 때 거리 함수(SDF)로 그린다.
/// </summary>
public static class Art
{
    // ------------------------------------------------------------ 색 (고스톱 판 느낌: 초록 담요, 나무, 금박, 붉은 화투)
    public static readonly Color FeltDark = new Color32(0x0F, 0x4A, 0x2A, 0xFF);
    public static readonly Color Felt = new Color32(0x1E, 0x6E, 0x40, 0xFF);
    public static readonly Color FeltLight = new Color32(0x2E, 0x8C, 0x55, 0xFF);
    public static readonly Color Wood = new Color32(0x5A, 0x34, 0x18, 0xFF);
    public static readonly Color WoodDark = new Color32(0x3A, 0x20, 0x0E, 0xFF);
    public static readonly Color Gold = new Color32(0xF5, 0xC5, 0x4A, 0xFF);
    public static readonly Color GoldDark = new Color32(0xB7, 0x83, 0x1C, 0xFF);
    public static readonly Color Red = new Color32(0xD0, 0x1E, 0x2E, 0xFF);
    public static readonly Color RedDark = new Color32(0x8A, 0x0C, 0x18, 0xFF);
    public static readonly Color Paper = new Color32(0xFF, 0xF5, 0xDF, 0xFF);
    public static readonly Color PaperDark = new Color32(0xE8, 0xD2, 0xA8, 0xFF);
    public static readonly Color Ink = new Color32(0x3B, 0x24, 0x14, 0xFF);
    public static readonly Color Blue = new Color32(0x2D, 0x7F, 0xD8, 0xFF);
    public static readonly Color BlueDark = new Color32(0x1A, 0x4F, 0x91, 0xFF);
    public static readonly Color Green = new Color32(0x3C, 0xB3, 0x5A, 0xFF);
    public static readonly Color GreenDark = new Color32(0x22, 0x77, 0x37, 0xFF);
    public static readonly Color Grey = new Color32(0x9A, 0x8F, 0x80, 0xFF);
    public static readonly Color GreyDark = new Color32(0x63, 0x5A, 0x4E, 0xFF);

    public const int AtlasCols = 8;
    public const int CardW = 258, CardH = 420;
    public const float CardAspect = CardH / (float)CardW;
    public const int BackIndex = 48;

    static Sprite[] cards;

    /// <summary>화투패 그림. 번호 = 월*4 + 그 달의 몇 번째, 48은 뒷면.</summary>
    public static Sprite Card(int index)
    {
        if (cards == null)
        {
            var tex = Resources.Load<Texture2D>("Cards/hwatu_atlas");
            cards = new Sprite[49];
            if (tex == null)
            {
                Debug.LogError("[Art] Resources/Cards/hwatu_atlas.png가 없습니다.");
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < cards.Length; i++)
            {
                int col = i % AtlasCols, row = i / AtlasCols;
                var rect = new Rect(col * CardW, tex.height - (row + 1) * CardH, CardW, CardH);
                cards[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                cards[i].name = "card" + i;
            }
        }
        return cards[Mathf.Clamp(index, 0, cards.Length - 1)];
    }

    public static Sprite Back => Card(BackIndex);

    static Sprite gwang;

    /// <summary>
    /// 광(光) 표시. 별 대신 쓴다 (★★★ → 광 셋). 1월 광 패의 붉은 동그라미를 떼어 낸 그림 (Tools/Cards/gwang.svg).
    /// 아직 못 받은 자리는 GwangEmpty 색으로 칠해 그림자처럼 보인다.
    /// </summary>
    public static Sprite Gwang
    {
        get
        {
            if (gwang == null)
            {
                var tex = Resources.Load<Texture2D>("Cards/gwang");
                gwang = tex != null
                    ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect)
                    : Star;
            }
            return gwang;
        }
    }

    public static readonly Color GwangEmpty = new Color(0f, 0f, 0f, 0.28f);

    // ------------------------------------------------------------ 거리 함수 그림
    delegate float Sdf(Vector2 p);

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>
    /// size x size 텍스처에 거리 함수를 칠한다. 좌표는 가운데가 (0,0), 가장자리가 ±1.
    /// border를 주면 9-slice 테두리(픽셀)로 쓴다.
    /// </summary>
    static Sprite Paint(string name, int size, Sdf f, int border = 0, Func<Vector2, float, Color> shade = null)
    {
        if (cache.TryGetValue(name, out var s)) return s;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
        var px = new Color32[size * size];
        float scale = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / scale - 1f, (y + 0.5f) / scale - 1f);
                float d = f(p);
                float a = Mathf.Clamp01(0.5f - d * scale);
                Color c = shade != null ? shade(p, d) : Color.white;
                c.a *= a;
                px[y * size + x] = c;
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        s.name = name;
        cache[name] = s;
        return s;
    }

    static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

    static float Box(Vector2 p, Vector2 c, Vector2 half, float round = 0f)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(round, round);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
    }

    static float Segment(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }

    static float Polygon(Vector2 p, Vector2[] v)
    {
        float d = Vector2.Dot(p - v[0], p - v[0]);
        float s = 1f;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            Vector2 e = v[j] - v[i], w = p - v[i];
            Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, Vector2.Dot(b, b));
            bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
        }
        return s * Mathf.Sqrt(d);
    }

    static Vector2 Rot(Vector2 p, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
    }

    static Vector2[] StarPoints(float outer, float inner, int points = 5)
    {
        var v = new Vector2[points * 2];
        for (int i = 0; i < v.Length; i++)
        {
            float r = i % 2 == 0 ? outer : inner;
            float a = Mathf.PI / 2f + i * Mathf.PI / points;
            v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        return v;
    }

    // ------------------------------------------------------------ 모양
    /// <summary>둥근 사각형 (9-slice). 반지름 큰 것.</summary>
    public static Sprite Round => Paint("round", 96, p => Box(p, Vector2.zero, Vector2.one, 0.6f), 30);

    /// <summary>둥근 사각형 (9-slice). 반지름 작은 것.</summary>
    public static Sprite RoundSmall => Paint("roundSmall", 64, p => Box(p, Vector2.zero, Vector2.one, 0.45f), 15);

    /// <summary>둥근 사각형 테두리만 (선택 표시용 9-slice).</summary>
    public static Sprite RoundOutline => Paint("roundOutline", 96, p => Mathf.Abs(Box(p, Vector2.zero, Vector2.one * 0.93f, 0.5f)) - 0.07f, 30);

    public static Sprite Circle128 => Paint("circle", 128, p => Circle(p, Vector2.zero, 0.98f));

    public static Sprite Ring => Paint("ring", 128, p => Mathf.Abs(Circle(p, Vector2.zero, 0.88f)) - 0.1f);

    /// <summary>가운데가 밝고 가장자리로 갈수록 사라지는 빛.</summary>
    public static Sprite Glow => Paint("glow", 128, p => -1f, 0, (p, d) =>
    {
        float r = Mathf.Clamp01(1f - p.magnitude);
        return new Color(1f, 1f, 1f, r * r);
    });

    /// <summary>가장자리 어둡게 (화면 전체 비네트, 9-slice 아님).</summary>
    public static Sprite Vignette => Paint("vignette", 128, p => -1f, 0, (p, d) =>
    {
        float r = Mathf.Clamp01((Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y) * 0.9f) - 0.35f) / 0.65f);
        return new Color(0f, 0f, 0f, r * r * 0.6f);
    });

    public static Sprite Star => Paint("star", 128, p => Polygon(p, StarPoints(0.98f, 0.45f)) - 0.02f);

    public static Sprite Petal => Paint("petal", 64, p => (new Vector2(p.x * 1.7f, p.y)).magnitude - 0.9f);

    public static Sprite Spark => Paint("spark", 64, p => Mathf.Min(Box(p, Vector2.zero, new Vector2(0.12f, 0.95f), 0.1f), Box(p, Vector2.zero, new Vector2(0.95f, 0.12f), 0.1f)));

    // ------------------------------------------------------------ 아이콘 (흰색, 쓰는 쪽에서 색을 입힌다)
    public static Sprite IconPause => Paint("iPause", 96, p => Mathf.Min(Box(p, new Vector2(-0.32f, 0), new Vector2(0.16f, 0.62f), 0.1f), Box(p, new Vector2(0.32f, 0), new Vector2(0.16f, 0.62f), 0.1f)));

    public static Sprite IconBack => Paint("iBack", 96, p => Mathf.Min(
        Segment(p, new Vector2(0.3f, 0.6f), new Vector2(-0.3f, 0f), 0.14f),
        Segment(p, new Vector2(-0.3f, 0f), new Vector2(0.3f, -0.6f), 0.14f)));

    public static Sprite IconPlay => Paint("iPlay", 96, p => Polygon(p, new[] { new Vector2(-0.4f, -0.62f), new Vector2(0.62f, 0f), new Vector2(-0.4f, 0.62f) }) - 0.06f);

    public static Sprite IconGrid => Paint("iGrid", 96, p => Mathf.Min(
        Mathf.Min(Box(p, new Vector2(-0.33f, 0.33f), new Vector2(0.24f, 0.24f), 0.08f), Box(p, new Vector2(0.33f, 0.33f), new Vector2(0.24f, 0.24f), 0.08f)),
        Mathf.Min(Box(p, new Vector2(-0.33f, -0.33f), new Vector2(0.24f, 0.24f), 0.08f), Box(p, new Vector2(0.33f, -0.33f), new Vector2(0.24f, 0.24f), 0.08f))));

    public static Sprite IconRetry => Paint("iRetry", 96, p =>
    {
        float ring = Mathf.Abs(Circle(p, Vector2.zero, 0.55f)) - 0.12f;
        // 오른쪽 위를 비워 화살표 자리를 낸다.
        float cut = Polygon(p, new[] { Vector2.zero, new Vector2(1.2f, 0.25f), new Vector2(1.2f, 1.2f), new Vector2(0.25f, 1.2f) });
        float arc = Mathf.Max(ring, -cut);
        float head = Polygon(p, new[] { new Vector2(0.18f, 0.88f), new Vector2(0.2f, 0.28f), new Vector2(0.72f, 0.55f) }) - 0.03f;
        return Mathf.Min(arc, head);
    });

    public static Sprite IconGear => Paint("iGear", 96, p =>
    {
        float body = Circle(p, Vector2.zero, 0.56f);
        for (int i = 0; i < 8; i++)
            body = Mathf.Min(body, Box(Rot(p, i * 45f), new Vector2(0f, 0.68f), new Vector2(0.14f, 0.2f), 0.05f));
        return Mathf.Max(body, -Circle(p, Vector2.zero, 0.24f));
    });

    public static Sprite IconBulb => Paint("iBulb", 96, p =>
    {
        float bulb = Circle(p, new Vector2(0f, 0.2f), 0.5f);
        float neck = Box(p, new Vector2(0f, -0.38f), new Vector2(0.24f, 0.2f), 0.06f);
        float glass = Mathf.Min(bulb, Box(p, new Vector2(0f, -0.15f), new Vector2(0.28f, 0.15f), 0f));
        float shape = Mathf.Min(glass, neck);
        float gap = Box(p, new Vector2(0f, -0.38f), new Vector2(0.3f, 0.025f), 0f);
        float baseCap = Box(p, new Vector2(0f, -0.66f), new Vector2(0.14f, 0.07f), 0.05f);
        return Mathf.Min(Mathf.Max(shape, -gap), baseCap);
    });

    public static Sprite IconBomb => Paint("iBomb", 128, p =>
    {
        float body = Circle(p, new Vector2(-0.08f, -0.12f), 0.66f);
        float cap = Box(Rot(p - new Vector2(0.36f, 0.42f), 45f), Vector2.zero, new Vector2(0.17f, 0.12f), 0.03f);
        float fuse = Segment(p, new Vector2(0.48f, 0.56f), new Vector2(0.66f, 0.8f), 0.05f);
        return Mathf.Min(Mathf.Min(body, cap), fuse);
    }, 0, (p, d) =>
    {
        // 몸통은 검게, 왼쪽 위에 반짝임
        float hi = Mathf.Clamp01(1f - (p - new Vector2(-0.3f, 0.12f)).magnitude / 0.28f);
        Color c = Color.Lerp(new Color(0.12f, 0.12f, 0.16f), new Color(0.75f, 0.75f, 0.85f), hi * 0.8f);
        if (p.y > 0.45f && p.x > 0.4f) c = new Color(0.85f, 0.7f, 0.45f);
        return c;
    });

    public static Sprite IconLock => Paint("iLock", 96, p =>
    {
        float body = Box(p, new Vector2(0f, -0.25f), new Vector2(0.52f, 0.42f), 0.1f);
        float shackle = Mathf.Abs(Circle(p, new Vector2(0f, 0.2f), 0.32f)) - 0.09f;
        shackle = Mathf.Max(shackle, -(p.y - 0.15f));
        float hole = Circle(p, new Vector2(0f, -0.22f), 0.1f);
        return Mathf.Max(Mathf.Min(body, shackle), -hole);
    });

    public static Sprite IconCheck => Paint("iCheck", 96, p => Mathf.Min(
        Segment(p, new Vector2(-0.55f, 0f), new Vector2(-0.15f, -0.42f), 0.13f),
        Segment(p, new Vector2(-0.15f, -0.42f), new Vector2(0.58f, 0.45f), 0.13f)));

    public static Sprite IconClose => Paint("iClose", 96, p => Mathf.Min(
        Segment(p, new Vector2(-0.5f, -0.5f), new Vector2(0.5f, 0.5f), 0.13f),
        Segment(p, new Vector2(-0.5f, 0.5f), new Vector2(0.5f, -0.5f), 0.13f)));

    public static Sprite IconClock => Paint("iClock", 96, p =>
    {
        float ring = Mathf.Abs(Circle(p, Vector2.zero, 0.72f)) - 0.11f;
        float hands = Mathf.Min(Segment(p, Vector2.zero, new Vector2(0f, 0.45f), 0.08f), Segment(p, Vector2.zero, new Vector2(0.32f, 0f), 0.08f));
        return Mathf.Min(ring, hands);
    });

    /// <summary>광고 보기(영상) 표시.</summary>
    public static Sprite IconVideo => Paint("iVideo", 96, p =>
    {
        float body = Box(p, new Vector2(-0.12f, 0f), new Vector2(0.5f, 0.38f), 0.1f);
        float lens = Polygon(p, new[] { new Vector2(0.38f, 0f), new Vector2(0.85f, 0.32f), new Vector2(0.85f, -0.32f) });
        return Mathf.Min(body, lens);
    });

    // ------------------------------------------------------------ 담요
    static Texture2D felt;

    /// <summary>고스톱 담요 같은 초록 융 무늬 (타일).</summary>
    public static Texture2D FeltTexture
    {
        get
        {
            if (felt != null) return felt;
            const int size = 256;
            felt = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "felt" };
            var rng = new System.Random(7);
            var noise = new float[size * size];
            for (int i = 0; i < noise.Length; i++) noise[i] = (float)rng.NextDouble();
            var px = new Color32[size * size];
            Color baseColor = Felt;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // 가로·세로로 살짝 번진 잡음 = 섬유결
                    float n = noise[y * size + x] * 0.5f
                              + noise[y * size + (x + 1) % size] * 0.25f
                              + noise[((y + 1) % size) * size + x] * 0.25f;
                    float weave = ((x + y) % 4 < 2 ? 0.015f : -0.015f);
                    float k = (n - 0.5f) * 0.12f + weave;
                    px[y * size + x] = new Color(baseColor.r + k, baseColor.g + k * 1.2f, baseColor.b + k, 1f);
                }
            felt.SetPixels32(px);
            felt.Apply(true, true);
            return felt;
        }
    }

    /// <summary>달 이름. 0부터.</summary>
    public static string MonthName(int month)
    {
        string[] ko = { "송학", "매조", "벚꽃", "흑싸리", "난초", "모란", "홍싸리", "공산", "국진", "단풍", "오동", "비" };
        string[] en = { "Pine", "Plum", "Cherry", "Wisteria", "Iris", "Peony", "Clover", "Moon", "Mum", "Maple", "Paulownia", "Rain" };
        return Loc.T(ko[month], en[month]);
    }

    /// <summary>달마다 대표 패(광이나 열끗).</summary>
    public static int MonthHero(int month) => month * 4;

    /// <summary>달마다 테마 색 (스테이지 목록 머리띠).</summary>
    public static Color MonthColor(int month)
    {
        Color[] c =
        {
            new Color32(0x2F, 0x7D, 0x4F, 0xFF), new Color32(0xC8, 0x3B, 0x52, 0xFF), new Color32(0xE0, 0x6E, 0x9A, 0xFF),
            new Color32(0x5B, 0x4A, 0x8C, 0xFF), new Color32(0x3E, 0x6F, 0xC2, 0xFF), new Color32(0xC2, 0x2E, 0x3C, 0xFF),
            new Color32(0xB8, 0x4A, 0x2E, 0xFF), new Color32(0x3A, 0x3F, 0x5C, 0xFF), new Color32(0xD9, 0x9A, 0x1E, 0xFF),
            new Color32(0xC7, 0x4B, 0x16, 0xFF), new Color32(0x6B, 0x4A, 0x8F, 0xFF), new Color32(0x2C, 0x5E, 0x7A, 0xFF),
        };
        return c[Mathf.Clamp(month, 0, 11)];
    }
}
