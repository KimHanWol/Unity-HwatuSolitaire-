using System.Collections.Generic;
using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 화면. 위: 일시정지·스테이지 이름·남은 시간과 광 기준. 가운데: 판. 아래: 쌌다(폭탄) 칸·힌트.
///
/// 규칙(화투 솔리테어, 타일 매치 방식): 패가 여러 층으로 쌓여 있다. 위가 덮이지 않은 "열린 패" 두 장이 같은 달이면
/// "짝!" 하고 지운다 (옆은 상관없다). 덮인 패는 살짝 어둡게 보인다.
/// 쌌다: 짝을 맞추면 두 장이 그 자리에 잠깐(stackWindow초, 고리 시간) 포개져 있다. 그 사이에 같은 달 열린 패를 하나 더 누르면
/// 고스톱처럼 세 장이 뭉쳐 "쌌다!" → 그 뭉치가 아래 폭탄 칸으로 간다 (최대 3뭉치).
/// 폭탄: 폭탄 칸에 있는 달의 열린 패를 판에서 하나 누르면 그 패와 둘레의 짝 몇 쌍이 한꺼번에 펑.
/// 짝 단위로 지우므로 달마다 장수는 짝수로 돌아온다. 막히면(폭탄도 못 쓰면) 자동으로 섞는데, 섞은 판도 늘 풀린다(Rules.Shuffle).
/// 시간 안에 다 지우면 클리어(걸린 시간으로 광), 시간이 다 되면 실패 → 광고 보고 시간 더 받아 이어하기.
/// </summary>
public class GameScreen : MonoBehaviour, IBackHandler
{
    const int MaxBombs = 3;
    const int MaxRevives = 2;
    const float TopBar = 150f, TimerRow = 96f, BottomBar = 230f;

    StageDef def;
    Board board;
    Rng rng;

    // 자리 번호(Layout.Tiles 순서)마다 그 자리 패 (지워졌으면 null)
    CardView[] views;
    CardView selected;
    CardView[] hint;

    RectTransform boardArea, shakeRoot, boardRoot;
    // 패 한 장 크기(cellW x cellH)와 층마다 비키는 거리
    float cellW, cellH;
    Vector2 layerShift, boardOrigin;

    float elapsed, bonusTime;
    bool running, finished;
    int revives;
    // 짝을 맞춘 뒤 셋째 패를 기다리는 뭉치 (없으면 pending == null)
    class Pending
    {
        public int month;
        public float left;
        public CardView a, b;
        public Image ring;
    }
    Pending pending;
    int lastTickSecond = -1;
    readonly List<int> bombs = new List<int>();
    int matches;

    // HUD
    Text stageText, chapterText, timeText;
    Image timeFill;
    Image[] hudStars;
    RectTransform[] starMarks;
    RectTransform timerBar;
    RectTransform[] traySlots;
    RectTransform[] trayFans;
    Image[][] trayFanCards;
    // 폭탄 칸마다 뭉친 세 장의 패 번호 (bombs와 같은 순서)
    readonly List<int[]> bombCards = new List<int[]>();
    static readonly float[] FanAngles = { 6f, -6f, 0f }; // 맨 앞(마지막) 장은 빈 칸에 똑바로
    static readonly float[] FanOffsets = { -4f, 4f, 0f };
    // 기울인 뒤쪽 두 장은 모서리가 칸 아래로 처지지 않게 조금 올린다.
    static readonly float[] FanLift = { 5f, 5f, 0f };
    Text hintCount;
    Image hintAd;
    RectTransform hintButton;
    Text toast;

    bool Paused => !running || App.I.PopupOpen || Ads.Showing;
    float Limit => def.timeLimit + bonusTime;

    public void Begin(StageDef stage)
    {
        def = stage;
        board = def.CreateBoard();
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

        // 오른쪽 위: 광 3개 (지금 속도면 몇 광인지)
        hudStars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var s = Ui.Image(top, Art.Gwang, Color.white, "HudGwang" + i);
            Ui.Place(s.rectTransform, 1f, 0.5f, new Vector2(70f, 70f), new Vector2(-206f + i * 70f, 0f));
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
            var m = Ui.Image(timerBar, Art.Gwang, Color.white, "Mark" + i);
            m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            m.rectTransform.sizeDelta = new Vector2(52f, 52f);
            starMarks[i] = m.rectTransform;
        }
        timeText = Ui.Text(timer, "0:00", 54, Color.white, TextAnchor.MiddleCenter, "Time");
        Ui.Place(timeText.rectTransform, 1f, 0.5f, new Vector2(180f, 80f), new Vector2(-100f, 0f));
        Ui.Outline(timeText, Art.WoodDark, 3f);

