using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 효과: 꽃잎·불꽃 파티클, 퍼지는 고리, 번쩍임, 떠오르는 글자, 도장 찍기, 화면 흔들기.
/// 모든 위치는 월드 좌표(RectTransform.position)로 받는다. 효과는 팝업보다 위 층에 그린다.
/// </summary>
public class Fx : MonoBehaviour
{
    struct Particle
    {
        public Image image;
        public Vector2 pos, vel;
        public float life, maxLife, spin, gravity, size0, size1, drag;
        public Color color;
    }

    static Fx instance;
    RectTransform layer;
    readonly List<Particle> live = new List<Particle>();
    readonly Stack<Image> pool = new Stack<Image>();

    public static RectTransform Layer => instance != null ? instance.layer : null;

    public static readonly Color[] FlowerColors =
    {
        new Color32(0xFF, 0x4D, 0x5E, 0xFF), new Color32(0xFF, 0x9E, 0xB5, 0xFF), new Color32(0xFF, 0xD1, 0x4A, 0xFF),
        new Color32(0xFF, 0xFF, 0xFF, 0xFF), new Color32(0xE8, 0x2A, 0x3A, 0xFF),
    };

    public static readonly Color[] FireColors =
    {
        new Color32(0xFF, 0xF2, 0x9E, 0xFF), new Color32(0xFF, 0xC1, 0x3B, 0xFF), new Color32(0xFF, 0x7A, 0x1F, 0xFF),
        new Color32(0xE8, 0x2A, 0x2A, 0xFF), new Color32(0xFF, 0xFF, 0xFF, 0xFF),
    };

    public static void Create(RectTransform canvasRoot)
    {
        var go = new GameObject("Fx", typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvasRoot, false);
        Ui.Stretch(rt);
        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;
        instance = go.AddComponent<Fx>();
        instance.layer = rt;
    }

    static Vector2 Local(Vector3 world)
    {
        Vector3 p = instance.layer.InverseTransformPoint(world);
        return new Vector2(p.x, p.y);
    }

    Image Take(Sprite sprite)
    {
        Image img;
        if (pool.Count > 0)
        {
            img = pool.Pop();
            img.gameObject.SetActive(true);
        }
        else
        {
            img = Ui.Image(layer, sprite, Color.white, "P");
        }
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.transform.SetAsLastSibling();
        return img;
    }

