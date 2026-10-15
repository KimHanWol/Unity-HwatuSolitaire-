using UnityEngine;
using UnityEngine.UI;

/// <summary>타이틀: 부채처럼 펼친 광 다섯 장, "짝!" 도장 + 제목, 시작·설정 버튼.</summary>
public class TitleScreen : MonoBehaviour, IBackHandler
{
    static readonly int[] FanCards = { 0, 8, 28, 40, 44 }; // 송학·벚꽃·공산·오동·비 광

    RectTransform fan;
    RectTransform[] fanCards;
    RectTransform badge;
    float time;

    void Start() => Build();

    void Build()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        var rt = (RectTransform)transform;

        // 광 다섯 장 부채
        fan = Ui.Rect("Fan", rt);
        Ui.Place(fan, 0.5f, 0.72f, new Vector2(10f, 10f));
        fanCards = new RectTransform[FanCards.Length];
        for (int i = 0; i < FanCards.Length; i++)
        {
            var pivot = Ui.Rect("Pivot" + i, fan);
            Ui.Place(pivot, 0.5f, 0.5f, Vector2.zero, new Vector2(0f, -260f));
            float angle = (2 - i) * 13f;
            pivot.localEulerAngles = new Vector3(0f, 0f, angle);
            var shadow = Ui.Image(pivot, Art.RoundSmall, new Color(0f, 0f, 0f, 0.35f), "Shadow");
            Ui.Place(shadow.rectTransform, 0.5f, 0.5f, new Vector2(230f, 374f), new Vector2(10f, 250f));
            var card = Ui.Image(pivot, Art.Card(FanCards[i]), Color.white, "Card");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, new Vector2(230f, 374f), new Vector2(0f, 262f));
            fanCards[i] = pivot;
            // 한 장씩 펼쳐진다.
            pivot.localEulerAngles = Vector3.zero;
            float target = angle;
            Anim.Run(pivot, 0.5f, t => pivot.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(0f, target, Anim.OutBack(t))), null, 0.1f + i * 0.06f);
        }
        var glow = Ui.Image(rt, Art.Glow, new Color(1f, 0.9f, 0.5f, 0.25f), "FanGlow");
        Ui.Place(glow.rectTransform, 0.5f, 0.72f, new Vector2(1100f, 900f));
        glow.transform.SetSiblingIndex(0);

        // 제목
        // 한글 이름은 한국에서 마작 패 맞추기를 두루 이르는 "사천성"으로 (영어권은 Solitaire 가 익숙하다).
        string main = Loc.T("화투 사천성", "Hwatu Solitaire");
        var title = Ui.Text(rt, main, Loc.Korean ? 140 : 116, Art.Gold, TextAnchor.MiddleCenter, "Title");
        Ui.Place(title.rectTransform, 0.5f, 0.47f, new Vector2(1060f, 190f));
        Ui.Outline(title, Art.WoodDark, 7f);
        Ui.Shadow(title, new Color(0f, 0f, 0f, 0.5f), 12f);
        var shine = title.gameObject.AddComponent<Gradient2>();
        shine.top = new Color(1f, 0.97f, 0.75f);
        shine.bottom = Art.Gold;

        badge = Ui.Rect("Badge", rt);
        Ui.Place(badge, 0.5f, 0.47f, new Vector2(190f, 190f), new Vector2(Loc.Korean ? -380f : -420f, 120f));
        var badgeBg = Ui.Image(badge, Art.Circle128, Art.Red, "Bg");
        Ui.Stretch(badgeBg.rectTransform);
        var badgeRing = Ui.Image(badge, Art.Ring, Art.Gold, "Ring");
        Ui.Stretch(badgeRing.rectTransform, 8f);
        var badgeText = Ui.Text(badge, Loc.T("짝!", "SNAP!"), Loc.Korean ? 86 : 54, Color.white, TextAnchor.MiddleCenter, "Text");
        Ui.Stretch(badgeText.rectTransform);
        Ui.Shadow(badgeText, new Color(0f, 0f, 0f, 0.4f), 4f);
        badge.localEulerAngles = new Vector3(0f, 0f, 12f);
        badge.localScale = Vector3.zero;
        Anim.Run(badge, 0.4f, t => badge.localScale = Vector3.one * Anim.OutBack(t), () =>
        {
            Sfx.Snap(1);
            Fx.Burst(badge.position, 16, Fx.FlowerColors, 900f);
        }, 0.55f);

        var sub = Ui.Text(rt, App.Subtitle, 46, Color.white, TextAnchor.MiddleCenter, "Sub");
        Ui.Place(sub.rectTransform, 0.5f, 0.47f, new Vector2(1000f, 70f), new Vector2(0f, -120f));
        Ui.Outline(sub, Art.WoodDark, 3f);

        // 모은 별
        var pill = Ui.Image(rt, Art.Round, new Color(0f, 0f, 0f, 0.35f), "Stars");
        Ui.Place(pill.rectTransform, 0.5f, 0.47f, new Vector2(420f, 90f), new Vector2(0f, -220f));
        var starIcon = Ui.Image(pill.rectTransform, Art.Gwang, Color.white, "Icon");
        Ui.Place(starIcon.rectTransform, 0f, 0.5f, new Vector2(70f, 70f), new Vector2(60f, 0f));
        var starText = Ui.Text(pill.rectTransform, Progress.TotalStars + " / " + (App.I.Stages.stages.Length * 3), 48, Color.white, TextAnchor.MiddleCenter, "Text");
        Ui.Stretch(starText.rectTransform);
        starText.rectTransform.offsetMin = new Vector2(70f, 0f);

        // 버튼
        int frontier = Progress.Frontier;
        string playLabel = Progress.TotalStars == 0 ? Loc.T("시작하기", "Play") : Loc.T("이어하기  ", "Continue  ") + frontier;
        var play = Ui.Button(rt, new Vector2(640f, 170f), Art.Green, Art.GreenDark, playLabel, Art.IconPlay, () => App.I.Play(frontier), 66, "Play");
        Ui.Place((RectTransform)play.transform, 0.5f, 0.2f, new Vector2(640f, 170f), new Vector2(0f, 40f));
        Anim.Run(play, 100000f, t =>
        {
            if (play) play.transform.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 4f));
        });
        // 아래 줄(스테이지 + 설정)의 양 끝을 위 이어하기 버튼(폭 640)의 양 끝에 맞춘다: 490 + 간격 20 + 130 = 640
        var stages = Ui.Button(rt, new Vector2(490f, 130f), Art.Red, Art.RedDark, Loc.T("스테이지", "Stages"), Art.IconGrid, () => App.I.ShowStages(frontier), 50, "Stages");
        Ui.Place((RectTransform)stages.transform, 0.5f, 0.2f, new Vector2(490f, 130f), new Vector2(-75f, -150f));
        var settings = Ui.IconButton(rt, 130f, Art.Wood, Art.WoodDark, Art.IconGear, () => Tips.ShowSettings(Build), "Settings");
        Ui.Place((RectTransform)settings.transform, 0.5f, 0.2f, new Vector2(130f, 130f), new Vector2(255f, -150f));

        // 언어 빠른 전환
        var lang = Ui.Button(rt, new Vector2(150f, 90f), Art.Wood, Art.WoodDark, Loc.Korean ? "EN" : "한글", null, () =>
        {
            Loc.Toggle();
            Build();
        }, 40, "Lang");
        Ui.Place((RectTransform)lang.transform, 1f, 1f, new Vector2(150f, 90f), new Vector2(-100f, -70f));

        var credit = Ui.Text(rt, Loc.T("화투 그림: Wikimedia Commons (CC BY-SA 4.0)", "Card art: Wikimedia Commons (CC BY-SA 4.0)"), 26, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, "Credit");
        Ui.Place(credit.rectTransform, 0.5f, 0f, new Vector2(1000f, 40f), new Vector2(0f, 36f));
    }

    void Update()
    {
        time += Time.unscaledDeltaTime;
        if (fan != null) fan.anchoredPosition = new Vector2(0f, Mathf.Sin(time * 1.4f) * 10f);
        if (badge != null && badge.localScale.x > 0.99f) badge.localEulerAngles = new Vector3(0f, 0f, 12f + Mathf.Sin(time * 2.2f) * 4f);
    }

    public void OnBack() => Application.Quit();
}

/// <summary>글자에 위→아래 그라데이션 (금박 제목).</summary>
public class Gradient2 : BaseMeshEffect
{
    public Color top = Color.white, bottom = Color.white;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;
        var v = new UIVertex();
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            min = Mathf.Min(min, v.position.y);
            max = Mathf.Max(max, v.position.y);
        }
        float h = Mathf.Max(1f, max - min);
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            // 테두리(Outline)가 만든 진한 정점은 그대로 둔다.
            Color old = v.color;
            if (old.r < 0.5f && old.g < 0.4f) continue;
            Color c = Color.Lerp(bottom, top, (v.position.y - min) / h);
            v.color = c;
            vh.SetUIVertex(v, i);
        }
    }
}
