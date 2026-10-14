using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 목록. 12장(1월 송학 ~ 12월 비) × 25칸. 장마다 그 달의 광(또는 대표 패)과 색 띠를 머리에 단다.
/// 깬 스테이지는 별, 다음에 할 스테이지는 금색으로 깜빡이고, 아직 못 여는 곳은 자물쇠.
/// </summary>
public class StageSelectScreen : MonoBehaviour, IBackHandler
{
    const float TopBar = 160f;
    const int Columns = 5;
    const float Tile = 176f, Gap = 22f;
    const float HeaderH = 210f, ChapterGap = 50f;

    RectTransform content;
    ScrollRect scroll;
    RectTransform frontierTile;
    float time;

    public void Build(int focus)
    {
        var rt = (RectTransform)transform;

        var top = Ui.Rect("Top", rt);
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = Vector2.one;
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(0f, TopBar);
        var topBg = Ui.Image(top, null, new Color(0f, 0f, 0f, 0.3f), "Bg");
        Ui.Stretch(topBg.rectTransform);
        var back = Ui.IconButton(top, 118f, Art.Wood, Art.WoodDark, Art.IconBack, App.I.ShowTitle, "Back");
        Ui.Place((RectTransform)back.transform, 0f, 0.5f, new Vector2(118f, 118f), new Vector2(90f, 0f));
        var title = Ui.Text(top, Loc.T("스테이지", "Stages"), 68, Color.white, TextAnchor.MiddleCenter, "Title");
        Ui.Place(title.rectTransform, 0.5f, 0.5f, new Vector2(500f, 100f));
        Ui.Outline(title, Art.WoodDark, 4f);
        var starIcon = Ui.Image(top, Art.Star, Art.Gold, "Star");
        Ui.Place(starIcon.rectTransform, 1f, 0.5f, new Vector2(60f, 60f), new Vector2(-250f, 0f));
        var starText = Ui.Text(top, Progress.TotalStars + "/" + (App.I.Stages.stages.Length * 3), 46, Color.white, TextAnchor.MiddleLeft, "Stars");
        Ui.Place(starText.rectTransform, 1f, 0.5f, new Vector2(200f, 80f), new Vector2(-110f, 0f));

        // 스크롤 목록
        var view = Ui.Rect("Scroll", rt);
        Ui.Stretch(view);
        view.offsetMax = new Vector2(0f, -TopBar);
        var mask = view.gameObject.AddComponent<RectMask2D>();
        var hit = view.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0);
        scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 40f;
        scroll.decelerationRate = 0.12f;
        content = Ui.Rect("Content", view);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        scroll.content = content;
        scroll.viewport = view;

        float gridW = Columns * Tile + (Columns - 1) * Gap;
        float y = 30f;
        float focusY = 0f;
        int frontier = Progress.Frontier;
        int stageCount = App.I.Stages.stages.Length;
        int chapters = Mathf.CeilToInt(stageCount / (float)StageGenerator.StagesPerChapter);
        for (int ch = 0; ch < chapters; ch++)
        {
            int first = ch * StageGenerator.StagesPerChapter + 1;
            bool chapterOpen = Progress.Unlocked(first);
            BuildHeader(ch, y, gridW, chapterOpen);
            y += HeaderH + 20f;
            for (int k = 0; k < StageGenerator.StagesPerChapter; k++)
            {
                int stage = first + k;
                if (stage > stageCount) break;
                int col = k % Columns, row = k / Columns;
                float x = -gridW / 2f + Tile / 2f + col * (Tile + Gap);
                float ty = y + row * (Tile + Gap) + Tile / 2f;
                var tile = BuildTile(stage, frontier);
                Ui.Place(tile, 0.5f, 1f, new Vector2(Tile, Tile), new Vector2(x, -ty));
                if (stage == focus) focusY = ty;
            }
            y += 5 * (Tile + Gap) + ChapterGap;
        }
        content.sizeDelta = new Vector2(0f, y + 40f);

