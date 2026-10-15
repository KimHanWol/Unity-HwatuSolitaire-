using System;
using HwatuSolitaire.Core;
using UnityEngine;

/// <summary>처음 보는 규칙을 그 스테이지에서 한 번씩 알려 주는 창, 설정·크레딧 창.</summary>
public static class Tips
{
    public static void ShowOnce(string key, Action show)
    {
        string k = "Tip_" + key;
        if (PlayerPrefs.GetInt(k, 0) == 1) return;
        PlayerPrefs.SetInt(k, 1);
        PlayerPrefs.Save();
        // 패가 깔리는 연출이 끝난 뒤에
        Anim.Delay(App.I, 0.7f, show);
    }

    /// <summary>카드 그림 두 장을 나란히 (가운데에 기호).</summary>
    static void Pair(Popup p, int a, int b, string mid, float y)
    {
        float w = 150f, h = w * Art.CardAspect;
        var ia = Ui.Image(p.Panel, Art.Card(a), Color.white, "A");
        Ui.Place(ia.rectTransform, 0.5f, 1f, new Vector2(w, h), new Vector2(-130f, -y));
        var ib = Ui.Image(p.Panel, Art.Card(b), Color.white, "B");
        Ui.Place(ib.rectTransform, 0.5f, 1f, new Vector2(w, h), new Vector2(130f, -y));
        var t = Ui.Text(p.Panel, mid, 90, Art.Red, TextAnchor.MiddleCenter, "Mid");
        Ui.Place(t.rectTransform, 0.5f, 1f, new Vector2(120f, 120f), new Vector2(0f, -y));
    }

    public static void ShowRules()
    {
        var p = Popup.Open(new Vector2(920f, 1320f), Loc.T("이렇게 해요", "How to play"), true);
        Pair(p, 0, 3, "=", 300f);
        p.Label(Loc.T("같은 달(같은 꽃) 화투끼리 짝이에요", "Cards of the same month (flower) make a pair"), 46, Art.Ink, 470f, 80f);
        p.Label(Loc.T(
            "두 장을 차례로 누르면 \"짝!\" 하고 사라져요.\n단, 두 장을 잇는 선이 <color=#C8102E>두 번까지만 꺾여야</color> 하고\n중간에 다른 패가 막고 있으면 안 돼요.\n판 바깥 테두리로 돌아가는 건 괜찮아요.",
            "Tap two cards to snap them away.\nThe line joining them may turn <color=#C8102E>at most twice</color>\nand must not cross other cards.\nGoing around the outside edge is fine."),
            40, Art.GreyDark, 680f, 300f);
        p.Label(Loc.T("빨리 깰수록 광을 더 받아요. 3광에 도전!", "Clear fast to collect up to 3 Gwang (光)!"), 44, Art.Red, 900f, 80f);
        p.Button(Loc.T("알겠어요!", "Got it!"), Art.IconCheck, Art.Green, Art.GreenDark, 1050f, () => p.Close(), 0f, 0f, 140f);
        p.Button(Loc.T("연속·폭탄 규칙", "Combos & Bombs"), Art.IconBomb, Art.Red, Art.RedDark, 1200f, () => { p.Close(); ShowCombo(); }, 0f, 0f, 120f);
    }

    public static void ShowCombo()
    {
        var p = Popup.Open(new Vector2(920f, 1180f), Loc.T("쌌다! 그리고 펑!", "Stack & Boom!"), true);
        var bomb = Ui.Image(p.Panel, Art.IconBomb, Color.white, "Bomb");
        Ui.Place(bomb.rectTransform, 0.5f, 1f, new Vector2(200f, 200f), new Vector2(0f, -240f));
        p.Label(Loc.T(
            "짝을 맞추면 두 장이 잠깐 <color=#C8102E>포개져</color> 있어요.\n(둘레의 금빛 고리가 줄어드는 동안)\n\n그 사이에 <color=#C8102E>같은 달 패를 하나 더</color> 누르면\n세 장이 뭉쳐 \"쌌다!\" — 폭탄 칸으로 가요. (최대 3개)\n\n폭탄 달의 빛나는 패를 하나 누르면\n그 달 패가 한꺼번에 <color=#C8102E>펑!</color> 터져요.",
            "After a pair, the two cards <color=#C8102E>stay stacked</color> briefly\n(while the gold ring runs down).\n\nTap <color=#C8102E>one more card of that month</color> in time and\nthe three clump up - \"Stacked!\" - into the bomb tray (max 3).\n\nTap any glowing card of a bomb month and\nall of that month go <color=#C8102E>BOOM</color> at once!"),
            40, Art.GreyDark, 640f, 520f);
        p.Button(Loc.T("알겠어요!", "Got it!"), Art.IconCheck, Art.Green, Art.GreenDark, 1040f, () => p.Close(), 0f, 0f, 140f);
    }

