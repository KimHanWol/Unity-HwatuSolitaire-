using System.Collections.Generic;
using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 화면. 위: 일시정지·스테이지 이름·남은 시간과 별 기준. 가운데: 판. 아래: 쌌다(폭탄) 칸·콤보·힌트.
///
/// 규칙: 같은 달 화투 두 장을 꺾임 2번 이하의 선(빈칸·판 바깥만 지남)으로 이으면 "짝!" 하고 사라진다.
/// 콤보: 짝을 맞춘 뒤 comboWindow초 안에 또 맞추면 연속. 3연속마다 "쌌다!" → 마지막에 맞춘 달이 폭탄이 되어 아래 칸에 모인다.
/// 폭탄이 걸린 달의 패를 아무거나 하나 누르면 그 달 패가 판에서 한꺼번에 펑 터진다 (선 연결 필요 없음).
/// 시간 안에 다 지우면 클리어(걸린 시간으로 별), 시간이 다 되면 실패 → 광고 보고 시간 더 받아 이어하기.
/// 둘 곳이 없으면(폭탄도 없으면) 자동으로 섞는다.
/// </summary>
public class GameScreen : MonoBehaviour, IBackHandler
{
    const int MaxBombs = 3;
    const int MaxRevives = 2;
    const float TopBar = 150f, TimerRow = 96f, BottomBar = 230f;

    StageDef def;
    Board board;
    GravityMode gravity;
    Rng rng;

    CardView[,] views;
    readonly List<CardView> blockViews = new List<CardView>();
    CardView selected;
    CardView[] hint;

    RectTransform boardArea, shakeRoot, boardRoot, pathLayer;
    float cellW, cellH;

    float elapsed, bonusTime;
    bool running, finished;
    int revives;
    int combo;
    float lastMatchAt = -100f;
    int lastTickSecond = -1;
    readonly List<int> bombs = new List<int>();
    int matches;

    // HUD
    Text stageText, chapterText, timeText, comboText;
    Image timeFill, comboRing;
    Image[] hudStars;
    RectTransform[] starMarks;
    RectTransform timerBar;
    RectTransform[] traySlots;
    Image[] trayCards;
    Text hintCount;
    Image hintAd;
    RectTransform comboRoot;
    RectTransform hintButton;
    Text toast;

    bool Paused => !running || App.I.PopupOpen || Ads.Showing;
    float Limit => def.timeLimit + bonusTime;

    public void Begin(StageDef stage)
    {
        def = stage;
        board = def.CreateBoard();
        gravity = def.Gravity;
        rng = new Rng(def.number * 31 + System.Environment.TickCount);
        BuildHud();
        BuildBoard();
        App.I.LayoutChanged += Relayout;
        running = true;
        RefreshHint();
        RefreshTray();
        RefreshTimer();
        ShowTipIfNeeded();
    }

    void OnDestroy()
    {
        if (App.I != null) App.I.LayoutChanged -= Relayout;
    }

    // ------------------------------------------------------------ 화면 만들기
    void BuildHud()
    {
        var rt = (RectTransform)transform;

        // 위쪽 나무 띠
        var top = Ui.Rect("Top", rt);
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(0f, TopBar);
        var pause = Ui.IconButton(top, 118f, Art.Wood, Art.WoodDark, Art.IconPause, OpenPause, "Pause");
        Ui.Place((RectTransform)pause.transform, 0f, 0.5f, new Vector2(118f, 118f), new Vector2(90f, -4f));

        int chapter = StageGenerator.ChapterOf(def.number);
        int inChapter = (def.number - 1) % StageGenerator.StagesPerChapter + 1;
        stageText = Ui.Text(top, Loc.T("스테이지 ", "Stage ") + def.number, 62, Color.white, TextAnchor.MiddleCenter, "Stage");
        Ui.Place(stageText.rectTransform, 0.5f, 0.5f, new Vector2(600f, 80f), new Vector2(0f, 18f));
        Ui.Outline(stageText, Art.WoodDark, 3f);
        chapterText = Ui.Text(top, Loc.T((chapter + 1) + "월 · ", "") + Art.MonthName(chapter) + "  " + (chapter + 1) + "-" + inChapter, 38, Art.Gold, TextAnchor.MiddleCenter, "Chapter");
        Ui.Place(chapterText.rectTransform, 0.5f, 0.5f, new Vector2(600f, 50f), new Vector2(0f, -40f));
        Ui.Outline(chapterText, Art.WoodDark, 2f);

        // 중력 방향 표시
        if (gravity != GravityMode.None)
        {
            var g = Ui.Rect("Gravity", top);
            Ui.Place(g, 0f, 0.5f, new Vector2(96f, 96f), new Vector2(215f, -4f));
            var gbg = Ui.Image(g, Art.Circle128, new Color(0f, 0f, 0f, 0.35f), "Bg");
            Ui.Stretch(gbg.rectTransform);
            void Arrow(float angle, Vector2 offset, float size)
            {
                var a = Ui.Image(g, Art.IconBack, Art.Gold, "Arrow");
                Ui.Place(a.rectTransform, 0.5f, 0.5f, new Vector2(size, size), offset);
                a.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            }
            switch (gravity)
            {
                case GravityMode.Left: Arrow(0f, Vector2.zero, 64f); break;
                case GravityMode.Right: Arrow(180f, Vector2.zero, 64f); break;
                case GravityMode.Down: Arrow(90f, Vector2.zero, 64f); break;
                case GravityMode.Up: Arrow(-90f, Vector2.zero, 64f); break;
                case GravityMode.SplitOut: Arrow(0f, new Vector2(-18f, 0f), 44f); Arrow(180f, new Vector2(18f, 0f), 44f); break;
                case GravityMode.SplitIn: Arrow(180f, new Vector2(-18f, 0f), 44f); Arrow(0f, new Vector2(18f, 0f), 44f); break;
                case GravityMode.SplitVertical: Arrow(-90f, new Vector2(0f, 18f), 44f); Arrow(90f, new Vector2(0f, -18f), 44f); break;
            }
        }

        // 오른쪽 위: 별 3개 (지금 속도면 몇 개인지)
        hudStars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var s = Ui.Image(top, Art.Star, Art.Gold, "HudStar" + i);
            Ui.Place(s.rectTransform, 1f, 0.5f, new Vector2(64f, 64f), new Vector2(-200f + i * 66f, 0f));
            hudStars[i] = s;
        }