        // 고른 스테이지가 화면 가운데쯤 오게
        Canvas.ForceUpdateCanvases();
        float viewH = view.rect.height;
        float maxScroll = Mathf.Max(0f, content.sizeDelta.y - viewH);
        float target = Mathf.Clamp(focusY - viewH * 0.45f, 0f, maxScroll);
        content.anchoredPosition = new Vector2(0f, target);
    }

    void BuildHeader(int chapter, float y, float width, bool open)
    {
        var head = Ui.Rect("Chapter" + (chapter + 1), content);
        Ui.Place(head, 0.5f, 1f, new Vector2(width + 20f, HeaderH), new Vector2(0f, -(y + HeaderH / 2f)));
        Color c = Art.MonthColor(chapter);
        var lip = Ui.Image(head, Art.Round, Color.Lerp(c, Color.black, 0.4f), "Lip");
        Ui.Stretch(lip.rectTransform);
        var band = Ui.Image(head, Art.Round, open ? c : Color.Lerp(c, Art.Grey, 0.7f), "Band");
        Ui.Stretch(band.rectTransform);
        band.rectTransform.offsetMin = new Vector2(0f, 10f);
        var edge = Ui.Image(band.rectTransform, Art.RoundOutline, new Color(Art.Gold.r, Art.Gold.g, Art.Gold.b, 0.8f), "Edge");
        Ui.Stretch(edge.rectTransform, 6f);

        // 그 달 대표 패 (살짝 기울여 머리띠 위로 삐져나오게)
        var card = Ui.Image(band.rectTransform, Art.Card(Art.MonthHero(chapter)), open ? Color.white : new Color(0.6f, 0.6f, 0.6f), "Card");
        Ui.Place(card.rectTransform, 0f, 0.5f, new Vector2(130f, 130f * Art.CardAspect), new Vector2(110f, 18f));
        card.rectTransform.localEulerAngles = new Vector3(0f, 0f, 8f);

        var name = Ui.Text(band.rectTransform, Loc.T((chapter + 1) + "월 · " + Art.MonthName(chapter), "Chapter " + (chapter + 1) + " · " + Art.MonthName(chapter)), 62, Color.white, TextAnchor.MiddleLeft, "Name");
        Ui.Place(name.rectTransform, 0f, 0.5f, new Vector2(600f, 90f), new Vector2(230f + 300f, 22f));
        Ui.Outline(name, Color.Lerp(c, Color.black, 0.55f), 3f);
        int got = Progress.ChapterStars(chapter);
        var stars = Ui.Text(band.rectTransform, "★ " + got + " / " + StageGenerator.StagesPerChapter * 3, 42, Art.Gold, TextAnchor.MiddleLeft, "Stars");
        Ui.Place(stars.rectTransform, 0f, 0.5f, new Vector2(600f, 60f), new Vector2(230f + 300f, -45f));
        Ui.Outline(stars, Color.Lerp(c, Color.black, 0.55f), 2f);
        if (!open)
        {
            var lockIcon = Ui.Image(band.rectTransform, Art.IconLock, new Color(1f, 1f, 1f, 0.8f), "Lock");
            Ui.Place(lockIcon.rectTransform, 1f, 0.5f, new Vector2(80f, 80f), new Vector2(-80f, 0f));
        }
    }

    RectTransform BuildTile(int stage, int frontier)
    {
        bool open = Progress.Unlocked(stage);
        int stars = Progress.Stars(stage);
        bool current = stage == frontier && stars == 0;

        var root = Ui.Rect("Stage" + stage, content);
        var lip = Ui.Image(root, Art.Round, open ? Art.PaperDark : new Color(0f, 0f, 0f, 0.3f), "Lip");
        Ui.Stretch(lip.rectTransform);
        var face = Ui.Image(root, Art.Round, open ? Art.Paper : new Color(1f, 1f, 1f, 0.12f), "Face");
        Ui.Stretch(face.rectTransform);
        face.rectTransform.offsetMin = new Vector2(0f, 8f);
        face.raycastTarget = true;

        if (open)
        {
            var num = Ui.Text(face.rectTransform, stage.ToString(), stage >= 100 ? 58 : 66, current ? Art.Red : Art.Ink, TextAnchor.MiddleCenter, "Num");
            Ui.Stretch(num.rectTransform);
            num.rectTransform.offsetMin = new Vector2(0f, 30f);
            for (int i = 0; i < 3; i++)
            {
                var s = Ui.Image(face.rectTransform, Art.Star, i < stars ? Art.Gold : new Color(0f, 0f, 0f, 0.15f), "Star" + i);
                Ui.Place(s.rectTransform, 0.5f, 0f, new Vector2(i == 1 ? 46f : 40f, i == 1 ? 46f : 40f), new Vector2((i - 1) * 46f, i == 1 ? 38f : 34f));
            }
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                Sfx.Click();
                App.I.Play(stage);
            });
            var press = root.gameObject.AddComponent<PressFeedback>();
            press.face = face.rectTransform;
            press.lipHeight = 8f;
        }
        else
        {
            var lockIcon = Ui.Image(face.rectTransform, Art.IconLock, new Color(1f, 1f, 1f, 0.45f), "Lock");
            Ui.Place(lockIcon.rectTransform, 0.5f, 0.5f, new Vector2(70f, 70f), new Vector2(0f, 10f));
            var num = Ui.Text(face.rectTransform, stage.ToString(), 34, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleCenter, "Num");
            Ui.Place(num.rectTransform, 0.5f, 0f, new Vector2(Tile, 50f), new Vector2(0f, 26f));
        }

        if (current)
        {
            var ring = Ui.Image(root, Art.RoundOutline, Art.Gold, "Current");
            Ui.Stretch(ring.rectTransform, -10f);
            ring.transform.SetAsFirstSibling();
            frontierTile = ring.rectTransform;
        }
        return root;
    }

    void Update()
    {
        time += Time.unscaledDeltaTime;
        if (frontierTile != null)
        {
            float k = 0.5f + 0.5f * Mathf.Sin(time * 5f);
            frontierTile.GetComponent<Image>().color = new Color(Art.Gold.r, Art.Gold.g, Art.Gold.b, 0.5f + 0.5f * k);
            frontierTile.localScale = Vector3.one * (1f + 0.03f * k);
        }
    }

    public void OnBack() => App.I.ShowTitle();
}
