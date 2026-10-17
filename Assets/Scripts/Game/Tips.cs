using System;
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
            "패가 층층이 쌓여 있어요.\n<color=#C8102E>위에 덮인 패가 없는 패</color>만 고를 수 있어요.\n덮인 패는 조금 어둡게 보여요.\n위 패를 치우면 아래 패가 열려요.",
            "Cards are stacked in layers.\nOnly cards with <color=#C8102E>nothing on top</color> can be picked.\nCovered cards look a little darker.\nClear the top ones to open the cards below."),
            40, Art.GreyDark, 680f, 300f);
        p.Label(Loc.T("빨리 깰수록 광을 더 받아요. 3광에 도전!", "Clear fast to collect up to 3 Gwang (光)!"), 44, Art.Red, 900f, 80f);
        p.Button(Loc.T("알겠어요!", "Got it!"), Art.IconCheck, Art.Green, Art.GreenDark, 1050f, () => p.Close(), 0f, 0f, 140f);
        p.Button(Loc.T("뻑·폭탄 규칙", "Ppeok & Bomb rules"), Art.IconBomb, Art.Red, Art.RedDark, 1200f, () => { p.Close(); ShowCombo(); }, 0f, 0f, 120f);
    }

    public static void ShowCombo()
    {
        var p = Popup.Open(new Vector2(920f, 1340f), Loc.T("쌌다! 뻑 폭탄", "Ppeok & Boom!"), true);
        var bomb = Ui.Image(p.Panel, Art.IconBomb, Color.white, "Bomb");
        Ui.Place(bomb.rectTransform, 0.5f, 1f, new Vector2(200f, 200f), new Vector2(0f, -240f));
        p.Label(Loc.T(
            "짝을 맞추면 두 장이 잠깐 <color=#C8102E>포개져</color> 있어요.\n(둘레의 금빛 고리가 줄어드는 동안)\n\n그 사이에 <color=#C8102E>같은 달 패를 하나 더</color> 누르면\n세 장이 뭉쳐 \"쌌다!\" — <color=#C8102E>뻑</color> 폭탄이 돼요. (최대 3개)\n\n폭탄 달의 빛나는 패를 하나 누르면 그 패와\n둘레의 짝 여러 쌍이 한꺼번에 <color=#C8102E>펑!</color> 터져요.\n\n폭탄 달 패만 남으면 저절로 펑! 펑!\n뻑 폭탄 셋으로 판을 비우면 <color=#C8102E>싹쓸이</color> → 황금 광!",
            "After a pair, the two cards <color=#C8102E>stay stacked</color> briefly\n(while the gold ring runs down).\n\nTap <color=#C8102E>one more card of that month</color> in time and\nthe three clump up - a <color=#C8102E>Ppeok</color> bomb (max 3).\n\nTap a glowing card of a bomb month: it and\nseveral nearby pairs go <color=#C8102E>BOOM</color> at once!\n\nIf only bomb cards are left, they blow up on their own.\nClear the board with three bombs for a <color=#C8102E>Sweep</color> - Golden Gwang!"),
            38, Art.GreyDark, 700f, 640f);
        p.Button(Loc.T("알겠어요!", "Got it!"), Art.IconCheck, Art.Green, Art.GreenDark, 1210f, () => p.Close(), 0f, 0f, 140f);
    }

    /// <summary>설정 창 (타이틀에서). onLanguage: 언어를 바꿨을 때 화면을 다시 그리라고 알린다.</summary>
    public static void ShowSettings(Action onLanguage)
    {
        // 광고 동의가 필요한 지역(EEA 등)에서는 동의를 다시 고르는 버튼을 하나 더 둔다.
        bool privacy = Ads.PrivacyOptionsRequired;
        float extra = privacy ? 140f : 0f;
        var p = Popup.Open(new Vector2(880f, 1420f + extra), Loc.T("설정", "Settings"), true);
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
        p.Button(Loc.T("튜토리얼 다시 하기", "Replay tutorial"), Art.IconPlay, Art.Green, Art.GreenDark, 830f, () => { p.Close(true); App.I.PlayTutorial(); }, 0f, 0f, 120f);
        p.Button(Loc.T("규칙 보기", "How to play"), null, Art.Wood, Art.WoodDark, 970f, () => { p.Close(); ShowRules(); }, 0f, 0f, 120f);
        p.Button(Loc.T("만든 사람 · 저작권", "Credits"), null, Art.Wood, Art.WoodDark, 1110f, () => { p.Close(); ShowCredits(); }, 0f, 0f, 120f);
        if (privacy)
            p.Button(Loc.T("광고 개인정보 설정", "Ad privacy options"), null, Art.Wood, Art.WoodDark, 1250f, () => { p.Close(); Ads.ShowPrivacyOptions(); }, 0f, 0f, 120f);
        p.Button(Loc.T("닫기", "Close"), Art.IconClose, Art.Red, Art.RedDark, 1270f + extra, () => p.Close(), 0f, 0f, 130f);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        var dev = Ui.Button(p.Panel, new Vector2(220f, 70f), Art.Grey, Art.GreyDark, "DEV: unlock", null, () =>
        {
            Progress.UnlockAll();
            p.Close();
        }, 30);
        Ui.Place((RectTransform)dev.transform, 0f, 0f, new Vector2(220f, 70f), new Vector2(150f, -50f));
        // 개발용 초기화: 진행·기록·황금 광·튜토리얼·처음 안내를 모두 지우고 처음 실행한 상태로 (설정·언어는 남긴다)
        var reset = Ui.Button(p.Panel, new Vector2(220f, 70f), Art.Red, Art.RedDark, "DEV: 초기화", null, () =>
        {
            Progress.ResetEverything();
            p.Close(true);
            App.I.ShowTitle();
        }, 30);
        Ui.Place((RectTransform)reset.transform, 1f, 0f, new Vector2(220f, 70f), new Vector2(-150f, -50f));
#endif
    }

    public static void ShowCredits()
    {
        var p = Popup.Open(new Vector2(920f, 1180f), Loc.T("만든 사람 · 저작권", "Credits"), true);
        p.Label(App.Title, 56, Art.Ink, 200f, 80f);
        p.Label(Loc.T(
            "<b>화투 그림</b>\nWikimedia Commons \"SVG Hwatu\"\nSpenĉjo, Marcus Richert, Louie Mantia Jr.\nCC BY-SA 4.0 (크기만 바꿔 사용)\ncreativecommons.org/licenses/by-sa/4.0\n\n<b>글꼴</b>\n주아체 (Woowa Brothers) · SIL OFL 1.1\n\n<b>효과음</b>\n짝·쌌다: Freesound 뺨 치는 소리 (CC0) · 패 소리: Kenney (CC0)\n그 밖의 소리·음악·화투 뒷면: 직접 만듦",
            "<b>Card art</b>\nWikimedia Commons \"SVG Hwatu\"\nSpenĉjo, Marcus Richert, Louie Mantia Jr.\nCC BY-SA 4.0 (resized)\ncreativecommons.org/licenses/by-sa/4.0\n\n<b>Font</b>\nJua (Woowa Brothers) · SIL OFL 1.1\n\n<b>Sounds</b>\nSnaps: Freesound slaps (CC0) · Card sounds: Kenney (CC0)\nOther sounds, music, card back: original"),
            38, Art.GreyDark, 600f, 720f);
        p.Button(Loc.T("닫기", "Close"), Art.IconClose, Art.Red, Art.RedDark, 1060f, () => p.Close(), 0f, 0f, 130f);
    }
}
