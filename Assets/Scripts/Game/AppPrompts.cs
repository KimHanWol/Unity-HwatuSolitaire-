using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 앱 차원의 안내 창 두 가지.
/// - 리뷰 요청: 어느 정도 즐긴 뒤(접속 3회 이상, 처음 켠 지 2일 이상, 8스테이지 이상 클리어) 스테이지 목록에서 한 번
///   "게임을 재밌게 즐기고 계신가요?"를 묻고, [리뷰 남기기]를 누르면 Play 스토어의 앱 페이지를 연다.
///   [나중에]는 5일 뒤 다시, [괜찮아요]·[리뷰 남기기]는 다시 묻지 않는다.
///   (어느 쪽을 골라도 똑같이 스토어로 보내지 않는 "골라 받기"는 하지 않는다 — 스토어 정책상 좋은 리뷰만 유도하는 건 피한다.)
/// - 업데이트 안내: 타이틀에서 GitHub Pages 의 docs/app.json 을 읽어 지금 버전(Application.version)보다 새 버전이 있으면 알린다.
///   minVersion 보다 낮으면 닫을 수 없는 창(업데이트만 가능), latestVersion 보다만 낮으면 [업데이트]/[나중에] (켤 때마다 한 번).
///   새 버전을 스토어에 올려 심사가 끝난 뒤 docs/app.json 의 latestVersion 을 올려서 푸시한다.
/// </summary>
public static class AppPrompts
{
    const string VersionUrl = "https://kimhanwol.github.io/Unity-HwatuSolitaire-/app.json";

    const string LaunchKey = "LaunchCount";
    const string FirstDayKey = "FirstLaunchDay";
    const string ReviewDoneKey = "ReviewDone";
    const string ReviewSnoozeKey = "ReviewSnoozeDay";

    const int ReviewMinLaunches = 3, ReviewMinDays = 2, ReviewMinCleared = 8, ReviewSnoozeDays = 5;

    static bool updateChecked, reviewShownThisRun;

    static int Today => (int)(DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalDays;

    /// <summary>앱을 켤 때 한 번: 접속 횟수와 처음 켠 날을 센다.</summary>
    public static void CountLaunch()
    {
        PlayerPrefs.SetInt(LaunchKey, PlayerPrefs.GetInt(LaunchKey, 0) + 1);
        if (!PlayerPrefs.HasKey(FirstDayKey)) PlayerPrefs.SetInt(FirstDayKey, Today);
        PlayerPrefs.Save();
    }

    /// <summary>Play 스토어의 이 앱 페이지 (기기에서는 스토어 앱, 그 밖에는 웹).</summary>
    public static void OpenStorePage()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.OpenURL("market://details?id=" + Application.identifier);
#else
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
#endif
    }

    // ------------------------------------------------------------ 리뷰 요청
    /// <summary>스테이지 목록을 열 때 부른다. 조건이 맞으면 잠시 뒤 리뷰 요청 창을 띄운다.</summary>
    public static void MaybeAskReview(MonoBehaviour host)
    {
        if (reviewShownThisRun || PlayerPrefs.GetInt(ReviewDoneKey, 0) == 1) return;
        if (PlayerPrefs.GetInt(LaunchKey, 0) < ReviewMinLaunches) return;
        if (Today - PlayerPrefs.GetInt(FirstDayKey, Today) < ReviewMinDays) return;
        if (Today < PlayerPrefs.GetInt(ReviewSnoozeKey, 0)) return;
        if (Progress.ClearedCount() < ReviewMinCleared) return;
        reviewShownThisRun = true;
        Anim.Delay(host, 0.6f, () =>
        {
            if (host == null || App.I.PopupOpen) return;
            ShowReview();
        });
    }

