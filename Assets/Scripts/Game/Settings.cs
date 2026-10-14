using UnityEngine;

/// <summary>기기에 저장되는 사용자 설정.</summary>
public static class Settings
{
    const string MusicKey = "Music";
    const string SoundKey = "Sound";
    const string VibrationKey = "Vibration";
    const string MonthKey = "ShowMonth";

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set => Set(MusicKey, value);
    }

    public static bool SoundOn
    {
        get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
        set => Set(SoundKey, value);
    }

    public static bool VibrationOn
    {
        get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        set => Set(VibrationKey, value);
    }

    /// <summary>패 귀퉁이에 몇 월 패인지 숫자를 띄운다. 화투가 낯선 사람을 위해 처음에는 켜 둔다.</summary>
    public static bool ShowMonth
    {
        get => PlayerPrefs.GetInt(MonthKey, 1) == 1;
        set => Set(MonthKey, value);
    }

    /// <summary>
    /// 짧고 세기를 조절할 수 있는 진동. Handheld.Vibrate는 길이도 세기도 못 바꿔서 짝을 맞출 때처럼 자주 나는 일에는 쓸 수 없다.
    /// </summary>
    /// <param name="milliseconds">진동 길이</param>
    /// <param name="amplitude">세기 1~255. 세기 조절이 안 되는 기기에서는 무시된다.</param>
    public static void Haptic(int milliseconds, int amplitude)
    {
        if (!VibrationOn) return;
        amplitude = Mathf.Clamp(amplitude, 1, 255);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (hapticFailed) return;
        try
        {
            if (vibrator == null)
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    sdk = version.GetStatic<int>("SDK_INT");
            }

            // 안드로이드 8 미만은 세기 조절이 없다.
            if (sdk >= 26)
            {
                using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude))
                    vibrator.Call("vibrate", effect);
            }
            else vibrator.Call("vibrate", (long)milliseconds);
        }
        catch (System.Exception e)
        {
            // 진동 때문에 게임이 멈추면 안 된다. 다시 시도하지 않게 꺼 둔다.
            if (!hapticFailed) Debug.LogWarning("[Settings] 짧은 진동을 쓸 수 없습니다: " + e.Message);
            hapticFailed = true;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS는 네이티브 플러그인 없이는 짧은 진동을 낼 수 없어서, 큰 사건에만 기본 진동을 쓴다.
        if (milliseconds >= 80) Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator;
    static int sdk;
    static bool hapticFailed;
#endif

    static void Set(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