        // 시간 막대
        var timer = Ui.Rect("Timer", rt);
        timer.anchorMin = new Vector2(0f, 1f);
        timer.anchorMax = new Vector2(1f, 1f);
        timer.pivot = new Vector2(0.5f, 1f);
        timer.sizeDelta = new Vector2(0f, TimerRow);
        timer.anchoredPosition = new Vector2(0f, -TopBar);
        var clock = Ui.Image(timer, Art.IconClock, Art.Gold, "Clock");
        Ui.Place(clock.rectTransform, 0f, 0.5f, new Vector2(62f, 62f), new Vector2(76f, 0f));
        timerBar = Ui.Rect("Bar", timer);
        timerBar.anchorMin = new Vector2(0f, 0.5f);
        timerBar.anchorMax = new Vector2(1f, 0.5f);
        timerBar.offsetMin = new Vector2(120f, -22f);
        timerBar.offsetMax = new Vector2(-200f, 22f);
        var barBg = Ui.Image(timerBar, Art.RoundSmall, new Color(0f, 0f, 0f, 0.45f), "Bg");
        Ui.Stretch(barBg.rectTransform, -5f);
        timeFill = Ui.Image(timerBar, Art.RoundSmall, Art.Gold, "Fill");
        Ui.Stretch(timeFill.rectTransform);
        starMarks = new RectTransform[2];
        for (int i = 0; i < 2; i++)
        {
            var m = Ui.Image(timerBar, Art.Star, Color.white, "Mark" + i);
            m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            m.rectTransform.sizeDelta = new Vector2(46f, 46f);
            starMarks[i] = m.rectTransform;
        }
        timeText = Ui.Text(timer, "0:00", 54, Color.white, TextAnchor.MiddleCenter, "Time");
        Ui.Place(timeText.rectTransform, 1f, 0.5f, new Vector2(180f, 80f), new Vector2(-100f, 0f));
        Ui.Outline(timeText, Art.WoodDark, 3f);

        // 아래 띠: 쌌다 칸 / 콤보 / 힌트
        var bottom = Ui.Rect("Bottom", rt);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.sizeDelta = new Vector2(0f, BottomBar);
        var tray = Ui.Rect("Tray", bottom);
        Ui.Place(tray, 0f, 0.5f, new Vector2(470f, 200f), new Vector2(270f, 0f));
        var trayBg = Ui.Image(tray, Art.Round, new Color(0f, 0f, 0f, 0.35f), "Bg");
        Ui.Stretch(trayBg.rectTransform);
        var trayEdge = Ui.Image(tray, Art.RoundOutline, new Color(Art.Gold.r, Art.Gold.g, Art.Gold.b, 0.6f), "Edge");
        Ui.Stretch(trayEdge.rectTransform, 4f);
        var trayLabel = Ui.Text(tray, Loc.T("쌌다! 폭탄", "BOMBS"), 32, Art.Gold, TextAnchor.MiddleCenter, "Label");
        Ui.Place(trayLabel.rectTransform, 0.5f, 1f, new Vector2(400f, 44f), new Vector2(0f, -6f));
        Ui.Outline(trayLabel, Art.WoodDark, 2f);
        traySlots = new RectTransform[MaxBombs];
        trayCards = new Image[MaxBombs];
        for (int i = 0; i < MaxBombs; i++)
        {
            var slot = Ui.Rect("Slot" + i, tray);
            Ui.Place(slot, 0.5f, 0.5f, new Vector2(88f, 143f), new Vector2((i - 1) * 140f, -12f));
            var empty = Ui.Image(slot, Art.RoundSmall, new Color(1f, 1f, 1f, 0.12f), "Empty");
            Ui.Stretch(empty.rectTransform);
            var card = Ui.Image(slot, null, Color.white, "Card");
            Ui.Stretch(card.rectTransform);
            var icon = Ui.Image(card.rectTransform, Art.IconBomb, Color.white, "Bomb");
            Ui.Place(icon.rectTransform, 0.5f, 0.5f, new Vector2(70f, 70f), new Vector2(0f, -10f));
            card.gameObject.SetActive(false);
            traySlots[i] = slot;
            trayCards[i] = card;
            int index = i;
            var btn = slot.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var hitImg = slot.gameObject.AddComponent<Image>();
            hitImg.color = new Color(0, 0, 0, 0);
            btn.onClick.AddListener(() => PeekBomb(index));
        }