        // 아래 띠: 쌌다 칸 / 힌트
        var bottom = Ui.Rect("Bottom", rt);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.sizeDelta = new Vector2(0f, BottomBar);
        var tray = Ui.Rect("Tray", bottom);
        Ui.Place(tray, 0f, 0.5f, new Vector2(560f, 200f), new Vector2(320f, 0f));
        var trayBg = Ui.Image(tray, Art.Round, new Color(0f, 0f, 0f, 0.35f), "Bg");
        Ui.Stretch(trayBg.rectTransform);
        var trayEdge = Ui.Image(tray, Art.RoundOutline, new Color(Art.Gold.r, Art.Gold.g, Art.Gold.b, 0.6f), "Edge");
        Ui.Stretch(trayEdge.rectTransform, 4f);
        var trayLabel = Ui.Text(tray, Loc.T("쌌다! 폭탄", "BOMBS"), 32, Art.Gold, TextAnchor.MiddleCenter, "Label");
        Ui.Place(trayLabel.rectTransform, 0.5f, 1f, new Vector2(400f, 44f), new Vector2(0f, -6f));
        Ui.Outline(trayLabel, Art.WoodDark, 2f);
        traySlots = new RectTransform[MaxBombs];
        trayFans = new RectTransform[MaxBombs];
        trayFanCards = new Image[MaxBombs][];
        for (int i = 0; i < MaxBombs; i++)
        {
            var slot = Ui.Rect("Slot" + i, tray);
            Ui.Place(slot, 0.5f, 0.5f, new Vector2(88f, 143f), new Vector2((i - 1) * 165f, -6f));
            var empty = Ui.Image(slot, Art.RoundSmall, new Color(1f, 1f, 1f, 0.12f), "Empty");
            Ui.Stretch(empty.rectTransform);
            // 뭉친 세 장을 빈 칸에 맞춰 겹쳐 놓고, 아래쪽 가운데를 축으로 살짝만 벌려 세 장임을 보여 준다.
            var fan = Ui.Rect("Fan", slot);
            Ui.Stretch(fan);
            trayFanCards[i] = new Image[3];
            for (int k = 0; k < 3; k++)
            {
                var c = Ui.Image(fan, null, Color.white, "Card" + k);
                var crt = c.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
                crt.pivot = new Vector2(0.5f, 0f);
                crt.sizeDelta = new Vector2(88f, 143f); // 아래 빈 칸과 같은 크기
                crt.anchoredPosition = new Vector2(FanOffsets[k], FanLift[k]);
                crt.localEulerAngles = new Vector3(0f, 0f, FanAngles[k]);
                trayFanCards[i][k] = c;
            }
            var icon = Ui.Image(fan, Art.IconBomb, Color.white, "Bomb");
            Ui.Place(icon.rectTransform, 0.5f, 0f, new Vector2(60f, 60f), new Vector2(0f, 22f));
            fan.gameObject.SetActive(false);
            traySlots[i] = slot;
            trayFans[i] = fan;
            int index = i;
            var btn = slot.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var hitImg = slot.gameObject.AddComponent<Image>();
            hitImg.color = new Color(0, 0, 0, 0);
            btn.onClick.AddListener(() => PeekBomb(index));
        }


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

