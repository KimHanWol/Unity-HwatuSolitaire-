using System.Text;
using HwatuSolitaire.Core;
using UnityEngine;

/// <summary>
/// 스테이지 진행(광), 가장 빠른 기록, 황금 광. PlayerPrefs에 저장한다.
/// 별은 스테이지 순서대로 '0'~'3' 글자 300개짜리 문자열 하나로 둔다.
/// </summary>
public static class Progress
{
    const string StarsKey = "Stars";
    const string BestKey = "BestTimes";
    const string LastKey = "LastStage";
    const string GoldenKey = "Golden";


    static char[] stars;
    static int[] best;

    static void Load()
    {
        if (stars != null) return;
        int n = StageGenerator.StageCount;
        stars = new char[n];
        best = new int[n];
        string s = PlayerPrefs.GetString(StarsKey, "");
        for (int i = 0; i < n; i++) stars[i] = i < s.Length && s[i] >= '0' && s[i] <= '3' ? s[i] : '0';
        string[] b = PlayerPrefs.GetString(BestKey, "").Split(',');
        for (int i = 0; i < n; i++) best[i] = i < b.Length && int.TryParse(b[i], out int v) ? v : 0;
    }

    /// <summary>얻은 별 수 (0이면 아직 못 깸).</summary>
    public static int Stars(int stage)
    {
        Load();
        return stage >= 1 && stage <= stars.Length ? stars[stage - 1] - '0' : 0;
    }

    /// <summary>가장 빨리 깬 시간(초). 0이면 기록 없음.</summary>
    public static int BestSeconds(int stage)
    {
        Load();
        return stage >= 1 && stage <= best.Length ? best[stage - 1] : 0;
    }

    public static bool Unlocked(int stage) => stage == 1 || Stars(stage - 1) > 0 || Stars(stage) > 0;

    /// <summary>깬 결과를 저장한다. 별과 기록은 더 좋은 쪽만 남긴다. 새 기록이면 true.</summary>
    public static bool Record(int stage, int starCount, int seconds)
    {
        Load();
        int i = stage - 1;
        if (starCount > stars[i] - '0') stars[i] = (char)('0' + starCount);
        bool newBest = best[i] == 0 || seconds < best[i];
        if (newBest) best[i] = seconds;
        PlayerPrefs.SetString(StarsKey, new string(stars));
        var sb = new StringBuilder();
        for (int k = 0; k < best.Length; k++)
        {
            if (k > 0) sb.Append(',');
            if (best[k] > 0) sb.Append(best[k]);
        }
        PlayerPrefs.SetString(BestKey, sb.ToString());
        PlayerPrefs.Save();
        return newBest;
    }

    public static int TotalStars
    {
        get
        {
            Load();
            int t = 0;
            foreach (char c in stars) t += c - '0';
            return t;
        }
    }

    public static int ChapterStars(int chapter)
    {
        int t = 0;
        for (int k = 1; k <= StageGenerator.StagesPerChapter; k++) t += Stars(chapter * StageGenerator.StagesPerChapter + k);
        return t;
    }

    /// <summary>아직 깨지 않은 첫 스테이지 (모두 깼으면 마지막).</summary>
    public static int Frontier
    {
        get
        {
            for (int s = 1; s <= StageGenerator.StageCount; s++) if (Stars(s) == 0) return s;
            return StageGenerator.StageCount;
        }
    }

    public static int LastPlayed
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(LastKey, 1), 1, StageGenerator.StageCount);
        set { PlayerPrefs.SetInt(LastKey, value); PlayerPrefs.Save(); }
    }

    /// <summary>싹쓸이로 깬 적이 있으면 황금 광 (광 개수와는 따로).</summary>
    public static bool Golden(int stage)
    {
        string g = PlayerPrefs.GetString(GoldenKey, "");
        return stage >= 1 && stage <= g.Length && g[stage - 1] == '1';
    }

    public static void SetGolden(int stage)
    {
        var g = PlayerPrefs.GetString(GoldenKey, "").PadRight(StageGenerator.StageCount, '0').ToCharArray();
        if (stage < 1 || stage > g.Length) return;
        g[stage - 1] = '1';
        PlayerPrefs.SetString(GoldenKey, new string(g));
        PlayerPrefs.Save();
    }

    public static int TotalGolden
    {
        get
        {
            int n = 0;
            foreach (char c in PlayerPrefs.GetString(GoldenKey, "")) if (c == '1') n++;
            return n;
        }
    }

    /// <summary>개발용: 모든 스테이지를 연다.</summary>
    public static void UnlockAll()
    {
        Load();
        for (int i = 0; i < stars.Length; i++) if (stars[i] == '0') stars[i] = '1';
        PlayerPrefs.SetString(StarsKey, new string(stars));
        PlayerPrefs.Save();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(StarsKey);
        PlayerPrefs.DeleteKey(BestKey);
        PlayerPrefs.DeleteKey(LastKey);
        PlayerPrefs.DeleteKey(GoldenKey);
        PlayerPrefs.Save();
        stars = null;
        best = null;
    }
}