        comboRoot = Ui.Rect("Combo", bottom);
        Ui.Place(comboRoot, 0.5f, 0.5f, new Vector2(170f, 170f), new Vector2(140f, 0f));
        var comboBg = Ui.Image(comboRoot, Art.Circle128, new Color(0f, 0f, 0f, 0.35f), "Bg");
        Ui.Stretch(comboBg.rectTransform);
        comboRing = Ui.Image(comboRoot, Art.Ring, Art.Gold, "Ring");
        Ui.Stretch(comboRing.rectTransform);
        comboRing.type = Image.Type.Filled;
        comboRing.fillMethod = Image.FillMethod.Radial360;
        comboRing.fillOrigin = (int)Image.Origin360.Top;
        comboRing.fillClockwise = false;
        comboRing.fillAmount = 0f;
        comboText = Ui.Text(comboRoot, "", 58, Color.white, TextAnchor.MiddleCenter, "Text");
        Ui.Stretch(comboText.rectTransform);
        Ui.Outline(comboText, Art.WoodDark, 3f);
        var comboLabel = Ui.Text(comboRoot, Loc.T("연속", "COMBO"), 28, Art.Gold, TextAnchor.MiddleCenter, "Label");
        Ui.Place(comboLabel.rectTransform, 0.5f, 0f, new Vector2(200f, 40f), new Vector2(0f, 4f));

        var hintBtn = Ui.Button(bottom, new Vector2(220f, 180f), Art.Blue, Art.BlueDark, Loc.T("힌트", "Hint"), Art.IconBulb, OnHint, 46, "Hint");
        hintButton = (RectTransform)hintBtn.transform;
        Ui.Place(hintButton, 1f, 0.5f, new Vector2(220f, 180f), new Vector2(-140f, 0f));
        var badge = Ui.Image(hintButton, Art.Circle128, Art.Red, "Badge");
        Ui.Place(badge.rectTransform, 1f, 1f, new Vector2(78f, 78f), new Vector2(-12f, -6f));
        hintCount = Ui.Text(badge.rectTransform, "0", 44, Color.white, TextAnchor.MiddleCenter, "Count");
        Ui.Stretch(hintCount.rectTransform);
        hintAd = Ui.Image(badge.rectTransform, Art.IconVideo, Color.white, "Ad");
        Ui.Place(hintAd.rectTransform, 0.5f, 0.5f, new Vector2(46f, 46f));

        // 판 자리
        boardArea = Ui.Rect("BoardArea", rt);
        Ui.Stretch(boardArea);
        boardArea.offsetMax = new Vector2(-20f, -(TopBar + TimerRow + 10f));
        boardArea.offsetMin = new Vector2(20f, BottomBar + 10f);
        shakeRoot = Ui.Rect("Shake", boardArea);
        Ui.Stretch(shakeRoot);
        boardRoot = Ui.Rect("Board", shakeRoot);
        Ui.Place(boardRoot, 0.5f, 0.5f, Vector2.zero);
        pathLayer = Ui.Rect("Paths", shakeRoot);
        Ui.Place(pathLayer, 0.5f, 0.5f, Vector2.zero);