    static void ShowReview()
    {
        var p = Popup.Open(new Vector2(860f, 900f), Loc.T("즐거우신가요?", "Enjoying the game?"));
        var coin = Ui.Image(p.Panel, Art.Gwang, Color.white, "Gwang");
        Ui.Place(coin.rectTransform, 0.5f, 1f, new Vector2(150f, 150f), new Vector2(0f, -230f));
        Anim.Pop(coin.transform, 0.4f, 0.3f);
        p.Label(Loc.T("화투 사천성을 재밌게 즐기고 계신가요?\n별점과 리뷰를 남겨 주시면\n더 좋은 게임을 만드는 데 큰 힘이 돼요!",
                      "Are you enjoying Hwatu Solitaire?\nA rating or review would help us\nmake the game even better!"), 40, Art.Ink, 410f, 200f);
        p.Button(Loc.T("리뷰 남기기", "Rate the game"), null, Art.Green, Art.GreenDark, 600f, () =>
        {
            PlayerPrefs.SetInt(ReviewDoneKey, 1);
            PlayerPrefs.Save();
            p.Close();
            OpenStorePage();
        }, 0f, 0f, 130f);
        float half = 330f;
        p.Button(Loc.T("나중에", "Later"), null, Art.Wood, Art.WoodDark, 760f, () =>
        {
            PlayerPrefs.SetInt(ReviewSnoozeKey, Today + ReviewSnoozeDays);
            PlayerPrefs.Save();
            p.Close();
        }, half, -(half + 30f) / 2f, 110f);
        p.Button(Loc.T("괜찮아요", "No thanks"), null, Art.Grey, Art.GreyDark, 760f, () =>
        {
            PlayerPrefs.SetInt(ReviewDoneKey, 1);
            PlayerPrefs.Save();
            p.Close();
        }, half, (half + 30f) / 2f, 110f);
        // 바깥을 누르거나 뒤로 가기는 "나중에"와 같게
        p.OnBackPressed = () =>
        {
            PlayerPrefs.SetInt(ReviewSnoozeKey, Today + ReviewSnoozeDays);
            PlayerPrefs.Save();
            p.Close();
        };
    }

    // ------------------------------------------------------------ 업데이트 안내
    [Serializable]
    class VersionInfo
    {
        public string latestVersion = "";
        public string minVersion = "";
    }

    /// <summary>타이틀에서 부른다. 켤 때마다 한 번만 확인한다. 인터넷이 없거나 파일을 못 읽으면 조용히 넘어간다.</summary>
    public static void CheckUpdate(MonoBehaviour host)
    {
        if (updateChecked) return;
        updateChecked = true;
        host.StartCoroutine(FetchVersion(host));
    }

    static IEnumerator FetchVersion(MonoBehaviour host)
    {
        // GitHub Pages 캐시를 피하려고 시각을 붙인다
        using (var req = UnityWebRequest.Get(VersionUrl + "?t=" + DateTime.UtcNow.Ticks))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            VersionInfo info;
            try { info = JsonUtility.FromJson<VersionInfo>(req.downloadHandler.text); }
            catch (Exception) { yield break; }
            if (info == null || host == null) yield break;
            string current = Application.version;
            bool forced = Compare(current, info.minVersion) < 0;
            bool newer = Compare(current, info.latestVersion) < 0;
            if (forced || newer) ShowUpdate(forced);
        }
    }

    static void ShowUpdate(bool forced)
    {
        var p = Popup.Open(new Vector2(860f, forced ? 640f : 760f), Loc.T("업데이트", "Update"));
        p.CanClose = !forced;
        p.Label(forced
            ? Loc.T("새 버전이 나왔어요!\n계속 즐기려면 업데이트가 필요해요.", "A new version is out!\nPlease update to keep playing.")
            : Loc.T("새 버전이 나왔어요!\n업데이트하고 더 좋아진 게임을 즐겨 보세요.", "A new version is out!\nUpdate to enjoy the latest improvements."),
            42, Art.Ink, 250f, 160f);
        p.Button(Loc.T("업데이트", "Update"), null, Art.Green, Art.GreenDark, 450f, OpenStorePage, 0f, 0f, 130f);
        if (forced)
        {
            p.OnBackPressed = () => { };
            return;
        }
        p.Button(Loc.T("나중에", "Later"), null, Art.Wood, Art.WoodDark, 610f, () => p.Close(), 0f, 0f, 110f);
    }

    /// <summary>"1.2.10" 같은 버전을 숫자 단위로 비교한다 (a &lt; b 이면 음수). 빈 문자열은 0.</summary>
    static int Compare(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(b)) return 1;
        string[] x = (a ?? "").Split('.'), y = b.Split('.');
        for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            int u = i < x.Length && int.TryParse(x[i], out int p) ? p : 0;
            int v = i < y.Length && int.TryParse(y[i], out int q) ? q : 0;
            if (u != v) return u.CompareTo(v);
        }
        return 0;
    }
}
