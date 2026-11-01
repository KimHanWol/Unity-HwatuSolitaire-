using System;
using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼: 정해진 작은 판(20장: 아래층 4×4 + 2층 4장)에서 손가락으로 짚어 주며 차례로 해 본다.
///  ① 짝: 2층의 3월 벚꽃 두 장 (짝을 지우면 아래 두 줄이 열린다)
///  ② 덮인 패: 아직 2층 매조가 덮고 있는 패를 눌러 보면 안 눌린다
///  ③ 쌌다(뻑): 1월 두 장으로 짝 → 포개진 동안 셋째 패 → 뻑 폭탄 (고리는 평소처럼 줄고, 다 줄면 다시 찬다)
///  ④ 폭탄 쓰기: 남은 1월 패를 누르면 그 패와 둘레의 매조 짝이 함께 펑 → 위 두 줄이 열린다
///  ⑤ 8월·12월·9월로 쌌다 세 번 → 뻑 폭탄 셋 → "다음"을 누르면 폭탄 마무리 → 싹쓸이 → 황금 광
/// 짚은 패 말고는 누를 수 없다. 끝나면 TutorialDirector.Done 이 켜지고 1탄으로 간다.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    const string DoneKey = "TutorialDone";

    public static bool Done
    {
        get => PlayerPrefs.GetInt(DoneKey, 0) == 1;
        set { PlayerPrefs.SetInt(DoneKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>튜토리얼에서 폭탄이 둘레 짝을 몇 쌍 터뜨리는지 (매조 한 쌍만 정확히 터지게).</summary>
    public const int BlastPairs = 1;

    // ------------------------------------------------------------ 판 (자리 번호)
    // 2층: X1(1,1) X2(5,1) — 아래층 위 두 줄을 덮는다 (2월 매조)
    //      P1(1,5) P2(5,5) — 아래층 아래 두 줄을 덮는다 (3월 벚꽃, 첫 짝)
    // 아래층 (x = 0,2,4,6):
    //   y=0: B4 C3 D3 C4
    //   y=2: B2 C2 B3 D4
    //   y=4: A4 A1 A2 D1
    //   y=6: B1 A3 C1 D2
    // 폭탄 쓰기: A4(0,4)에서 터지면, 열린 패 가운데 가장 가까운 같은 달 한 쌍이 매조(X1·X2)가 되도록
    // 아래 두 줄의 다른 달은 한 장씩만 가깝게, 겹치는 9월(D1·D2)은 매조 X2보다 멀리 둔다.
    const int X1 = 0, X2 = 1, P1 = 2, P2 = 3;
    const int B4 = 4, C3 = 5, D3 = 6, C4 = 7;
    const int B2 = 8, C2 = 9, B3 = 10, D4 = 11;
    const int A4 = 12, A1 = 13, A2 = 14, D1 = 15;
    const int B1 = 16, A3 = 17, C1 = 18, D2 = 19;
    const int MonthP = 2, MonthX = 1, MonthA = 0, MonthB = 7, MonthC = 11, MonthD = 8; // 3월 벚꽃, 2월 매조, 1월 송학, 8월 공산, 12월 비, 9월 국진

    public static StageDef Stage()
    {
        int[] tiles =
        {
            1, 1, 1,  5, 1, 1,  1, 5, 1,  5, 5, 1,
            0, 0, 0,  2, 0, 0,  4, 0, 0,  6, 0, 0,
            0, 2, 0,  2, 2, 0,  4, 2, 0,  6, 2, 0,
            0, 4, 0,  2, 4, 0,  4, 4, 0,  6, 4, 0,
            0, 6, 0,  2, 6, 0,  4, 6, 0,  6, 6, 0,
        };
        var cells = new int[20];
        // 첫 짝은 거의 똑같이 생긴 벚꽃 피 두 장 ("같은 그림"이 바로 보이게)
        cells[P1] = MonthP * 4 + 2; cells[P2] = MonthP * 4 + 3;
        cells[X1] = MonthX * 4 + 0; cells[X2] = MonthX * 4 + 2;
        cells[A1] = MonthA * 4 + 0; cells[A2] = MonthA * 4 + 2; cells[A3] = MonthA * 4 + 3; cells[A4] = MonthA * 4 + 1;
        cells[B1] = MonthB * 4 + 0; cells[B2] = MonthB * 4 + 1; cells[B3] = MonthB * 4 + 2; cells[B4] = MonthB * 4 + 3;
        cells[C1] = MonthC * 4 + 0; cells[C2] = MonthC * 4 + 1; cells[C3] = MonthC * 4 + 2; cells[C4] = MonthC * 4 + 3;
        cells[D1] = MonthD * 4 + 0; cells[D2] = MonthD * 4 + 1; cells[D3] = MonthD * 4 + 2; cells[D4] = MonthD * 4 + 3;
        return new StageDef
        {
            number = 0,
            shape = "tutorial",
            tiles = tiles,
            cells = cells,
            stackWindow = 2f,
            star3 = 9999, star2 = 9999, timeLimit = 9999,
        };
    }

    // ------------------------------------------------------------ 진행
    GameScreen game;
    RectTransform panel, pointer, arrow, ring;
    Text text;
    Button next;
    // 한 번에 한 가지만 말하려고 나눈 안내 ("다음"으로 넘긴다). 18번은 폭탄 마무리 중.
    const int StepTryDark = 19, StepLocked = 20, StepBombMade = 21, StepSweep = 22;
    int step = -1;
    int expect = -1;          // 지금 누를 수 있는 자리 (-1 이면 없음)
    RectTransform target;     // 손가락이 가리키는 곳
    float time;

    /// <summary>판 위 안내 글 자리(가로 띠)와 손가락을 만든다. top: 화면 위에서부터의 위치.</summary>
    public void Init(GameScreen screen, RectTransform root, float top, float height)
    {
        game = screen;
        panel = Ui.Rect("TutorialPanel", root);
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.offsetMin = new Vector2(30f, -(top + height));
        panel.offsetMax = new Vector2(-30f, -top);
        var bg = Ui.Image(panel, Art.Round, Art.Paper, "Bg");
        Ui.Stretch(bg.rectTransform);
        var edge = Ui.Image(panel, Art.RoundOutline, Art.GoldDark, "Edge");
        Ui.Stretch(edge.rectTransform, 6f);
        text = Ui.Text(panel, "", 40, Art.Ink, TextAnchor.MiddleLeft, "Text");
        Ui.Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(40f, 10f);
        text.rectTransform.offsetMax = new Vector2(-40f, -10f);
        next = Ui.Button(panel, new Vector2(200f, 96f), Art.Green, Art.GreenDark, Loc.T("다음", "Next"), null, Next, 42, "Next");
        // 오른쪽 가운데: 아래 테두리(와 버튼 아랫단 그림자)에 닿지 않게
        Ui.Place((RectTransform)next.transform, 1f, 0.5f, new Vector2(200f, 96f), new Vector2(-135f, 6f));
        next.gameObject.SetActive(false);

        pointer = Ui.Rect("Pointer", root);
        pointer.sizeDelta = new Vector2(10f, 10f);
        // 쌌다 고리와 같은 납작한 금빛 고리 + 둘레의 은은한 빛
        ring = Ui.Rect("Ring", pointer);
        Ui.Place(ring, 0.5f, 0.5f, new Vector2(200f, 200f));
        var halo = Ui.Image(ring, Art.RingHalo, new Color(1f, 0.82f, 0.35f, 0.6f), "Halo");
        Ui.Stretch(halo.rectTransform);
        halo.rectTransform.anchorMin = Vector2.one * -0.175f;
        halo.rectTransform.anchorMax = Vector2.one * 1.175f;
        var band = Ui.Image(ring, Art.RingThin, Art.Gold, "Band");
        Ui.Stretch(band.rectTransform);
        arrow = Ui.Rect("Arrow", pointer);
        Ui.Place(arrow, 0.5f, 0.5f, new Vector2(90f, 90f), new Vector2(0f, 170f));
        var arrowShadow = Ui.Image(arrow, Art.IconBack, new Color(0f, 0f, 0f, 0.3f), "Shadow");
        Ui.Place(arrowShadow.rectTransform, 0.5f, 0.5f, new Vector2(90f, 90f), new Vector2(0f, -6f));
        var arrowImg = Ui.Image(arrow, Art.IconBack, Art.Gold, "Image");
        Ui.Stretch(arrowImg.rectTransform);
        arrowShadow.rectTransform.localEulerAngles = arrowImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f); // 아래를 가리킨다
        foreach (var g in pointer.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        pointer.gameObject.SetActive(false);
    }

    public void StartLesson() => Go(0);

    /// <summary>지금 이 자리를 눌러도 되는지 (짚은 패만).</summary>
    public bool Allow(int tile) => tile == expect;

    /// <summary>다른 패를 눌렀을 때: 손가락을 한 번 들썩여 알려 준다.</summary>
    public void Nudge()
    {
        Sfx.Blocked();
        if (pointer.gameObject.activeSelf) Anim.Pop(pointer, 0.35f, 0.35f);
    }

    public void OnSelected(int tile)
    {
        if (step == 0 && tile == P1) Go(1);
        else if (step == 3 && tile == A1) Go(4);
        else if (step == 8 && tile == B1) Go(9);
        else if (step == 11 && tile == C1) Go(12);
        else if (step == 14 && tile == D1) Go(15);
    }

    /// <summary>덮인 패를 눌렀을 때 (직접 눌러 보고 안 눌리는 걸 확인하는 단계).</summary>
    public void OnBlockedTap(int tile)
    {
        if (step == StepTryDark && tile == C2) Later(1.1f, StepLocked);
    }

    public void OnMatched(int month)
    {
        if (step == 1 && month == MonthP) Later(0.7f, 2);
        else if (step == 4 && month == MonthA) Go(5);
        else if (step == 9 && month == MonthB) Go(10);
        else if (step == 12 && month == MonthC) Go(13);
        else if (step == 15 && month == MonthD) Go(16);
    }

    public void OnStacked(int month)
    {
        if (step == 5 && month == MonthA) Later(1.4f, StepBombMade);
        else if (step == 10 && month == MonthB) Later(1.4f, 11);
        else if (step == 13 && month == MonthC) Later(1.4f, 14);
        else if (step == 16 && month == MonthD) Later(1.4f, 17);
    }

    /// <summary>폭탄을 직접 터뜨렸을 때.</summary>
    public void OnDetonated(int month)
    {
        if (step == 6 && month == MonthA) Later(1.4f, 7);
    }

    void Later(float seconds, int nextStep)
    {
        Hide();
        Anim.Delay(this, seconds, () => Go(nextStep));
    }

    void Next()
    {
        if (step == 2) Go(StepTryDark);
        else if (step == StepLocked) Go(3);
        else if (step == StepBombMade) Go(6);
        else if (step == 7) Go(8);
        else if (step == 17) Go(StepSweep);
        else if (step == StepSweep)
        {
            // 설명을 다 읽고 나서 폭탄 마무리
            Hide();
            step = 18;
            game.TutorialFinale();
        }
    }

    void Hide()
    {
        expect = -1;
        target = null;
        pointer.gameObject.SetActive(false);
        ShowNext(false);
    }

    /// <summary>"다음" 버튼을 보이거나 숨기고, 글이 버튼과 겹치지 않게 글자 영역 오른쪽을 비킨다.</summary>
    void ShowNext(bool show)
    {
        next.gameObject.SetActive(show);
        text.rectTransform.offsetMax = new Vector2(show ? -260f : -40f, -10f);
    }

    void Go(int s)
    {
        step = s;
        Hide();
        switch (s)
        {
            // ① 짝
            case 0:
                Say(Loc.T("화투 사천성에 온 걸 환영해요!\n<color=#C8102E>같은 그림</color> 패 두 장을 차례로 누르면 <color=#C8102E>짝!</color> 하고 없어져요.",
                          "Welcome! Tap two cards with the\n<color=#C8102E>same picture</color> to <color=#C8102E>snap</color> them away."));
                Point(P1);
                break;
            case 1:
                Say(Loc.T("좋아요! 똑같은 벚꽃 패를 하나 더 눌러요.", "Good! Now tap the matching cherry card."));
                Point(P2);
                break;
            // ② 덮인 패
            case 2:
                Say(Loc.T("패는 층층이 쌓여 있어요.\n벚꽃을 치우니 아래 패가 밝아졌죠?", "Cards are stacked in layers.\nThe cards below lit up!"));
                ShowNext(true);
                break;
            case StepTryDark:
                Say(Loc.T("이번엔 <color=#C8102E>어두운 패</color>를 한번 눌러 볼까요?", "Now try tapping a <color=#C8102E>dark card</color>."));
                Point(C2);
                break;
            case StepLocked:
                Say(Loc.T("패 위에 다른 패가 올라가 있어서\n<color=#C8102E>어둡게 보이는 패</color>는 선택할 수 없어요.",
                          "A card with another card on top looks dark\nand <color=#C8102E>can't be selected</color>."));
                ShowNext(true);
                break;
            // ③ 쌌다
            case 3:
                Say(Loc.T("다음은 <color=#C8102E>쌌다!</color>\n소나무 패 두 장으로 먼저 짝!", "Next up: <color=#C8102E>Ppeok</color>!\nFirst, snap two pine cards."));
                Point(A1);
                break;
            case 4:
                Say(Loc.T("그림이 조금 달라도 <color=#C8102E>같은 종류</color>면 짝이에요.\n소나무 패를 하나 더!", "Pictures can differ: the <color=#C8102E>same kind</color> is a match.\nOne more pine card!"));
                Point(A2);
                break;
            case 5:
                Say(Loc.T("짝 맞춘 두 장이 잠깐 포개져 있어요.\n금빛 고리가 줄기 전에 <color=#C8102E>같은 종류 패를 하나 더</color> 누르면…",
                          "The pair stays stacked for a moment.\nTap <color=#C8102E>one more of that kind</color> before the ring runs out..."));
                Point(A3);
                break;
            // ④ 폭탄 쓰기
            case StepBombMade:
                Say(Loc.T("<color=#C8102E>쌌다!</color> 세 장이 뭉쳐\n<color=#C8102E>뻑 폭탄</color>이 됐어요.", "<color=#C8102E>Stacked!</color> That's a\n<color=#C8102E>Ppeok bomb</color>."));
                ShowNext(true);
                break;
            case 6:
                Say(Loc.T("남은 소나무 패를 누르면 주변 패까지 펑!\n눌러 볼까요?", "Tap the last pine card to blast it\nand nearby pairs!"));
                Point(A4);
                break;
            case 7:
                Say(Loc.T("펑! 위를 덮던 패까지 함께 날아가서\n위쪽 패가 모두 열렸어요.", "Boom! The cards on top went too,\nso every card is open now."));
                ShowNext(true);
                break;
            // ⑤ 쌌다 세 번 → 싹쓸이
            case 8:
                Say(Loc.T("이번엔 폭탄을 모아 볼게요.\n짝을 맞추고, 같은 패를 한 장 더!", "Now let's collect bombs.\nSnap a pair, then tap one more!"));
                Point(B1);
                break;
            case 9:
                Say(Loc.T("짝!", "Snap!"));
                Point(B2);
                break;
            case 10:
                Say(Loc.T("고리가 줄기 전에 셋째 패!", "The third card, before the ring runs out!"));
                Point(B3);
                break;
            case 11:
                Say(Loc.T("비 패로 한 번 더.", "Again with the rain cards."));
                Point(C1);
                break;
            case 12:
                Say(Loc.T("짝!", "Snap!"));
                Point(C2);
                break;
            case 13:
                Say(Loc.T("셋째 패!", "The third card!"));
                Point(C3);
                break;
            case 14:
                Say(Loc.T("마지막! 국화 패로 한 번 더.", "Last one! Chrysanthemum cards."));
                Point(D1);
                break;
            case 15:
                Say(Loc.T("짝!", "Snap!"));
                Point(D2);
                break;
            case 16:
                Say(Loc.T("셋째 패!", "The third card!"));
                Point(D3);
                break;
            case 17:
                Say(Loc.T("뻑 폭탄이 셋!\n폭탄과 같은 패만 남으면\n저절로 펑! 펑! 펑! 터져요.", "Three bombs!\nWhen only cards matching your bombs are left,\nthey blow up on their own."));
                ShowNext(true);
                break;
            case StepSweep:
                Say(Loc.T("뻑 폭탄 셋으로 판을 비우면\n<color=#C8102E>싹쓸이</color>!", "Clear the board with three bombs:\n<color=#C8102E>Sweep</color>!"));
                ShowNext(true);
                break;
        }
    }

    void Say(string message)
    {
        text.text = message;
        Anim.Pop(panel, 0.04f, 0.25f);
    }

    void Point(int tile)
    {
        expect = tile;
        PointAt(game.TileRect(tile));
    }

    void PointAt(RectTransform rt)
    {
        target = rt;
        pointer.gameObject.SetActive(rt != null);
    }

    void LateUpdate()
    {
        if (pointer == null || target == null) return;
        time += Time.unscaledDeltaTime;
        pointer.position = target.position;
        float size = Mathf.Max(target.rect.width, target.rect.height) * 1.15f;
        ring.sizeDelta = Vector2.one * size * (1f + 0.04f * Mathf.Sin(time * 5f));
        arrow.anchoredPosition = new Vector2(0f, size * 0.5f + 46f + 10f * Mathf.Sin(time * 5f));
    }

    /// <summary>튜토리얼 판을 다 비웠을 때 (싹쓸이): 끝났다고 알리고 1탄으로.</summary>
    public void Finish()
    {
        Hide();
        Done = true;
        Anim.Delay(this, 1.3f, () => AskMonthNumbers(ShowEnd));
    }

    /// <summary>
    /// 패 귀퉁이에 몇 월인지 숫자를 띄울지 묻는다. 화투가 낯설면 켜고(같은 달 찾기가 쉬워진다), 잘 알면 끈다.
    /// 나중에 설정·일시정지 창의 "월 숫자 표시"로 언제든 바꿀 수 있다.
    /// </summary>
    void AskMonthNumbers(Action then)
    {
        var p = Popup.Open(new Vector2(880f, 1000f), Loc.T("월 숫자 표시", "Month numbers"));
        p.CanClose = false;
        p.OnBackPressed = () => { };

        // 예시: 숫자가 붙은 패 두 장 (같은 1월)
        float w = 150f, h = w * Art.CardAspect;
        for (int i = 0; i < 2; i++)
        {
            var card = Ui.Image(p.Panel, Art.Card(i == 0 ? 0 : 3), Color.white, "Card" + i);
            Ui.Place(card.rectTransform, 0.5f, 1f, new Vector2(w, h), new Vector2((i - 0.5f) * 190f, -(160f + h / 2f)));
            var badge = Ui.Image(card.rectTransform, Art.Circle128, Art.Ink, "Badge");
            badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(0f, 1f);
            badge.rectTransform.sizeDelta = new Vector2(60f, 60f);
            badge.rectTransform.anchoredPosition = new Vector2(12f, -12f);
            var ring = Ui.Image(badge.rectTransform, Art.Ring, Art.Gold, "Ring");
            Ui.Stretch(ring.rectTransform);
            var n = Ui.Text(badge.rectTransform, "1", 36, Color.white, TextAnchor.MiddleCenter, "N");
            Ui.Stretch(n.rectTransform);
        }

        p.Label(Loc.T("패 귀퉁이에 몇 월인지 숫자를 보여 줄까요?\n화투가 낯설면 켜 두는 게 편해요.\n(설정에서 언제든 바꿀 수 있어요)",
                      "Show the month number on each card?\nHandy if Korean flower cards are new to you.\n(You can change this anytime in Settings)"), 38, Art.Ink, 560f, 170f);

        void Choose(bool on)
        {
            Settings.ShowMonth = on;
            p.Close();
            then?.Invoke();
        }
        p.Button(Loc.T("숫자 보기 (화투가 낯설어요)", "Show numbers (new to Hwatu)"), null, Art.Green, Art.GreenDark, 730f, () => Choose(true), 0f, 0f, 120f);
        p.Button(Loc.T("숫자 끄기 (화투를 알아요)", "Hide numbers (I know Hwatu)"), null, Art.Wood, Art.WoodDark, 880f, () => Choose(false), 0f, 0f, 120f);
    }

    void ShowEnd()
    {
        var p = Popup.Open(new Vector2(880f, 900f), Loc.T("튜토리얼 끝!", "Tutorial done!"));
        p.CanClose = false;
        p.OnBackPressed = () => App.I.ShowTitle();
        var coin = Ui.Image(p.Panel, Art.GwangGold, Color.white, "Coin");
        Ui.Place(coin.rectTransform, 0.5f, 1f, new Vector2(200f, 200f), new Vector2(0f, -250f));
        coin.rectTransform.localScale = Vector3.zero;
        Anim.Run(coin, 0.45f, t => coin.rectTransform.localScale = Vector3.one * Anim.OutBack(t), () =>
        {
            Sfx.Golden();
            Fx.Burst(coin.rectTransform.position, 30, Fx.FireColors, 900f, Art.Star, 0.9f, 34f, -800f);
        }, 0.2f);
        p.Label(Loc.T("모든 패를 시간 안에 없애면 <color=#C8102E>클리어!</color>\n뻑 폭탄 3개로 마무리하면 <color=#B7831C>황금 광</color>을 얻어요.",
                      "Clear all cards in time to <color=#C8102E>win!</color>\nFinish with three Ppeok bombs for a <color=#B7831C>Golden Gwang</color>."), 42, Art.Ink, 470f, 140f);
        p.Button(Loc.T("1탄 시작", "Start Stage 1"), Art.IconPlay, Art.Green, Art.GreenDark, 650f, () => App.I.Play(1), 0f, 0f, 150f);
        p.Button(Loc.T("처음 화면", "Title"), null, Art.Wood, Art.WoodDark, 800f, () => App.I.ShowTitle(), 0f, 0f, 110f);
    }
}