    /// <summary>한 점에서 사방으로 튀는 파티클.</summary>
    public static void Burst(Vector3 world, int count, Color[] colors, float speed, Sprite sprite = null, float life = 0.8f, float size = 34f, float gravity = -1600f)
    {
        if (instance == null) return;
        Vector2 at = Local(world);
        sprite = sprite != null ? sprite : Art.Petal;
        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float s = speed * Random.Range(0.35f, 1f);
            var p = new Particle
            {
                image = instance.Take(sprite),
                pos = at,
                vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s + Vector2.up * speed * 0.25f,
                life = 0f,
                maxLife = life * Random.Range(0.7f, 1.2f),
                spin = Random.Range(-540f, 540f),
                gravity = gravity,
                size0 = size * Random.Range(0.7f, 1.3f),
                size1 = size * 0.2f,
                drag = 2.2f,
                color = colors[Random.Range(0, colors.Length)],
            };
            p.image.rectTransform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
            instance.live.Add(p);
        }
    }

    /// <summary>화면 위에서 흩날리는 꽃잎 (클리어 축하).</summary>
    public static void Confetti(int count)
    {
        if (instance == null) return;
        Rect r = instance.layer.rect;
        for (int i = 0; i < count; i++)
        {
            var p = new Particle
            {
                image = instance.Take(i % 4 == 0 ? Art.Star : Art.Petal),
                pos = new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + Random.Range(20f, 400f)),
                vel = new Vector2(Random.Range(-120f, 120f), Random.Range(-500f, -200f)),
                maxLife = Random.Range(2.2f, 3.4f),
                spin = Random.Range(-300f, 300f),
                gravity = -180f,
                size0 = Random.Range(26f, 46f),
                drag = 0.2f,
                color = FlowerColors[Random.Range(0, FlowerColors.Length)],
            };
            p.size1 = p.size0;
            instance.live.Add(p);
        }
    }

    /// <summary>퍼져 나가는 고리 (폭탄 충격파).</summary>
    public static void Ring(Vector3 world, Color color, float maxSize, float duration)
    {
        if (instance == null) return;
        var img = Ui.Image(instance.layer, Art.Ring, color, "Ring");
        img.rectTransform.anchoredPosition = Local(world);
        Anim.Run(img, duration, t =>
        {
            float e = Anim.OutCubic(t);
            img.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(40f, maxSize, e);
            img.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
        }, () => Destroy(img.gameObject));
    }

    /// <summary>번쩍이는 빛.</summary>
    public static void Flash(Vector3 world, float size, Color color, float duration = 0.35f)
    {
        if (instance == null) return;
        var img = Ui.Image(instance.layer, Art.Glow, color, "Flash");
        img.rectTransform.anchoredPosition = Local(world);
        Anim.Run(img, duration, t =>
        {
            img.rectTransform.sizeDelta = Vector2.one * size * Mathf.Lerp(0.5f, 1.3f, Anim.OutCubic(t));
            img.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
        }, () => Destroy(img.gameObject));
    }

    /// <summary>위로 떠오르며 사라지는 글자 ("짝!", "+30초").</summary>
    public static void PopText(Vector3 world, string text, int size, Color color, Color outline, float rise = 140f, float duration = 0.9f)
    {
        if (instance == null) return;
        var t = Ui.Text(instance.layer, text, size, color, TextAnchor.MiddleCenter, "PopText");
        Ui.Outline(t, outline, 4f);
        Ui.Shadow(t, new Color(0f, 0f, 0f, 0.4f), 6f);
        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(900f, size * 1.6f);
        Vector2 start = Local(world);
        rt.anchoredPosition = start;
        Anim.Run(t, duration, k =>
        {
            float pop = k < 0.2f ? Anim.OutBack(k / 0.2f) : 1f;
            rt.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, pop);
            rt.anchoredPosition = start + Vector2.up * rise * Anim.OutCubic(k);
            float a = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
            t.color = new Color(color.r, color.g, color.b, a);
        }, () => Destroy(t.gameObject));
    }

    /// <summary>
    /// 화면 가운데에 도장처럼 쾅 찍히는 큰 글자 ("쌌다!", "폭탄!"). 크게 시작해서 내려찍고, 잠시 머문 뒤 사라진다.
    /// </summary>
    public static void Stamp(string text, string sub, Color fill, Color edge, float tilt = -8f)
    {
        if (instance == null) return;
        var root = Ui.Rect("Stamp", instance.layer);
        Ui.Place(root, 0.5f, 0.56f, new Vector2(900f, 360f));
        var glow = Ui.Image(root, Art.Glow, new Color(fill.r, fill.g, fill.b, 0.6f), "Glow");
        Ui.Place(glow.rectTransform, 0.5f, 0.5f, new Vector2(900f, 520f));
        var t = Ui.Text(root, text, 190, fill, TextAnchor.MiddleCenter, "Big");
        Ui.Stretch(t.rectTransform);
        Ui.Outline(t, edge, 7f);
        Ui.Shadow(t, new Color(0f, 0f, 0f, 0.5f), 12f);
        Text s = null;
        if (!string.IsNullOrEmpty(sub))
        {
            s = Ui.Text(root, sub, 58, Color.white, TextAnchor.MiddleCenter, "Sub");
            Ui.Place(s.rectTransform, 0.5f, 0f, new Vector2(900f, 80f), new Vector2(0f, -10f));
            Ui.Outline(s, edge, 4f);
        }
        root.localEulerAngles = new Vector3(0f, 0f, tilt);
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        Anim.Run(root, 1.25f, k =>
        {
            if (k < 0.18f)
            {
                float e = Anim.InCubic(k / 0.18f);
                root.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, e);
                group.alpha = Mathf.Lerp(0.2f, 1f, e);
            }
            else if (k < 0.3f)
            {
                root.localScale = Vector3.one * (1f + 0.08f * Anim.Punch((k - 0.18f) / 0.12f));
            }
            else
            {
                root.localScale = Vector3.one;
                group.alpha = k < 0.75f ? 1f : 1f - (k - 0.75f) / 0.25f;
            }
            glow.rectTransform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(k * 20f));
        }, () => Destroy(root.gameObject));
    }

    /// <summary>대상(보통 판)을 잠깐 흔든다.</summary>
    public static void Shake(RectTransform target, float strength, float duration)
    {
        if (target == null) return;
        Vector2 home = target.anchoredPosition;
        var shaker = target.GetComponent<ShakeHome>();
        if (shaker == null) { shaker = target.gameObject.AddComponent<ShakeHome>(); shaker.home = home; }
        else home = shaker.home;
        Anim.Run(target, duration, t =>
        {
            float s = strength * (1f - t);
            target.anchoredPosition = home + new Vector2(Random.Range(-s, s), Random.Range(-s, s));
        }, () => target.anchoredPosition = home);
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.life += dt;
            if (p.life >= p.maxLife || p.image == null)
            {
                if (p.image != null)
                {
                    p.image.gameObject.SetActive(false);
                    pool.Push(p.image);
                }
                live.RemoveAt(i);
                continue;
            }
            p.vel.y += p.gravity * dt;
            p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
            p.pos += p.vel * dt;
            float k = p.life / p.maxLife;
            var rt = p.image.rectTransform;
            rt.anchoredPosition = p.pos;
            float size = Mathf.Lerp(p.size0, p.size1, k);
            rt.sizeDelta = new Vector2(size, size);
            rt.Rotate(0f, 0f, p.spin * dt);
            var c = p.color;
            c.a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            p.image.color = c;
            live[i] = p;
        }
    }
}

/// <summary>흔들기가 겹쳐도 원래 자리로 돌아오게 기억해 둔다.</summary>
public class ShakeHome : MonoBehaviour
{
    public Vector2 home;
}