    public static void ShowGravity(GravityMode mode)
    {
        string dir;
        switch (mode)
        {
            case GravityMode.Down: dir = Loc.T("아래로", "down"); break;
            case GravityMode.Up: dir = Loc.T("위로", "up"); break;
            case GravityMode.Left: dir = Loc.T("왼쪽으로", "left"); break;
            case GravityMode.Right: dir = Loc.T("오른쪽으로", "right"); break;
            case GravityMode.SplitOut: dir = Loc.T("양옆 바깥으로", "out to both sides"); break;
            case GravityMode.SplitIn: dir = Loc.T("가운데로", "into the middle"); break;
            default: dir = Loc.T("위아래 바깥으로", "out to top and bottom"); break;
        }
        var p = Popup.Open(new Vector2(900f, 760f), Loc.T("패가 움직여요!", "Cards slide!"), true);
        p.Label(Loc.T(
            "이 판은 짝을 맞출 때마다 남은 패가\n<color=#C8102E>" + dir + "</color> 쏠려요.\n움직인 뒤의 모양도 생각하며 맞춰 보세요.\n\n화면 위에 쏠리는 방향이 표시돼요.",
            "In this board, remaining cards slide\n<color=#C8102E>" + dir + "</color> after each pair.\nThink about where they will land!\n\nThe direction is shown at the top."),
            42, Art.GreyDark, 360f, 380f);
        p.Button(Loc.T("알겠어요!", "Got it!"), Art.IconCheck, Art.Green, Art.GreenDark, 640f, () => p.Close(), 0f, 0f, 130f);
    }

    public static void ShowBlocks()
    {
        var p = Popup.Open(new Vector2(900f, 820f), Loc.T("엎어진 패", "Face-down cards"), true);
        var back = Ui.Image(p.Panel, Art.Back, new Color(0.78f, 0.72f, 0.72f), "Back");
        Ui.Place(back.rectTransform, 0.5f, 1f, new Vector2(130f, 130f * Art.CardAspect), new Vector2(0f, -260f));
        p.Label(Loc.T(
            "엎어진 패는 없앨 수 없고\n선이 지나갈 수도 없어요.\n돌아가는 길을 찾아 보세요!",
            "Face-down cards can't be removed\nand lines can't pass through them.\nFind a way around!"),
            42, Art.GreyDark, 530f, 220f);
        p.Button(Loc.T("알겠어요!", "Got it!"), Art.IconCheck, Art.Green, Art.GreenDark, 710f, () => p.Close(), 0f, 0f, 130f);
    }

    /// <summary>설정 창 (타이틀에서). onLanguage: 언어를 바꿨을 때 화면을 다시 그리라고 알린다.</summary>
    public static void ShowSettings(Action onLanguage)
    {
        var p = Popup.Open(new Vector2(880f, 1280f), Loc.T("설정", "Settings"), true);
        p.Toggle(Loc.T("효과음", "Sound"), 190f, () => Settings.SoundOn, v => Settings.SoundOn = v);
        p.Toggle(Loc.T("배경음", "Music"), 310f, () => Settings.MusicOn, v => { Settings.MusicOn = v; Sfx.RefreshMusic(); });
        p.Toggle(Loc.T("진동", "Vibration"), 430f, () => Settings.VibrationOn, v => Settings.VibrationOn = v);
        p.Toggle(Loc.T("월 숫자 표시", "Month numbers"), 550f, () => Settings.ShowMonth, v => Settings.ShowMonth = v);
        p.Button(Loc.T("언어: 한국어", "Language: English"), null, Art.Blue, Art.BlueDark, 690f, () =>
        {
            Loc.Toggle();
            p.Close(true);
            onLanguage?.Invoke();
        }, 0f, 0f, 120f);
        p.Button(Loc.T("규칙 보기", "How to play"), null, Art.Wood, Art.WoodDark, 830f, () => { p.Close(); ShowRules(); }, 0f, 0f, 120f);
        p.Button(Loc.T("만든 사람 · 저작권", "Credits"), null, Art.Wood, Art.WoodDark, 970f, () => { p.Close(); ShowCredits(); }, 0f, 0f, 120f);
        p.Button(Loc.T("닫기", "Close"), Art.IconClose, Art.Red, Art.RedDark, 1130f, () => p.Close(), 0f, 0f, 130f);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        var dev = Ui.Button(p.Panel, new Vector2(220f, 70f), Art.Grey, Art.GreyDark, "DEV: unlock", null, () =>
        {
            Progress.UnlockAll();
            p.Close();
        }, 30);
        Ui.Place((RectTransform)dev.transform, 0f, 0f, new Vector2(220f, 70f), new Vector2(150f, -50f));
#endif
    }

    public static void ShowCredits()
    {
        var p = Popup.Open(new Vector2(920f, 1180f), Loc.T("만든 사람 · 저작권", "Credits"), true);
        p.Label(App.Title, 56, Art.Ink, 200f, 80f);
        p.Label(Loc.T(
            "<b>화투 그림</b>\nWikimedia Commons \"SVG Hwatu\"\nSpenĉjo, Marcus Richert, Louie Mantia Jr.\nCC BY-SA 4.0 (크기만 바꿔 사용)\ncreativecommons.org/licenses/by-sa/4.0\n\n<b>글꼴</b>\n주아체 (Woowa Brothers) · SIL OFL 1.1\n\n<b>소리·음악·화투 뒷면</b>\n직접 만듦 (코드 합성)",
            "<b>Card art</b>\nWikimedia Commons \"SVG Hwatu\"\nSpenĉjo, Marcus Richert, Louie Mantia Jr.\nCC BY-SA 4.0 (resized)\ncreativecommons.org/licenses/by-sa/4.0\n\n<b>Font</b>\nJua (Woowa Brothers) · SIL OFL 1.1\n\n<b>Sounds, music, card back</b>\nOriginal (synthesized in code)"),
            38, Art.GreyDark, 600f, 720f);
        p.Button(Loc.T("닫기", "Close"), Art.IconClose, Art.Red, Art.RedDark, 1060f, () => p.Close(), 0f, 0f, 130f);
    }
}
