using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/AdConfig.json에 적은 AdMob 앱 ID를 Google Mobile Ads 플러그인 설정으로 옮긴다.
/// (Assets > Google Mobile Ads > Settings 화면에 직접 넣는 것과 같다.)
/// 앱 ID가 어디에도 없으면 구글의 공개 테스트용 앱 ID를 채운다. 앱 ID가 비면 안드로이드 앱이 실행 직후 종료되기 때문이다.
/// </summary>
public class AdsSetup : AssetPostprocessor
{
    const string ConfigPath = "Assets/Resources/AdConfig.json";
    const string SampleAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
    const string SampleIosAppId = "ca-app-pub-3940256099942544~1458002511";

    [Serializable]
    class Config
    {
        public Platform android = new Platform();
        public Platform ios = new Platform();
    }

    [Serializable]
    class Platform
    {
        public string appId = "";
    }

    [InitializeOnLoadMethod]
    static void Schedule() => EditorApplication.delayCall += Sync;

    // AdConfig.json을 고쳐 저장하면 바로 반영한다.
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (Array.IndexOf(imported, ConfigPath) >= 0) EditorApplication.delayCall += Sync;
    }

    [MenuItem("Tools/Hwatu Solitaire/Sync AdMob App IDs")]
    public static void Sync()
    {
        // 플러그인 안쪽 클래스라 리플렉션으로 접근한다. 플러그인이 없으면 아무 일도 하지 않는다.
        Type type = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");
            if (type != null) break;
        }
        var load = type?.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (load == null) return;

        var settings = load.Invoke(null, null) as ScriptableObject;
        if (settings == null) return;

        var config = new Config();
        if (File.Exists(ConfigPath))
        {
            try { JsonUtility.FromJsonOverwrite(File.ReadAllText(ConfigPath), config); }
            catch (Exception e) { Debug.LogError("[AdsSetup] AdConfig.json을 읽지 못했습니다: " + e.Message); }
        }

        var so = new SerializedObject(settings);
        bool changed = Apply(so.FindProperty("adMobAndroidAppId"), config.android.appId, SampleAndroidAppId, "Android");
        changed |= Apply(so.FindProperty("adMobIOSAppId"), config.ios.appId, SampleIosAppId, "iOS");
        if (!changed) return;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    static bool Apply(SerializedProperty property, string wanted, string sample, string label)
    {
        if (property == null) return false;
        wanted = wanted == null ? "" : wanted.Trim();

        if (wanted.Length > 0)
        {
            // 앱 ID는 물결표(~)가 들어간다. 슬래시(/)가 들어간 건 광고 단위 ID라 여기 넣으면 안 된다.
            if (!wanted.StartsWith("ca-app-pub-") || !wanted.Contains("~"))
            {
                Debug.LogError("[AdsSetup] AdConfig.json의 " + label + " appId 형식이 다릅니다 (ca-app-pub-숫자~숫자): " + wanted);
                return false;
            }
            if (property.stringValue == wanted) return false;
            property.stringValue = wanted;
            Debug.Log("[AdsSetup] " + label + " 앱 ID를 AdConfig.json 값으로 설정했습니다.");
            return true;
        }

        if (!string.IsNullOrEmpty(property.stringValue)) return false;
        property.stringValue = sample;
        Debug.Log("[AdsSetup] " + label + " 앱 ID가 비어 있어 테스트용 ID를 넣었습니다. 출시 전에 AdConfig.json의 appId를 채우세요.");
        return true;
    }
}