        toast = Ui.Text(rt, "", 48, Color.white, TextAnchor.MiddleCenter, "Toast");
        Ui.Place(toast.rectTransform, 0.5f, 0f, new Vector2(1000f, 80f), new Vector2(0f, BottomBar + 50f));
        Ui.Outline(toast, Art.WoodDark, 3f);
        toast.gameObject.SetActive(false);
    }

    void BuildBoard()
    {
        var layout = def.Layout;
        views = new CardView[layout.Count];
        ComputeCell();
        // 아래층부터, 같은 층은 위쪽 줄·왼쪽부터 그려야 위층 패가 아래 패를 덮는다.
        var order = new List<int>();
        for (int i = 0; i < layout.Count; i++) order.Add(i);
        order.Sort((a, b) =>
        {
            Tile ta = layout.Tiles[a], tb = layout.Tiles[b];
            if (ta.Z != tb.Z) return ta.Z.CompareTo(tb.Z);
            if (ta.Y != tb.Y) return ta.Y.CompareTo(tb.Y);
            return ta.X.CompareTo(tb.X);
        });
        foreach (int i in order)
        {
            if (!board.Present(i)) continue;
            var view = CardView.Create(boardRoot, board[i], CardSize);
            view.Index = i;
            view.Pressed = OnCardPressed;
            views[i] = view;
            Vector2 target = TilePos(i);
            view.Rt.anchoredPosition = target;

            // 처음에 패를 돌리듯 층마다 차례로 날아와 앉는다.
            Tile t = layout.Tiles[i];
            float delay = t.Z * 0.18f + (t.X + t.Y) * 0.012f;
            view.Rt.localScale = Vector3.zero;
            Anim.Run(view.Rt, 0.32f, k =>
            {
                view.Rt.localScale = Vector3.one * Anim.OutBack(k);
                view.Rt.anchoredPosition = Vector2.LerpUnclamped(target + new Vector2(0f, 220f), target, Anim.OutCubic(k));
            }, null, delay);
        }
        RefreshFree(true);
        Anim.Delay(this, 0.1f, Sfx.Shuffle);
    }

    Vector2 CardSize => new Vector2(cellW * 0.97f, cellH * 0.97f);

    /// <summary>판이 화면에 꽉 차게 패 크기를 정한다. 층마다 왼쪽 위로 조금씩 비켜 쌓인 두께도 넣는다.</summary>
    void ComputeCell()
    {
        var layout = def.Layout;
        Rect area = boardArea.rect;
        float aw = Mathf.Max(100f, area.width), ah = Mathf.Max(100f, area.height);
        float cols = (layout.MaxX - layout.MinX) / 2f, rows = (layout.MaxY - layout.MinY) / 2f;
        int z = layout.MaxZ;
        const float shiftX = 0.11f, shiftY = 0.07f; // 패 크기에 대한 한 층의 비킴
        cellW = Mathf.Min(aw / (cols + z * shiftX + 0.15f), ah / ((rows + z * shiftY + 0.15f) * Art.CardAspect));
        cellW = Mathf.Min(cellW, 200f);
        cellH = cellW * Art.CardAspect;
        layerShift = new Vector2(-cellW * shiftX, cellH * shiftY);
        // 판 전체(층 비킴 포함)의 가운데가 화면 가운데에 오게
        float width = cols * cellW + z * cellW * shiftX, height = rows * cellH + z * cellH * shiftY;
        boardOrigin = new Vector2(-width / 2f + z * cellW * shiftX + cellW / 2f, height / 2f - z * cellH * shiftY - cellH / 2f);
    }

    /// <summary>자리 i의 판 안 좌표 (패 가운데).</summary>
    Vector2 TilePos(int i)
    {
        var layout = def.Layout;
        Tile t = layout.Tiles[i];
        float x = (t.X - layout.MinX) / 2f * cellW, y = -(t.Y - layout.MinY) / 2f * cellH;
        return boardOrigin + new Vector2(x, y) + layerShift * t.Z;
    }

    void Relayout()
    {
        if (views == null) return;
        ComputeCell();
        foreach (var v in AllViews())
        {
            v.Rt.sizeDelta = CardSize;
            v.Rt.anchoredPosition = TilePos(v.Index);
        }
    }

    IEnumerable<CardView> AllViews()
    {
        foreach (var v in views) if (v != null) yield return v;
    }

    /// <summary>열린 패는 밝게, 덮인 패는 살짝 어둡게.</summary>
    void RefreshFree(bool instant = false)
    {
        foreach (var v in AllViews()) v.SetBlocked(!board.Free(v.Index), instant);
    }

    // ------------------------------------------------------------ 입력
    void OnCardPressed(CardView view)
    {
        if (Paused || finished) return;
        int i = view.Index;
        if (views[i] != view) return;
        int month = Board.Month(view.Value);

        // 폭탄 달의 열린 패를 누르면 터진다.
        if (bombs.Contains(month) && board.Free(i))
        {
            Detonate(i, month);
            return;
        }

        if (!board.Free(i))
        {
            Sfx.Deny();
            view.Wiggle();
            ShowToast(Loc.T("위에 다른 패가 덮고 있어요", "Covered by another card"));
            return;
        }

        if (pending != null && pending.month == month)
        {
            if (bombs.Count < MaxBombs)
            {
                StackThird(view);
                return;
            }
            ShowToast(Loc.T("폭탄 칸이 꽉 찼어요! 폭탄부터 쓰세요", "Bomb tray is full! Use a bomb first"));
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
            Match(selected, view);
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
    void Match(CardView a, CardView b)
    {
        ClearHint();
        selected = null;
        a.Selected = false;
        views[a.Index] = null;
        views[b.Index] = null;
        int month = Board.Month(a.Value);

        matches++;
        // 앞서 기다리던 뭉치는 여기서 끝난다.
        FinishPending();

        // a를 들어 b 위로 "짝!" 내리친다.
        a.Rt.SetAsLastSibling();
        b.Rt.SetSiblingIndex(boardRoot.childCount - 2);
        NoHit(a);
        NoHit(b);
        Vector2 from = a.Rt.anchoredPosition, to = b.Rt.anchoredPosition;
        const float fly = 0.15f;

        Rules.Remove(board, a.Index, b.Index);
        RefreshFree();
        // 판에 같은 달이 더 남아 있으면 두 장이 잠깐 포개져 셋째 패를 기다린다.
        Pending p = null;
        if (board.CountMonth(month) > 0 && def.stackWindow > 0f)
        {
            p = new Pending { month = month, left = def.stackWindow + fly, a = a, b = b };
            pending = p;
        }

        Anim.Run(a.Rt, fly, t =>
        {
            float e = Anim.InCubic(t);
            a.Rt.anchoredPosition = Vector2.Lerp(from, to, e) + Vector2.up * Mathf.Sin(t * Mathf.PI) * cellH * 0.35f;
            a.Rt.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            a.Rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -12f, t));
        }, () =>
        {
            Sfx.Snap(1);
            Settings.Haptic(18, 120);
            Vector3 hitWorld = b.Rt.position;
            Fx.Flash(hitWorld, cellW * 3.2f, new Color(1f, 0.95f, 0.7f, 0.9f), 0.3f);
            Fx.Burst(hitWorld, 12, Fx.FlowerColors, 900f, Art.Petal, 0.7f, cellW * 0.35f);
            Fx.PopText(hitWorld, Loc.T("짝!", "Snap!"), 84, Color.white, Art.WoodDark, 130f);
            if (p != null && pending == p) ShowPendingRing(p);
            else if (p == null)
            {
                Vanish(a, 0.18f);
                Vanish(b, 0.18f);
            }
        });

        AfterRemoval(fly + 0.3f);
    }

    static void NoHit(CardView v)
    {
        var img = v.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    /// <summary>포개진 두 장 둘레에 줄어드는 금빛 고리 (셋째 패를 누를 수 있는 남은 시간).</summary>
    void ShowPendingRing(Pending p)
    {
        if (p.b == null) return;
        p.a.Rt.localEulerAngles = new Vector3(0f, 0f, -10f);
        p.a.Rt.localScale = Vector3.one * 0.92f;
        p.b.Rt.localScale = Vector3.one * 0.92f;
        var ring = Ui.Image(boardRoot, Art.Ring, new Color(1f, 0.8f, 0.25f, 0.95f), "StackRing");
        Ui.Place(ring.rectTransform, 0.5f, 0.5f, Vector2.one * cellH * 1.25f, p.b.Rt.anchoredPosition);
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = false;
        p.ring = ring;
    }

    /// <summary>기다리던 뭉치를 정리한다 (시간이 다 됐거나 다른 짝을 맞췄을 때): 두 장이 그냥 사라진다.</summary>
    void FinishPending()
    {
        if (pending == null) return;
        var p = pending;
        pending = null;
        if (p.ring != null) Destroy(p.ring.gameObject);
        if (p.a != null) Vanish(p.a, 0.18f);
        if (p.b != null) Vanish(p.b, 0.18f);
    }

    void UpdatePending(float dt)
    {
        if (pending == null) return;
        pending.left -= dt;
        if (pending.ring != null)
        {
            float k = Mathf.Clamp01(pending.left / def.stackWindow);
            pending.ring.fillAmount = k;
            pending.ring.color = k < 0.35f ? Color.Lerp(Art.Red, Art.Gold, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 20f)) : new Color(1f, 0.8f, 0.25f, 0.95f);
            // 포개진 패가 들썩여서 "한 장 더 얹을 수 있다"는 느낌을 준다.
            if (pending.b != null) pending.b.Rt.localScale = Vector3.one * (0.92f + 0.03f * Mathf.Sin(Time.unscaledTime * 14f));
        }
        if (pending.left <= 0f) FinishPending();
    }

    /// <summary>포개진 두 장 위로 같은 달 셋째 패를 얹는다: "쌌다!" 세 장이 뭉쳐 폭탄 칸으로 날아간다.</summary>
    void StackThird(CardView third)
    {
        var p = pending;
        pending = null;
        ClearHint();
        if (selected != null) { selected.Selected = false; selected = null; }
        if (p.ring != null) Destroy(p.ring.gameObject);

        views[third.Index] = null;
        board[third.Index] = Board.Empty;
        NoHit(third);
        third.Rt.SetAsLastSibling();
        RefreshFree();

        bombs.Add(p.month);
        bombCards.Add(new[] { p.b != null ? p.b.Value : third.Value, p.a != null ? p.a.Value : third.Value, third.Value });
        int slot = Mathf.Min(bombs.Count - 1, MaxBombs - 1);
        Vector2 from = third.Rt.anchoredPosition;
        Vector2 to = p.b != null ? p.b.Rt.anchoredPosition : from;
        const float fly = 0.16f;
        Anim.Run(third.Rt, fly, t =>
        {
            third.Rt.anchoredPosition = Vector2.Lerp(from, to, Anim.InCubic(t)) + Vector2.up * Mathf.Sin(t * Mathf.PI) * cellH * 0.4f;
            third.Rt.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(t * Mathf.PI));
            third.Rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 10f, t));
        }, () =>
        {
            Vector3 w = third.Rt.position;
            Sfx.Snap(1);
            Sfx.Stack();
            Settings.Haptic(60, 200);
            Fx.Flash(w, cellW * 4.5f, new Color(1f, 0.7f, 0.25f, 1f), 0.45f);
            Fx.Ring(w, new Color(1f, 0.8f, 0.3f, 0.9f), cellW * 4f, 0.4f);
            Fx.Burst(w, 20, Fx.FireColors, 1000f, Art.Spark, 0.6f, cellW * 0.3f, -900f);
            Fx.Stamp(Loc.T("쌌다!", "STACKED!"), Art.MonthName(p.month) + Loc.T(" 폭탄! 같은 달 패를 누르면 펑", " bomb! Tap that month to blast"), Art.Gold, Art.RedDark);

            // 세 장 뭉치가 폭탄 칸으로 날아간다.
            var pile = new List<CardView> { third };
            if (p.a != null) pile.Add(p.a);
            if (p.b != null) pile.Add(p.b);
            Vector3 target = traySlots[slot].position;
            for (int i = 0; i < pile.Count; i++)
            {
                var v = pile[i];
                Vector3 start = Vector3.zero, startScale = Vector3.one;
                bool began = false;
                Anim.Run(v.Rt, 0.4f, t =>
                {
                    if (!began) { began = true; start = v.Rt.position; startScale = v.Rt.localScale; }
                    float e = Anim.InOutCubic(t);
                    v.Rt.position = Vector3.Lerp(start, target, e);
                    v.Rt.localScale = startScale * Mathf.Lerp(1f, 0.6f, e);
                }, () => Destroy(v.gameObject), 0.35f + i * 0.05f);
            }
            Anim.Delay(this, 0.85f, () =>
            {
                RefreshTray();
                var fan = trayFans[slot];
                var cards = trayFanCards[slot];
                // 포개진 채 들어와서 부채처럼 촤르륵 펼쳐진다.
                Anim.Run(fan, 0.5f, t =>
                {
                    fan.localScale = Vector3.one * Anim.OutElastic(Mathf.Min(1f, t * 1.4f));
                    float open = Anim.OutBack(Mathf.Clamp01((t - 0.2f) / 0.8f));
                    for (int k = 0; k < 3; k++)
                    {
                        cards[k].rectTransform.localEulerAngles = new Vector3(0f, 0f, FanAngles[k] * open);
                        cards[k].rectTransform.anchoredPosition = new Vector2(FanOffsets[k] * open, FanLift[k] * open);
                    }
                });
                Fx.Flash(fan.position, 300f, new Color(1f, 0.6f, 0.2f, 0.9f), 0.5f);
            });
        });

        RefreshBombMarks();
        AfterRemoval(fly + 0.3f);
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
        if (BombUsable())
        {
            ShowToast(Loc.T("짝이 없어요! 폭탄을 써 보세요", "No pairs! Use a bomb"));
            foreach (var s in traySlots) Anim.Pop(s, 0.25f, 0.4f);
            return;
        }
        ShuffleBoard();
    }

    /// <summary>폭탄 칸의 달 가운데 판에서 위가 드러난 패가 있는지.</summary>
    bool BombUsable()
    {
        foreach (int m in bombs)
            foreach (int i in board.TilesOfMonth(m))
                if (board.Exposed(i)) return true;
        return false;
    }

    void ShuffleBoard()
    {
        ClearHint();
        if (selected != null) { selected.Selected = false; selected = null; }
        ShowToast(Loc.T("짝이 없어서 패를 섞어요!", "No pairs left - shuffling!"));
        var move = Rules.Shuffle(board, rng);
        Sfx.Shuffle();

        // 자리를 옮긴 패는 새 자리로 날아가고, 모든 패가 뒤집히며 새 그림이 된다.
        var old = views;
        views = new CardView[old.Length];
        int n = 0;
        for (int i = 0; i < old.Length; i++)
        {
            var v = old[i];
            if (v == null || move[i] < 0) continue;
            int to = move[i];
            views[to] = v;
            v.Index = to;
            if (to != i)
            {
                var rt = v.Rt;
                Vector2 from = rt.anchoredPosition, target = TilePos(to);
                Anim.Run(rt, 0.35f, t => rt.anchoredPosition = Vector2.Lerp(from, target, Anim.InOutCubic(t)), null, n * 0.02f);
            }
            v.FlipTo(board[to], n++ * 0.015f);
        }
        // 옮긴 뒤 그리는 순서(아래층 먼저)를 다시 맞춘다.
        var order = new List<int>();
        for (int i = 0; i < views.Length; i++) if (views[i] != null) order.Add(i);
        order.Sort((a, b) =>
        {
            Tile ta = def.Layout.Tiles[a], tb = def.Layout.Tiles[b];
            if (ta.Z != tb.Z) return ta.Z.CompareTo(tb.Z);
            if (ta.Y != tb.Y) return ta.Y.CompareTo(tb.Y);
            return ta.X.CompareTo(tb.X);
        });
        foreach (int i in order) views[i].Rt.SetAsLastSibling();
        RefreshBombMarks();
        RefreshFree();
    }

    // ------------------------------------------------------------ 폭탄
    void RefreshTray()
    {
        for (int i = 0; i < MaxBombs; i++)
        {
            bool has = i < bombs.Count;
            trayFans[i].gameObject.SetActive(has);
            if (!has) continue;
            for (int k = 0; k < 3; k++)
            {
                var c = trayFanCards[i][k];
                c.sprite = Art.Card(bombCards[i][k]);
                c.rectTransform.localEulerAngles = new Vector3(0f, 0f, FanAngles[k]);
                c.rectTransform.anchoredPosition = new Vector2(FanOffsets[k], FanLift[k]);
            }
        }
    }

    void RefreshBombMarks()
    {
        foreach (var v in AllViews())
            v.Bomb = bombs.Contains(Board.Month(v.Value));
    }

    void PeekBomb(int index)
    {
        if (index >= bombs.Count || Paused) return;
        Sfx.Click();
        foreach (var v in AllViews())
            if (Board.Month(v.Value) == bombs[index]) Anim.Pop(v.Rt, 0.2f, 0.35f);
        ShowToast(Loc.T("빛나는 패를 누르면 펑!", "Tap a glowing card to blast!"));
    }

    void Detonate(int origin, int month)
    {
        ClearHint();
        if (selected != null) { selected.Selected = false; selected = null; }
        int slotIndex = bombs.IndexOf(month);
        if (slotIndex >= 0)
        {
            // 폭탄 칸의 뭉치가 눌린 패 쪽으로 날아간다.
            var fromSlot = traySlots[slotIndex];
            var flyer = Ui.Image(Fx.Layer != null ? Fx.Layer : (RectTransform)transform, Art.Card(bombCards[slotIndex][2]), Color.white, "BombFly");
            flyer.rectTransform.sizeDelta = new Vector2(80f, 80f * Art.CardAspect);
            flyer.rectTransform.position = fromSlot.position;
            Vector3 start = fromSlot.position, end = views[origin].Rt.position;
            Anim.Run(flyer, 0.18f, t =>
            {
                flyer.rectTransform.position = Vector3.Lerp(start, end, Anim.InCubic(t));
                flyer.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 360f);
            }, () => Destroy(flyer.gameObject));
        }
        if (slotIndex >= 0)
        {
            bombs.RemoveAt(slotIndex);
            bombCards.RemoveAt(slotIndex);
        }
        RefreshTray();

        var cells = BlastTiles(origin);
        float last = 0f;
        Settings.Haptic(90, 255);
        Fx.Shake(shakeRoot, cellW * 0.25f, 0.45f);
        Fx.PopText(views[origin].Rt.position, Loc.T("펑!", "BOOM!"), 130, Art.Gold, Art.RedDark, 180f, 1.1f);
        foreach (var c in cells)
        {
            var v = views[c];
            views[c] = null;
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
        RefreshBombMarks();
        RefreshFree();
        AfterRemoval(last + 0.45f);
    }

    /// <summary>
    /// 폭탄에 터질 패: 누른 패 1장(뭉친 세 장과 합쳐 그 달이 짝수가 된다) + 가까운 곳부터 고른 같은 달 짝 몇 쌍(달 상관없이).
    /// 위가 드러난 패만 고른다 (덮인 패가 먼저 사라지면 이상하므로). 짝 단위라 달마다 장수는 짝수로 유지된다.
    /// 다른 폭탄이 걸린 달은 그 폭탄을 쓸 패가 남도록 건드리지 않는다. 쌍 수는 2~5쌍 (남은 패 24장마다 1쌍 더).
    /// </summary>
    List<int> BlastTiles(int origin)
    {
        var result = new List<int> { origin };
        int pairs = Mathf.Clamp(2 + board.CardCount / 24, 2, 5);
        var near = new List<int>();
        for (int i = 0; i < board.Count; i++) if (i != origin && board.Exposed(i)) near.Add(i);
        near.Sort((p, q) => Dist(p, origin).CompareTo(Dist(q, origin)));
        var waiting = new int[Board.Months];
        for (int m = 0; m < waiting.Length; m++) waiting[m] = -1;
        foreach (int c in near)
        {
            if (pairs <= 0) break;
            int m = Board.Month(board[c]);
            if (bombs.Contains(m)) continue;
            if (waiting[m] < 0)
            {
                waiting[m] = c;
                continue;
            }
            result.Add(waiting[m]);
            result.Add(c);
            waiting[m] = -1;
            pairs--;
        }
        result.Sort((p, q) => Dist(p, origin).CompareTo(Dist(q, origin)));
        return result;
    }

    /// <summary>두 자리 사이 거리 (패 칸 단위, 층은 무시).</summary>
    float Dist(int a, int b)
    {
        Tile ta = def.Layout.Tiles[a], tb = def.Layout.Tiles[b];
        float dx = (ta.X - tb.X) / 2f, dy = (ta.Y - tb.Y) / 2f;
        return Mathf.Sqrt(dx * dx + dy * dy);
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
        var move = Solver.Hint(board, BombMask(), rng.Next(int.MaxValue));
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
        hint = new[] { views[m.A], views[m.B] };
        foreach (var v in hint) if (v != null) v.Hinted = true;
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
        if (!Paused && !finished) UpdatePending(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        RefreshTimer();
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
            starMarks[i].GetComponent<Image>().color = alive ? Color.white : Art.GwangEmpty;
        }
        int stars = revives > 0 ? 1 : def.StarsFor(elapsed);
        for (int i = 0; i < 3; i++)
            hudStars[i].color = i < stars ? Color.white : Art.GwangEmpty;
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

            // 큰 광 셋이 차례로 도장처럼 찍힌다.
            for (int i = 0; i < 3; i++)
            {
                var slot = Ui.Image(p.Panel, Art.Gwang, new Color(0f, 0f, 0f, 0.15f), "GwangSlot" + i);
                float size = i == 1 ? 210f : 170f;
                Ui.Place(slot.rectTransform, 0.5f, 1f, new Vector2(size, size), new Vector2((i - 1) * 220f, i == 1 ? -230f : -260f));
                if (i >= stars) continue;
                var star = Ui.Image(slot.rectTransform, Art.Gwang, Color.white, "Gwang");
                Ui.Stretch(star.rectTransform);
                star.rectTransform.localScale = Vector3.zero;
                int index = i;
                Anim.Run(star, 0.3f, t => star.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, Anim.InCubic(t)), () =>
                {
                    Sfx.Star(index);
                    Sfx.Snap(1);
                    Fx.Burst(star.rectTransform.position, 14, Fx.FireColors, 700f, Art.Spark, 0.6f, 30f, -1000f);
                }, 0.25f + i * 0.35f);
            }

            p.Label(Loc.T("걸린 시간  ", "Time  ") + Clock(seconds), 60, Art.Ink, 430f);
            string bestLine = newBest && prevBest > 0 ? Loc.T("새 기록!", "New record!") : Loc.T("최고 기록  ", "Best  ") + Clock(Progress.BestSeconds(def.number));
            p.Label(bestLine, 44, newBest && prevBest > 0 ? Art.Red : Art.GreyDark, 500f);
            p.Label(Loc.T("3광 ", "3 Gwang ") + Clock(def.star3) + "     " + Loc.T("2광 ", "2 Gwang ") + Clock(def.star2), 40, Art.GreyDark, 560f);

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
        else if (def.number == 2) Tips.ShowOnce("stack", Tips.ShowCombo);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ------------------------------------------------------------ 개발용 (DevAutoShot)
    /// <summary>둘 수 있는 짝 하나를 실제로 누른 것처럼 맞춘다. 폭탄 달은 피한다.</summary>
    public bool DevPlayMove()
    {
        if (Paused || finished) return false;
        var moves = Rules.FindMoves(board, BombMask());
        if (moves.Count == 0) return false;
        // 쌌다를 시험할 수 있게 같은 달이 4장 이상 남은 짝을 먼저
        var m = moves.Find(x => board.CountMonth(Board.Month(board[x.A])) >= 4);
        if (m.A == m.B) m = moves[0];
        if (selected != null) { selected.Selected = false; selected = null; }
        OnCardPressed(views[m.A]);
        OnCardPressed(views[m.B]);
        return true;
    }

    /// <summary>포개진 뭉치가 있으면 같은 달 셋째 패를 누른다.</summary>
    public bool DevStackThird()
    {
        if (pending == null) return false;
        foreach (int i in board.TilesOfMonth(pending.month))
            if (board.Free(i)) { OnCardPressed(views[i]); return true; }
        return false;
    }

    /// <summary>폭탄 칸의 첫 폭탄을 터뜨린다.</summary>
    public bool DevDetonate()
    {
        if (bombs.Count == 0) return false;
        foreach (int i in board.TilesOfMonth(bombs[0]))
            if (board.Exposed(i)) { OnCardPressed(views[i]); return true; }
        return false;
    }

    public void DevSelectFirst()
    {
        foreach (var v in AllViews()) if (board.Free(v.Index)) { OnCardPressed(v); return; }
    }

    public void DevHint() => ShowHint();
    public void DevTimeUp() => elapsed = Limit;
    public void DevPause() => OpenPause();
    public void DevWin()
    {
        for (int i = 0; i < board.Count; i++) board[i] = Board.Empty;
        finished = true;
        running = false;
        Win();
    }
#endif
}
