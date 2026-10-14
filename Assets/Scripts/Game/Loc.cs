using UnityEngine;

/// <summary>
/// 한국어 / 영어 전환. 처음에는 기기 언어를 따르고(한국어가 아니면 영어), 설정에서 바꾸면 그 선택을 기억한다.
/// 글자는 쓰는 자리에서 Loc.T("한국어", "English")로 두 언어를 함께 적는다.
/// </summary>
public static class Loc
{
    const string Key = "Language";
    const string KoreanCode = "ko", EnglishCode = "en";

    static bool loaded;
    static bool korean;

    public static bool Korean
    {
        get
        {
            if (!loaded)
            {
                loaded = true;
                string saved = PlayerPrefs.GetString(Key, "");
                korean = saved == "" ? Application.systemLanguage == SystemLanguage.Korean : saved == KoreanCode;
            }
            return korean;
        }
    }

    /// <summary>설정 창의 언어 버튼에 적는, 지금 언어의 이름.</summary>
    public static string CurrentName => Korean ? "한국어" : "English";

    public static string T(string ko, string en) => Korean ? ko : en;

    public static void Toggle() => Set(!Korean);

    public static void Set(bool useKorean)
    {
        loaded = true;
        korean = useKorean;
        PlayerPrefs.SetString(Key, useKorean ? KoreanCode : EnglishCode);
        PlayerPrefs.Save();
    }
}
