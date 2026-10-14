using System;
using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 판 위의 화투패 한 장. 그림자 + 패 그림 + (켜져 있으면) 월 숫자 + 선택/힌트 빛 + 폭탄 표시.
/// 누르는 순간(손을 떼기 전) 반응해야 손맛이 좋아서 PointerDown으로 받는다.
/// </summary>
public class CardView : MonoBehaviour, IPointerDownHandler
{
    public int Value { get; private set; }
    public Cell Cell;
    public bool IsBlock => Value == Board.Block;
    public RectTransform Rt { get; private set; }

    public Action<CardView> Pressed;

    Image face, shadow, glow, hintGlow, bombGlow, bombIcon;
    RectTransform lift;
    Text monthText;
    GameObject monthBadge;
    bool selected, hinted, bomb;
    float pulse;

    public static CardView Create(Transform parent, int value, Vector2 size)
    {
        var rt = Ui.Rect("Card", parent);
        rt.sizeDelta = size;
        var v = rt.gameObject.AddComponent<CardView>();
        v.Rt = rt;
        v.Build(size);
        v.SetValue(value);
        return v;
    }

    void Build(Vector2 size)
    {
        // 누르는 영역은 패보다 살짝 넓게 (칸 사이 틈을 눌러도 잡히게)
        var hit = gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0);

        glow = Ui.Image(Rt, Art.Glow, new Color(1f, 0.85f, 0.3f, 0f), "Glow");
        Ui.Place(glow.rectTransform, 0.5f, 0.5f, size * 1.6f);
        hintGlow = Ui.Image(Rt, Art.Glow, new Color(0.4f, 0.9f, 1f, 0f), "HintGlow");
        Ui.Place(hintGlow.rectTransform, 0.5f, 0.5f, size * 1.7f);
        bombGlow = Ui.Image(Rt, Art.Glow, new Color(1f, 0.3f, 0.1f, 0f), "BombGlow");
        Ui.Place(bombGlow.rectTransform, 0.5f, 0.5f, size * 1.7f);

        lift = Ui.Rect("Lift", Rt);
        Ui.Stretch(lift);
        shadow = Ui.Image(lift, Art.RoundSmall, new Color(0f, 0f, 0f, 0.35f), "Shadow");
        Ui.Stretch(shadow.rectTransform);
        shadow.rectTransform.offsetMin = new Vector2(size.x * 0.03f, -size.y * 0.035f);
        shadow.rectTransform.offsetMax = new Vector2(size.x * 0.03f, -size.y * 0.02f);
        face = Ui.Image(lift, null, Color.white, "Face");
        Ui.Stretch(face.rectTransform);
        face.preserveAspect = false;

        monthBadge = Ui.Rect("Month", lift).gameObject;
        var badgeRt = (RectTransform)monthBadge.transform;
        float b = Mathf.Max(30f, size.x * 0.4f);
        badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0f, 1f);
        badgeRt.pivot = new Vector2(0f, 1f);
        badgeRt.sizeDelta = new Vector2(b, b);
        badgeRt.anchoredPosition = new Vector2(-b * 0.18f, b * 0.18f);
        var badgeBg = Ui.Image(badgeRt, Art.Circle128, Art.Ink, "Bg");
        Ui.Stretch(badgeBg.rectTransform);
        var badgeRing = Ui.Image(badgeRt, Art.Ring, Art.Gold, "Ring");
        Ui.Stretch(badgeRing.rectTransform);
        monthText = Ui.Text(badgeRt, "", Mathf.RoundToInt(b * 0.58f), Color.white, TextAnchor.MiddleCenter, "N");
        Ui.Stretch(monthText.rectTransform);
        monthText.rectTransform.offsetMin = new Vector2(0f, b * 0.04f);

        bombIcon = Ui.Image(lift, Art.IconBomb, Color.white, "Bomb");
        float bi = Mathf.Max(34f, size.x * 0.5f);
        bombIcon.rectTransform.anchorMin = bombIcon.rectTransform.anchorMax = new Vector2(1f, 1f);
        bombIcon.rectTransform.sizeDelta = new Vector2(bi, bi);
        bombIcon.rectTransform.anchoredPosition = new Vector2(-bi * 0.2f, -bi * 0.25f);
        bombIcon.gameObject.SetActive(false);
    }

    public void SetValue(int value)
    {
        Value = value;
        face.sprite = Art.Card(value == Board.Block ? Art.BackIndex : value);
        if (value == Board.Block)
        {
            face.color = new Color(0.78f, 0.72f, 0.72f);
            monthBadge.SetActive(false);
            GetComponent<Image>().raycastTarget = false;
        }
        else
        {
            face.color = Color.white;
            monthText.text = (Board.Month(value) + 1).ToString();
            RefreshMonthBadge();
        }
    }

    public void RefreshMonthBadge()
    {
        if (!IsBlock) monthBadge.SetActive(Settings.ShowMonth);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!IsBlock) Pressed?.Invoke(this);
    }

    public bool Selected
    {
        get => selected;
        set
        {
            if (selected == value) return;
            selected = value;
            Anim.Run(lift, 0.12f, t =>
            {
                float k = selected ? Anim.OutBack(t) : 1f - Anim.OutCubic(t);
                lift.localScale = Vector3.one * (1f + 0.1f * k);
                lift.anchoredPosition = new Vector2(0f, Rt.sizeDelta.y * 0.06f * k);
                shadow.color = new Color(0f, 0f, 0f, 0.35f - 0.12f * k);
            });
        }
    }

    public bool Hinted
    {
        get => hinted;
        set => hinted = value;
    }

    public bool Bomb
    {
        get => bomb;
        set
        {
            if (bomb == value) return;
            bomb = value;
            bombIcon.gameObject.SetActive(value);
            if (value) Anim.Pop(bombIcon.transform, 0.5f, 0.35f);
        }
    }

    /// <summary>짝이 아닐 때 고개를 젓듯 좌우로 흔든다.</summary>
    public void Wiggle()
    {
        Anim.Run(lift, 0.3f, t =>
        {
            lift.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI * 5f) * 7f * (1f - t));
        });
    }

    /// <summary>뒤집듯 가로로 접었다 펴면서 그림을 바꾼다 (섞기).</summary>
    public void FlipTo(int value, float delay)
    {
        Anim.Run(lift, 0.3f, t =>
        {
            float sx = t < 0.5f ? 1f - Anim.InCubic(t * 2f) : Anim.OutCubic((t - 0.5f) * 2f);
            lift.localScale = new Vector3(Mathf.Max(0.02f, sx), 1f, 1f);
            if (t >= 0.5f && Value != value) SetValue(value);
        }, null, delay);
    }

    void Update()
    {
        pulse += Time.unscaledDeltaTime;
        float wave = 0.5f + 0.5f * Mathf.Sin(pulse * 6f);
        glow.color = new Color(1f, 0.85f, 0.3f, Mathf.MoveTowards(glow.color.a, selected ? 0.75f + 0.25f * wave : 0f, Time.unscaledDeltaTime * 6f));
        hintGlow.color = new Color(0.4f, 0.95f, 1f, hinted ? 0.45f + 0.5f * wave : Mathf.MoveTowards(hintGlow.color.a, 0f, Time.unscaledDeltaTime * 4f));
        bombGlow.color = new Color(1f, 0.35f, 0.1f, bomb ? 0.35f + 0.35f * wave : 0f);
        if (bomb) bombIcon.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(pulse * 9f) * 10f);
    }
}