        toast = Ui.Text(rt, "", 48, Color.white, TextAnchor.MiddleCenter, "Toast");
        Ui.Place(toast.rectTransform, 0.5f, 0f, new Vector2(1000f, 80f), new Vector2(0f, BottomBar + 50f));
        Ui.Outline(toast, Art.WoodDark, 3f);
        toast.gameObject.SetActive(false);
    }

    void BuildBoard()
    {
        views = new CardView[def.width, def.height];
        ComputeCell();
        for (int y = 0; y < def.height; y++)
            for (int x = 0; x < def.width; x++)
            {
                int v = board[x, y];
                if (v == Board.Empty) continue;
                var view = CardView.Create(boardRoot, v, CardSize);
                view.Cell = new Cell(x, y);
                view.Rt.anchoredPosition = CellPos(x, y);
                view.Pressed = OnCardPressed;
                if (v == Board.Block) blockViews.Add(view);
                else views[x, y] = view;

                // 처음에 패를 돌리듯 차례로 날아와 앉는다.
                float delay = (x + y) * 0.025f;
                Vector2 target = CellPos(x, y);
                view.Rt.localScale = Vector3.zero;
                Anim.Run(view.Rt, 0.32f, t =>
                {
                    float e = Anim.OutBack(t);
                    view.Rt.localScale = Vector3.one * e;
                    view.Rt.anchoredPosition = Vector2.LerpUnclamped(target + new Vector2(0f, 220f), target, Anim.OutCubic(t));
                }, null, delay);
            }
        Anim.Delay(this, 0.1f, Sfx.Shuffle);
    }

    Vector2 CardSize => new Vector2(cellW * 0.94f, cellH * 0.94f);

    void ComputeCell()
    {
        Rect area = boardArea.rect;
        float aw = Mathf.Max(100f, area.width), ah = Mathf.Max(100f, area.height);
        // 판 바깥으로 도는 선이 보이도록 양쪽에 칸 반쯤 여유
        cellW = Mathf.Min(aw / (def.width + 0.9f), ah / ((def.height + 0.9f) * Art.CardAspect));
        cellW = Mathf.Min(cellW, 190f);
        cellH = cellW * Art.CardAspect;
    }

    /// <summary>칸 중심의 판 안 좌표. 판 바깥(-1, width)은 칸 0.6개만큼만 나간다.</summary>
    Vector2 CellPos(float x, float y)
    {
        float fx = x < 0 ? -0.62f : x >= def.width ? def.width - 1 + 0.62f : x;
        float fy = y < 0 ? -0.62f : y >= def.height ? def.height - 1 + 0.62f : y;
        return new Vector2((fx - (def.width - 1) * 0.5f) * cellW, ((def.height - 1) * 0.5f - fy) * cellH);
    }

    Vector2 CellPos(Cell c) => CellPos(c.X, c.Y);

    void Relayout()
    {
        if (views == null) return;
        ComputeCell();
        foreach (var v in AllViews())
        {
            v.Rt.sizeDelta = CardSize;
            v.Rt.anchoredPosition = CellPos(v.Cell);
        }
    }

    IEnumerable<CardView> AllViews()
    {
        foreach (var b in blockViews) yield return b;
        for (int y = 0; y < def.height; y++)
            for (int x = 0; x < def.width; x++)
                if (views[x, y] != null) yield return views[x, y];
    }

    // ------------------------------------------------------------ 입력
    void OnCardPressed(CardView view)
    {
        if (Paused || finished) return;
        if (views[view.Cell.X, view.Cell.Y] != view) return;
        int month = Board.Month(view.Value);

        if (bombs.Contains(month))
        {
            Detonate(view.Cell, month);
            return;
        }

        if (selected == null)
        {
            Select(view);
            return;
        }
        if (selected == view)
        {
            selected.Selected = false;
            selected = null;
            Sfx.Click();
            return;
        }
        if (Board.Month(selected.Value) == month)
        {
            var path = PathFinder.FindPath(board, selected.Cell, view.Cell);
            if (path != null)
            {
                Match(selected, view, path);
                return;
            }
            // 같은 달인데 길이 막혔다.
            Sfx.Deny();
            selected.Wiggle();
            view.Wiggle();
            ShowToast(Loc.T("길이 막혔어요 (꺾임 2번까지)", "Blocked! (max 2 turns)"));
            selected.Selected = false;
            selected = null;
            return;
        }
        Select(view);
    }

    void Select(CardView view)
    {
        if (selected != null) selected.Selected = false;
        selected = view;
        view.Selected = true;
        Sfx.Select();
        Settings.Haptic(8, 40);
    }

    // ------------------------------------------------------------ 짝
    void Match(CardView a, CardView b, List<Cell> path)
    {
        ClearHint();
        selected = null;
        a.Selected = false;
        views[a.Cell.X, a.Cell.Y] = null;
        views[b.Cell.X, b.Cell.Y] = null;
        int month = Board.Month(a.Value);

        combo = elapsed - lastMatchAt <= def.comboWindow ? combo + 1 : 1;
        lastMatchAt = elapsed;
        matches++;
        int thisCombo = combo;

        DrawPath(path, new Color(1f, 0.86f, 0.3f), 0.35f);

        // a를 들어 b 위로 "짝!" 내리친다.
        a.Rt.SetAsLastSibling();
        b.Rt.SetSiblingIndex(boardRoot.childCount - 2);
        Vector2 from = a.Rt.anchoredPosition, to = b.Rt.anchoredPosition;
        const float fly = 0.15f;
        Anim.Run(a.Rt, fly, t =>
        {
            float e = Anim.InCubic(t);
            a.Rt.anchoredPosition = Vector2.Lerp(from, to, e) + Vector2.up * Mathf.Sin(t * Mathf.PI) * cellH * 0.35f;
            a.Rt.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            a.Rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -12f, t));
        }, () =>
        {
            Sfx.Snap(thisCombo);
            Settings.Haptic(18, 120);
            Vector3 hitWorld = b.Rt.position;
            Fx.Flash(hitWorld, cellW * 3.2f, new Color(1f, 0.95f, 0.7f, 0.9f), 0.3f);
            Fx.Burst(hitWorld, 10 + Mathf.Min(thisCombo, 6) * 2, Fx.FlowerColors, 900f, Art.Petal, 0.7f, cellW * 0.35f);
            Fx.PopText(hitWorld, ComboWord(thisCombo), thisCombo >= 3 ? 96 : 84, Color.white, thisCombo >= 2 ? Art.Red : Art.WoodDark, 130f);
            Vanish(a, 0.18f);
            Vanish(b, 0.18f);
        });

        var slides = Rules.Remove(board, a.Cell, b.Cell, gravity);
        AnimateSlides(slides, fly + 0.06f);

        if (thisCombo >= 3 && thisCombo % 3 == 0) Anim.Delay(this, fly + 0.1f, () => Stack(month));
        AfterRemoval(fly + 0.3f);
    }

    static string ComboWord(int c)
    {
        if (c <= 1) return Loc.T("짝!", "Snap!");
        if (c == 2) return Loc.T("짝짝!", "Snap Snap!");
        if (c % 3 == 0) return Loc.T("쌌다!", "Stacked!");
        return Loc.T("짝 ×", "Snap ×") + c;
    }

    void Vanish(CardView v, float duration)
    {
        var rt = v.Rt;
        Vector3 start = rt.localScale;
        Anim.Run(rt, duration, t =>
        {
            rt.localScale = start * (1f - Anim.InCubic(t));
        }, () => Destroy(v.gameObject));
    }

    void AnimateSlides(List<Slide> slides, float delay)
    {
        if (slides.Count == 0) return;
        // 옮긴 순서대로 따라가야 같은 패를 두 번 옮겨도 맞는다.
        var moved = new List<(CardView view, Vector2 to)>();
        foreach (var s in slides)
        {
            var v = views[s.From.X, s.From.Y];
            if (v == null) continue;
            views[s.From.X, s.From.Y] = null;
            views[s.To.X, s.To.Y] = v;
            v.Cell = s.To;
            moved.Add((v, CellPos(s.To)));
        }
        Anim.Delay(this, delay, Sfx.Slide);
        foreach (var (view, to) in moved)
        {
            var rt = view.Rt;
            Vector2? start = null;
            Anim.Run(rt, 0.2f, t =>
            {
                if (start == null) start = rt.anchoredPosition;
                rt.anchoredPosition = Vector2.Lerp(start.Value, to, Anim.OutCubic(t));
            }, () => rt.anchoredPosition = to, delay);
        }
    }

    /// <summary>패가 없어진 뒤: 다 지웠으면 클리어, 둘 곳이 없으면 섞기.</summary>
    void AfterRemoval(float delay)
    {
        if (board.Cleared)
        {
            finished = true;
            running = false;
            Anim.Delay(this, delay + 0.25f, Win);
            return;
        }
        Anim.Delay(this, delay, CheckStuck);
    }

    bool[] BombMask()
    {
        var mask = new bool[Board.Months];
        foreach (int m in bombs) mask[m] = true;
        return mask;
    }

    void CheckStuck()
    {
        if (finished || board.Cleared) return;
        if (Rules.HasMove(board, BombMask())) return;
        if (bombs.Count > 0)
        {
            ShowToast(Loc.T("짝이 없어요! 폭탄을 써 보세요", "No pairs! Use a bomb"));
            foreach (var s in traySlots) Anim.Pop(s, 0.25f, 0.4f);
            return;
        }
        ShuffleBoard();
    }

    void ShuffleBoard()
    {
        ClearHint();
        if (selected != null) { selected.Selected = false; selected = null; }
        ShowToast(Loc.T("짝이 없어서 패를 섞어요!", "No pairs left - shuffling!"));
        Rules.Shuffle(board, gravity, rng);
        Sfx.Shuffle();
        int i = 0;
        for (int y = 0; y < def.height; y++)
            for (int x = 0; x < def.width; x++)
            {
                var v = views[x, y];
                if (v == null) continue;
                v.FlipTo(board[x, y], i++ * 0.02f);
            }
        RefreshBombMarks();
    }

    // ------------------------------------------------------------ 쌌다 / 폭탄
    void Stack(int month)
    {
        if (finished) return;
        int pick = -1;
        if (bombs.Count < MaxBombs)
        {
            if (board.CountMonth(month) > 0 && !bombs.Contains(month)) pick = month;
            else
            {
                int best = 0;
                for (int m = 0; m < Board.Months; m++)
                {
                    int c = board.CountMonth(m);
                    if (c > best && !bombs.Contains(m)) { best = c; pick = m; }
                }
            }
        }

        Sfx.Stack();
        Settings.Haptic(60, 200);
        if (pick < 0)
        {
            // 폭탄 칸이 꽉 찼거나 걸 달이 없으면 시간 보너스
            bonusTime += 5f;
            Fx.Stamp(Loc.T("쌌다!", "STACKED!"), Loc.T("+5초 보너스", "+5 sec bonus"), Art.Gold, Art.RedDark);
            return;
        }
        bombs.Add(pick);
        Fx.Stamp(Loc.T("쌌다!", "STACKED!"), Art.MonthName(pick) + Loc.T(" 폭탄 준비! 그 패를 눌러요", " bomb ready! Tap that card"), Art.Gold, Art.RedDark);

        // 판의 그 달 패에서 빛이 날아가 폭탄 칸에 들어간다.
        int slot = bombs.Count - 1;
        RefreshTray();
        var card = trayCards[slot];
        Anim.Run(card, 0.45f, t => card.rectTransform.localScale = Vector3.one * Anim.OutElastic(t));
        Fx.Flash(traySlots[slot].position, 300f, new Color(1f, 0.6f, 0.2f, 0.9f), 0.5f);
        RefreshBombMarks();
        // 폭탄이 생기면 막혔다고 섞지 않아도 될 수 있으니 다시 본다.
        CheckStuck();
    }

    void RefreshTray()
    {
        for (int i = 0; i < MaxBombs; i++)
        {
            bool has = i < bombs.Count;
            trayCards[i].gameObject.SetActive(has);
            if (has) trayCards[i].sprite = Art.Card(Art.MonthHero(bombs[i]));
        }
    }

    void RefreshBombMarks()
    {
        foreach (var v in AllViews())
            if (!v.IsBlock) v.Bomb = bombs.Contains(Board.Month(v.Value));
    }

    void PeekBomb(int index)
    {
        if (index >= bombs.Count || Paused) return;
        Sfx.Click();
        foreach (var v in AllViews())
            if (!v.IsBlock && Board.Month(v.Value) == bombs[index]) Anim.Pop(v.Rt, 0.2f, 0.35f);
        ShowToast(Loc.T("빛나는 패를 누르면 펑!", "Tap a glowing card to blast!"));
    }

    void Detonate(Cell origin, int month)
    {
        ClearHint();
        if (selected != null) { selected.Selected = false; selected = null; }
        bombs.Remove(month);
        RefreshTray();

        var cells = board.CellsOfMonth(month);
        cells.Sort((p, q) => Dist(p, origin).CompareTo(Dist(q, origin)));
        float last = 0f;
        Settings.Haptic(90, 255);
        Fx.Shake(shakeRoot, cellW * 0.25f, 0.45f);
        Fx.PopText(views[origin.X, origin.Y].Rt.position, Loc.T("펑!", "BOOM!"), 130, Art.Gold, Art.RedDark, 180f, 1.1f);
        foreach (var c in cells)
        {
            var v = views[c.X, c.Y];
            views[c.X, c.Y] = null;
            board[c] = Board.Empty;
            if (v == null) continue;
            float delay = Dist(c, origin) * 0.07f;
            last = Mathf.Max(last, delay);
            bool first = c == origin;
            v.Rt.SetAsLastSibling();
            Anim.Run(v.Rt, 0.16f, t =>
            {
                v.Rt.localScale = Vector3.one * (1f + 0.35f * t);
                v.Rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 40f) * 8f);
            }, () =>
            {
                Vector3 w = v.Rt.position;
                Sfx.Bomb(first ? 1f : 0.45f);
                Fx.Flash(w, cellW * 4f, new Color(1f, 0.75f, 0.3f, 1f), 0.4f);
                Fx.Ring(w, new Color(1f, 0.85f, 0.5f, 0.9f), cellW * 5f, 0.45f);
                Fx.Burst(w, 22, Fx.FireColors, 1300f, Art.Spark, 0.6f, cellW * 0.3f, -900f);
                Fx.Burst(w, 10, Fx.FlowerColors, 900f, Art.Petal, 0.9f, cellW * 0.35f);
                Destroy(v.gameObject);
            }, delay);
        }
        var slides = Gravity.Apply(board, gravity);
        AnimateSlides(slides, last + 0.25f);
        RefreshBombMarks();
        AfterRemoval(last + 0.45f);
    }

    static float Dist(Cell a, Cell b) => Mathf.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    // ------------------------------------------------------------ 선 그리기
    void DrawPath(List<Cell> path, Color color, float life)
    {
        var line = Ui.Rect("Path", pathLayer);
        Ui.Stretch(line);
        var group = line.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        float thick = Mathf.Max(8f, cellW * 0.14f);
        for (int pass = 0; pass < 2; pass++)
        {
            float w = pass == 0 ? thick * 2.6f : thick;
            Color c = pass == 0 ? new Color(color.r, color.g * 0.7f, color.b * 0.3f, 0.45f) : color;
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 p = CellPos(path[i]);
                var dot = Ui.Image(line, Art.Circle128, c, "Joint");
                Ui.Place(dot.rectTransform, 0.5f, 0.5f, new Vector2(w, w), p);
                if (i == 0) continue;
                Vector2 q = CellPos(path[i - 1]);
                var seg = Ui.Image(line, null, c, "Seg");
                var rt = seg.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = (p + q) * 0.5f;
                Vector2 d = p - q;
                rt.sizeDelta = new Vector2(d.magnitude, w);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }
        Anim.Run(line, life, t => group.alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f, () => Destroy(line.gameObject));
    }

    // ------------------------------------------------------------ 힌트
    void OnHint()
    {
        if (Paused || finished) return;
        if (Progress.Hints > 0)
        {
            if (ShowHint()) Progress.Hints--;
            RefreshHint();
            return;
        }
        if (!Ads.RewardedReady(Ads.Reward.Hint))
        {
            ShowToast(Loc.T("광고를 불러오는 중이에요. 잠시 뒤 다시 눌러 주세요", "Loading an ad... please try again soon"));
            return;
        }
        Ads.ShowRewarded(Ads.Reward.Hint, ok =>
        {
            if (!ok || this == null) return;
            Progress.Hints += Progress.HintsPerAd;
            Fx.PopText(hintButton.position, "+" + Progress.HintsPerAd, 80, Color.white, Art.BlueDark);
            if (ShowHint()) Progress.Hints--;
            RefreshHint();
        });
    }

    bool ShowHint()
    {
        ClearHint();
        var move = Solver.Hint(board, gravity, BombMask(), rng.Next(int.MaxValue));
        if (move == null)
        {
            if (bombs.Count > 0)
            {
                ShowToast(Loc.T("폭탄 칸의 패를 눌러 보세요!", "Try using a bomb!"));
                foreach (var s in traySlots) Anim.Pop(s, 0.25f, 0.4f);
            }
            return false;
        }
        var m = move.Value;
        hint = new[] { views[m.A.X, m.A.Y], views[m.B.X, m.B.Y] };
        foreach (var v in hint) if (v != null) v.Hinted = true;
        var path = PathFinder.Connect(board, m.A, m.B);
        if (path != null) DrawPath(path, new Color(0.5f, 0.95f, 1f), 1.2f);
        Sfx.Hint();
        return true;
    }

    void ClearHint()
    {
        if (hint == null) return;
        foreach (var v in hint) if (v != null) v.Hinted = false;
        hint = null;
    }

    void RefreshHint()
    {
        int n = Progress.Hints;
        hintCount.gameObject.SetActive(n > 0);
        hintCount.text = n.ToString();
        hintAd.gameObject.SetActive(n <= 0);
    }

    // ------------------------------------------------------------ 시간
    void Update()
    {
        if (def == null) return;
        if (!Paused && !finished)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (elapsed >= Limit) TimeUp();
        }
        RefreshTimer();
        RefreshCombo();
    }

    void RefreshTimer()
    {
        float remain = Mathf.Max(0f, Limit - elapsed);
        int secs = Mathf.CeilToInt(remain);
        timeText.text = (secs / 60) + ":" + (secs % 60).ToString("00");
        timeFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(remain / Limit), 1f);

        bool warn = remain <= 10f && !finished;
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
        timeFill.color = warn ? Color.Lerp(Art.Red, new Color(1f, 0.5f, 0.4f), blink) : remain / Limit < 0.3f ? new Color(1f, 0.55f, 0.2f) : Art.Gold;
        timeText.color = warn ? Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), blink) : Color.white;
        if (warn && running && !Paused && secs != lastTickSecond)
        {
            lastTickSecond = secs;
            Sfx.Tick();
        }

        // 별 기준 표시: 막대가 이 자리 아래로 줄면 별이 하나씩 꺼진다.
        float barW = timerBar.rect.width;
        float[] lose = { def.star3, def.star2 };
        for (int i = 0; i < 2; i++)
        {
            float f = Mathf.Clamp01((Limit - lose[i]) / Limit);
            starMarks[i].anchoredPosition = new Vector2(f * barW, 0f);
            bool alive = elapsed <= lose[i] && revives == 0;
            starMarks[i].GetComponent<Image>().color = alive ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
        int stars = revives > 0 ? 1 : def.StarsFor(elapsed);
        for (int i = 0; i < 3; i++)
            hudStars[i].color = i < stars ? Art.Gold : new Color(0f, 0f, 0f, 0.35f);
    }

    void RefreshCombo()
    {
        float left = combo > 0 ? 1f - (elapsed - lastMatchAt) / def.comboWindow : 0f;
        if (left <= 0f && combo > 0 && !finished) combo = 0;
        comboRing.fillAmount = Mathf.Clamp01(left);
        int toBomb = 3 - combo % 3;
        comboText.text = combo > 0 ? "×" + combo : "-";
        comboRing.color = combo % 3 == 2 ? Color.Lerp(Art.Gold, Art.Red, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f)) : Art.Gold;
        comboRoot.localScale = Vector3.one * (combo % 3 == 2 && toBomb == 1 ? 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 12f) : 1f);
    }

    void TimeUp()
    {
        running = false;
        ClearHint();
        if (selected != null) { selected.Selected = false; selected = null; }
        Sfx.Fail();
        Settings.Haptic(120, 200);
        int bonus = ReviveSeconds;
        var p = Popup.Open(new Vector2(880f, revives < MaxRevives ? 900f : 760f), Loc.T("시간 초과!", "Time's up!"));
        p.CanClose = false;
        p.OnBackPressed = () => { };
        p.Label(Loc.T("남은 패 ", "Cards left: ") + board.CardCount + Loc.T("장", ""), 54, Art.Ink, 170f);
        float y = 300f;
        if (revives < MaxRevives)
        {
            p.Label(Loc.T("광고를 보고 " + bonus + "초 더 할까요?", "Watch an ad for +" + bonus + " seconds?"), 46, Art.GreyDark, 250f);
            p.Button(Loc.T("+" + bonus + "초 이어하기", "Continue +" + bonus + "s"), Art.IconVideo, Art.Green, Art.GreenDark, y + 90f, () =>
            {
                if (!Ads.RewardedReady(Ads.Reward.Revive))
                {
                    ShowToast(Loc.T("광고를 불러오는 중이에요. 잠시 뒤 다시 눌러 주세요", "Loading an ad... please try again soon"));
                    return;
                }
                Ads.ShowRewarded(Ads.Reward.Revive, ok =>
                {
                    if (!ok || this == null) return;
                    p.Close();
                    Revive(bonus);
                });
            }, 0f, 0f, 150f);
            y += 200f;
        }
        p.Button(Loc.T("다시 하기", "Retry"), Art.IconRetry, Art.Red, Art.RedDark, y + 80f, () => App.I.Play(def.number));
        p.Button(Loc.T("스테이지 목록", "Stage List"), Art.IconGrid, Art.Wood, Art.WoodDark, y + 230f, () => App.I.ShowStages(def.number));
    }

    int ReviveSeconds => Mathf.Max(30, Mathf.RoundToInt(def.timeLimit * 0.3f / 10f) * 10);

    void Revive(int bonus)
    {
        revives++;
        bonusTime = elapsed - def.timeLimit + bonus;
        running = true;
        Sfx.Revive();
        Fx.PopText(timeText.rectTransform.position, "+" + bonus + Loc.T("초", "s"), 80, Color.white, Art.GreenDark, 120f, 1.2f);
        CheckStuck();
    }

    // ------------------------------------------------------------ 클리어
    void Win()
    {
        int seconds = Mathf.CeilToInt(elapsed);
        int stars = revives > 0 ? 1 : def.StarsFor(elapsed);
        int prevBest = Progress.BestSeconds(def.number);
        bool newBest = Progress.Record(def.number, stars, seconds);
        Sfx.Clear();
        Settings.Haptic(80, 180);
        Fx.Confetti(70);
        Fx.Stamp(Loc.T("클리어!", "CLEAR!"), null, Art.Gold, Art.RedDark, 0f);

        Anim.Delay(this, 1.1f, () =>
        {
            bool last = def.number >= App.I.Stages.stages.Length;
            var p = Popup.Open(new Vector2(900f, 1060f), Loc.T("스테이지 ", "Stage ") + def.number + Loc.T(" 클리어", " Clear"));
            p.CanClose = false;
            p.OnBackPressed = () => App.I.ShowStages(def.number);

            // 큰 별 셋이 차례로 찍힌다.
            for (int i = 0; i < 3; i++)
            {
                var slot = Ui.Image(p.Panel, Art.Star, new Color(0f, 0f, 0f, 0.18f), "StarSlot" + i);
                float size = i == 1 ? 210f : 170f;
                Ui.Place(slot.rectTransform, 0.5f, 1f, new Vector2(size, size), new Vector2((i - 1) * 220f, i == 1 ? -230f : -260f));
                if (i >= stars) continue;
                var star = Ui.Image(slot.rectTransform, Art.Star, Art.Gold, "Star");
                Ui.Stretch(star.rectTransform);
                star.rectTransform.localScale = Vector3.zero;
                int index = i;
                Anim.Run(star, 0.35f, t => star.rectTransform.localScale = Vector3.one * Anim.OutBack(t), () =>
                {
                    Sfx.Star(index);
                    Fx.Burst(star.rectTransform.position, 14, Fx.FireColors, 700f, Art.Star, 0.6f, 30f, -1000f);
                }, 0.25f + i * 0.35f);
            }

            p.Label(Loc.T("걸린 시간  ", "Time  ") + Clock(seconds), 60, Art.Ink, 430f);
            string bestLine = newBest && prevBest > 0 ? Loc.T("새 기록!", "New record!") : Loc.T("최고 기록  ", "Best  ") + Clock(Progress.BestSeconds(def.number));
            p.Label(bestLine, 44, newBest && prevBest > 0 ? Art.Red : Art.GreyDark, 500f);
            p.Label("★★★ " + Clock(def.star3) + "     ★★ " + Clock(def.star2), 40, Art.GreyDark, 560f);

            float y = 690f;
            if (!last)
            {
                p.Button(Loc.T("다음 스테이지", "Next Stage"), Art.IconPlay, Art.Green, Art.GreenDark, y, () =>
                {
                    int next = def.number + 1;
                    if (!Ads.TryShowInterstitial(def.number, () => App.I.Play(next))) App.I.Play(next);
                }, 0f, 0f, 150f);
                y += 190f;
            }
            float half = (900f - 140f - 30f) / 2f;
            p.Button(Loc.T("다시", "Retry"), Art.IconRetry, Art.Red, Art.RedDark, y, () => App.I.Play(def.number), half, -(half + 30f) / 2f);
            p.Button(Loc.T("목록", "Stages"), Art.IconGrid, Art.Wood, Art.WoodDark, y, () => App.I.ShowStages(def.number), half, (half + 30f) / 2f);
        });
    }

    public static string Clock(int seconds) => (seconds / 60) + ":" + (seconds % 60).ToString("00");

    // ------------------------------------------------------------ 일시정지·설정
    public void OnBack()
    {
        if (!finished) OpenPause();
    }

    void OpenPause()
    {
        if (finished || App.I.PopupOpen || !running) return;
        if (selected != null) { selected.Selected = false; selected = null; }
        var p = Popup.Open(new Vector2(880f, 1280f), Loc.T("일시정지", "Paused"), true);
        p.Button(Loc.T("계속하기", "Resume"), Art.IconPlay, Art.Green, Art.GreenDark, 190f, () => p.Close(), 0f, 0f, 140f);
        p.Button(Loc.T("다시 하기", "Restart"), Art.IconRetry, Art.Red, Art.RedDark, 350f, () => App.I.Play(def.number), 0f, 0f, 140f);
        p.Button(Loc.T("스테이지 목록", "Stage List"), Art.IconGrid, Art.Wood, Art.WoodDark, 510f, () => App.I.ShowStages(def.number), 0f, 0f, 140f);
        p.Toggle(Loc.T("효과음", "Sound"), 660f, () => Settings.SoundOn, v => Settings.SoundOn = v);
        p.Toggle(Loc.T("배경음", "Music"), 780f, () => Settings.MusicOn, v => { Settings.MusicOn = v; Sfx.RefreshMusic(); });
        p.Toggle(Loc.T("진동", "Vibration"), 900f, () => Settings.VibrationOn, v => Settings.VibrationOn = v);
        p.Toggle(Loc.T("월 숫자 표시", "Month numbers"), 1020f, () => Settings.ShowMonth, v =>
        {
            Settings.ShowMonth = v;
            foreach (var view in AllViews()) view.RefreshMonthBadge();
        });
        p.Button(Loc.T("규칙 보기", "How to play"), null, Art.Blue, Art.BlueDark, 1150f, () => Tips.ShowRules(), 0f, 0f, 110f);
    }

    void ShowToast(string text)
    {
        toast.text = text;
        toast.gameObject.SetActive(true);
        var rt = toast.rectTransform;
        Anim.Run(toast, 1.8f, t =>
        {
            float a = t < 0.1f ? t / 0.1f : t > 0.8f ? (1f - t) / 0.2f : 1f;
            toast.color = new Color(1f, 1f, 1f, a);
            rt.localScale = Vector3.one * (t < 0.1f ? Mathf.Lerp(0.8f, 1f, t / 0.1f) : 1f);
        }, () => toast.gameObject.SetActive(false));
    }

    void ShowTipIfNeeded()
    {
        if (def.number == 1) Tips.ShowOnce("rules", Tips.ShowRules);
        else if (def.number == 2) Tips.ShowOnce("combo", Tips.ShowCombo);
        else if (gravity != GravityMode.None) Tips.ShowOnce("gravity", () => Tips.ShowGravity(gravity));
        else if (blockViews.Count > 0) Tips.ShowOnce("blocks", Tips.ShowBlocks);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ------------------------------------------------------------ 개발용 (DevAutoShot)
    /// <summary>둘 수 있는 짝 하나를 실제로 누른 것처럼 맞춘다. 폭탄 달은 피한다.</summary>
    public bool DevPlayMove()
    {
        if (Paused || finished) return false;
        var moves = Rules.FindMoves(board, BombMask());
        if (moves.Count == 0) return false;
        var m = moves[0];
        if (selected != null) { selected.Selected = false; selected = null; }
        OnCardPressed(views[m.A.X, m.A.Y]);
        OnCardPressed(views[m.B.X, m.B.Y]);
        return true;
    }

    /// <summary>폭탄 칸의 첫 폭탄을 터뜨린다.</summary>
    public bool DevDetonate()
    {
        if (bombs.Count == 0) return false;
        var cells = board.CellsOfMonth(bombs[0]);
        if (cells.Count == 0) return false;
        OnCardPressed(views[cells[0].X, cells[0].Y]);
        return true;
    }

    public void DevSelectFirst()
    {
        foreach (var v in AllViews()) if (!v.IsBlock) { OnCardPressed(v); return; }
    }

    public void DevHint() => ShowHint();
    public void DevTimeUp() => elapsed = Limit;
    public void DevPause() => OpenPause();
    public void DevWin()
    {
        foreach (var c in board.CardCells()) board[c] = Board.Empty;
        finished = true;
        running = false;
        Win();
    }
#endif
}
