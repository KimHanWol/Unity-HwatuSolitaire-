using System;
using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼: 정해진 작은 판(2층 16장)에서 손가락으로 짚어 주며 차례로 해 본다.
///  ① 짝: 처음부터 열려 있는 3월 벚꽃 두 장
///  ② 덮인 패: 어둡게 보이는 패를 눌러 보면 안 눌린다 → 위층 2월 매조 짝을 치우면 아래 패가 밝아진다
///  ③ 쌌다(뻑): 1월 두 장으로 짝 → 포개진 동안 셋째 패 → 뻑 폭탄 (튜토리얼에서는 고리 시간이 멈춰 있다)
///     같은 걸 8월·12월로 두 번 더 → 뻑 폭탄 셋
///  ④ 판에 폭탄 달 패만 남으면 폭탄 마무리 → 싹쓸이 → 황금 광 (설명을 읽고 "다음"을 눌러야 터진다)
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

    // ------------------------------------------------------------ 판 (자리 번호)
    // 위층: X1(1,1) X2(5,1) — 아래층 위쪽 두 줄 여덟 장을 덮는다.
    // 아래층 (x = 0,2,4,6):
    //   y=0: A4 B2 B3 C4   ← 덮여 있음
    //   y=2: C1 C2 C3 B4   ← 덮여 있음
    //   y=4: A1 A2 A3 B1   ← 처음부터 열려 있음
    //   y=6:    P1 P2      ← 처음부터 열려 있음 (첫 짝)
    const int X1 = 0, X2 = 1, A4 = 2, B2 = 3, B3 = 4, C4 = 5, C1 = 6, C2 = 7, C3 = 8, B4 = 9, A1 = 10, A2 = 11, A3 = 12, B1 = 13, P1 = 14, P2 = 15;
    const int MonthP = 2, MonthX = 1, MonthA = 0, MonthB = 7, MonthC = 11; // 3월 벚꽃, 2월 매조, 1월 송학, 8월 공산, 12월 비

    public static StageDef Stage()
    {
        int[] tiles =
        {
            1, 1, 1,  5, 1, 1,
            0, 0, 0,  2, 0, 0,  4, 0, 0,  6, 0, 0,
            0, 2, 0,  2, 2, 0,  4, 2, 0,  6, 2, 0,
            0, 4, 0,  2, 4, 0,  4, 4, 0,  6, 4, 0,
            2, 6, 0,  4, 6, 0,
        };
        var cells = new int[16];
        cells[P1] = MonthP * 4 + 0; cells[P2] = MonthP * 4 + 3;
        cells[X1] = MonthX * 4 + 0; cells[X2] = MonthX * 4 + 2;
        cells[A1] = MonthA * 4 + 0; cells[A2] = MonthA * 4 + 2; cells[A3] = MonthA * 4 + 3; cells[A4] = MonthA * 4 + 1;
        cells[B1] = MonthB * 4 + 0; cells[B2] = MonthB * 4 + 1; cells[B3] = MonthB * 4 + 2; cells[B4] = MonthB * 4 + 3;
        cells[C1] = MonthC * 4 + 0; cells[C2] = MonthC * 4 + 1; cells[C3] = MonthC * 4 + 2; cells[C4] = MonthC * 4 + 3;
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
    RectTransform panel, pointer, arrow;
    Text text;
    Button next;
    Image ring;
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
        Ui.Place((RectTransform)next.transform, 1f, 0f, new Vector2(200f, 96f), new Vector2(-130f, 62f));
        next.gameObject.SetActive(false);

        pointer = Ui.Rect("Pointer", root);
        pointer.sizeDelta = new Vector2(10f, 10f);
        ring = Ui.Image(pointer, Art.Ring, Art.Gold, "Ring");
        Ui.Place(ring.rectTransform, 0.5f, 0.5f, new Vector2(200f, 200f));
        var arrowImg = Ui.Image(pointer, Art.IconBack, Art.Gold, "Arrow");
        arrow = arrowImg.rectTransform;
        Ui.Place(arrow, 0.5f, 0.5f, new Vector2(90f, 90f), new Vector2(0f, 170f));
        arrow.localEulerAngles = new Vector3(0f, 0f, 90f); // 아래를 가리킨다
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
        else if (step == 3 && tile == X1) Go(4);
        else if (step == 6 && tile == A1) Go(7);
        else if (step == 10 && tile == B1) Go(11);
        else if (step == 13 && tile == C1) Go(14);
    }

    /// <summary>덮인 패를 눌렀을 때 (직접 눌러 보고 안 눌리는 걸 확인하는 단계).</summary>
    public void OnBlockedTap(int tile)
    {
        if (step == 2 && tile == B2) Later(1.1f, 3);
    }

    public void OnMatched(int month)
    {
        if (step == 1 && month == MonthP) Later(0.7f, 2);
        else if (step == 4 && month == MonthX) Later(0.6f, 5);
        else if (step == 7 && month == MonthA) Go(8);
        else if (step == 11 && month == MonthB) Go(12);
        else if (step == 14 && month == MonthC) Go(15);
    }

    public void OnStacked(int month)
    {
        if (step == 8 && month == MonthA) Later(1.4f, 9);
        else if (step == 12 && month == MonthB) Later(1.4f, 13);
        else if (step == 15 && month == MonthC) Later(1.4f, 16);
    }

    void Later(float seconds, int nextStep)
    {
        Hide();
        Anim.Delay(this, seconds, () => Go(nextStep));
    }

    void Next()
    {
        if (step == 5) Go(6);
        else if (step == 9) Go(10);
        else if (step == 16)
        {
            // 설명을 다 읽고 나서 폭탄 마무리
            Hide();
            step = 17;
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
                Say(Loc.T("화투 사천성에 온 걸 환영해요!\n같은 달(같은 꽃) 패 두 장을 차례로 누르면 <color=#C8102E>짝!</color> 하고 없어져요.",
                          "Welcome! Tap two cards of the same month\n(same flower) to <color=#C8102E>snap</color> them away."));
                Point(P1);
                break;
            case 1:
                Say(Loc.T("좋아요! 같은 3월 벚꽃 패를 하나 더 눌러요.", "Good! Now tap the other March card."));
                Point(P2);
                break;
            // ② 덮인 패
            case 2:
                Say(Loc.T("패가 층층이 쌓여 있어요. <color=#C8102E>어둡게 보이는 패</color>는\n위에 다른 패가 덮고 있는 패예요. 하나 눌러 볼까요?",
                          "Cards are stacked in layers. <color=#C8102E>Darker cards</color>\nare covered by another card. Try tapping one."));
                Point(B2);
                break;
            case 3:
                Say(Loc.T("덮인 패는 <color=#C8102E>눌리지 않아요</color>. 위 패부터 치워야 해요.\n위에 있는 2월 매조 두 장을 짝지어 볼까요?",
                          "Covered cards <color=#C8102E>can't be picked</color>. Clear the top first.\nSnap the two February cards on top."));
                Point(X1);
                break;
            case 4:
                Say(Loc.T("같은 2월 매조 패를 하나 더!", "The other February card!"));
                Point(X2);
                break;
            case 5:
                Say(Loc.T("위 패를 치우니 아래 패가 밝아졌죠?\n이제 이 패들을 고를 수 있어요.", "The cards below lit up.\nNow they can be picked."));
                ShowNext(true);
                break;
            // ③ 쌌다
            case 6:
                Say(Loc.T("이번엔 <color=#C8102E>쌌다!</color>를 해 볼게요.\n1월 송학 두 장으로 먼저 짝!", "Now let's make a <color=#C8102E>Ppeok</color>!\nFirst, snap two January cards."));
                Point(A1);
                break;
            case 7:
                Say(Loc.T("같은 1월 패를 하나 더!", "One more January card!"));
                Point(A2);
                break;
            case 8:
                Say(Loc.T("짝 맞춘 두 장이 잠깐 포개져 있어요.\n금빛 고리가 줄기 전에 <color=#C8102E>같은 달 패를 하나 더</color> 누르면…",
                          "The pair stays stacked for a moment.\nTap <color=#C8102E>one more of that month</color> before the ring runs out..."));
                Point(A3);
                break;
            case 9:
                Say(Loc.T("<color=#C8102E>쌌다!</color> 세 장이 뭉쳐 <color=#C8102E>뻑 폭탄</color>이 됐어요.\n폭탄 달 패를 누르면 둘레 패까지 펑!\n하지만 지금은 아껴 둬요.",
                          "<color=#C8102E>Stacked!</color> That's a <color=#C8102E>Ppeok bomb</color>.\nTapping that month blasts nearby pairs -\nbut save it for now."));
                PointAt(game.TraySlot(0));
                ShowNext(true);
                break;
            case 10:
                Say(Loc.T("8월 공산으로 한 번 더! 짝 → 셋째 패.", "Again with August: pair, then the third card."));
                Point(B1);
                break;
            case 11:
                Say(Loc.T("짝!", "Snap!"));
                Point(B2);
                break;
            case 12:
                Say(Loc.T("고리가 줄기 전에 셋째 패!", "The third card, before the ring runs out!"));
                Point(B3);
                break;
            case 13:
                Say(Loc.T("마지막! 12월 비로 한 번 더.", "Last one! December this time."));
                Point(C1);
                break;
            case 14:
                Say(Loc.T("짝!", "Snap!"));
                Point(C2);
                break;
            case 15:
                Say(Loc.T("셋째 패!", "The third card!"));
                Point(C3);
                break;
            // ④ 싹쓸이
            case 16:
                Say(Loc.T("뻑 폭탄이 셋! 판에 폭탄 달 패만 남으면\n저절로 펑! 펑! 펑! 터져요.\n뻑 폭탄 셋으로 판을 비우면 <color=#C8102E>싹쓸이</color>!",
                          "Three bombs! When only bomb cards are left,\nthey blow up on their own.\nClear the board with three bombs: <color=#C8102E>Sweep</color>!"));
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
        ring.rectTransform.sizeDelta = Vector2.one * size * (1f + 0.08f * Mathf.Sin(time * 6f));
        arrow.anchoredPosition = new Vector2(0f, size * 0.5f + 40f + 14f * Mathf.Sin(time * 6f));
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
