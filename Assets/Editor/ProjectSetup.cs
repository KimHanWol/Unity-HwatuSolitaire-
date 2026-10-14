using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메인 씬 생성, 모바일 플레이어 설정, 빌드 명령. 메뉴 Tools/Hwatu Shisen-Sho 또는 배치 모드 -executeMethod 로 쓴다.
///   Unity -batchmode -quit -projectPath . -executeMethod ProjectSetup.Setup
///   Unity -batchmode -quit -projectPath . -buildTarget Win64 -executeMethod ProjectSetup.BuildWindows
///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod ProjectSetup.BuildTestApk
///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod ProjectSetup.BuildReleaseAab
/// </summary>
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    // 기기 홈 화면에 보이는 이름. 게임 안 제목은 App.Title.
    public const string ProductName = "짝! 화투 사천성";
    const string CompanyName = "LINK_";
    // 스토어에 올린 뒤에는 바꿀 수 없다.
    const string PackageName = "com.kimhanwol.hwatusolitaire";
    // 스토어에 새 빌드를 올릴 때마다 VersionCode를 1씩 올린다.
    const string Version = "1.0.0";
    const int VersionCode = 1;
    const string IconPath = "Assets/Icon/AppIcon.png";

    [InitializeOnLoadMethod]
    static void ApplyOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (PlayerSettings.companyName == "DefaultCompany") ApplyBuildSettings();
            if (!File.Exists(ScenePath)) Setup();
        };
    }

    [MenuItem("Tools/Hwatu Shisen-Sho/Apply Build Settings")]
    public static void ApplyBuildSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageName);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, PackageName);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.Android.bundleVersionCode = VersionCode;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        // PC 확인용 창 크기 (세로 화면)
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 1170;
        PlayerSettings.resizableWindow = true;

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        // 배치 빌드에서는 AdsSetup의 지연 호출이 빌드보다 늦을 수 있어 여기서 확실히 옮긴다 (앱 ID가 비면 앱이 바로 죽는다).
        AdsSetup.Sync();

        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup] build settings applied: " + CompanyName + " / " + PackageName);
    }

    [MenuItem("Tools/Hwatu Shisen-Sho/Setup Scene")]
    public static void Setup()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x0F, 0x4A, 0x2A, 0xFF);
        camGo.AddComponent<AudioListener>();
        new GameObject("App").AddComponent<App>();

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup] scene ready: " + ScenePath);
    }

    /// <summary>화면 확인용 PC 개발 빌드 (-autoshot 으로 스크린샷).</summary>
    [MenuItem("Tools/Hwatu Shisen-Sho/Build Windows (dev)")]
    public static void BuildWindows()
    {
        ApplyBuildSettings();
        if (!File.Exists(ScenePath)) Setup();
        const string output = "Builds/Windows/HwatuSolitaire.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.StandaloneWindows64, BuildOptions.Development);
        Debug.Log("[ProjectSetup] windows build " + report.summary.result + " errors=" + report.summary.totalErrors);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>기기에 바로 깔아 볼 수 있는 테스트 APK (디버그 키 서명, 스토어에는 못 올림).</summary>
    [MenuItem("Tools/Hwatu Shisen-Sho/Build Test APK")]
    public static void BuildTestApk()
    {
        if (!CheckAndroidTarget()) return;
        ApplyBuildSettings();
        if (!File.Exists(ScenePath)) Setup();
        const string output = "Builds/HwatuSolitaire-test.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.Android, BuildOptions.None);
        Debug.Log("[ProjectSetup] apk " + report.summary.result + " errors=" + report.summary.totalErrors + " -> " + output);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    // 서명 키 정보. 깃에 올라가지 않는 폴더(.gitignore의 /Keystore/)에 둔다.
    // KEYSTORE_PATH=, KEYSTORE_PASSWORD=, KEY_ALIAS=, KEY_PASSWORD= 네 줄.
    const string KeystoreEnvPath = "Keystore/keystore.env";

    static void LoadKeystoreEnv()
    {
        if (!File.Exists(KeystoreEnvPath)) return;
        var values = new System.Collections.Generic.Dictionary<string, string>();
        foreach (string raw in File.ReadAllLines(KeystoreEnvPath))
        {
            string line = raw.Trim();
            int eq = line.IndexOf('=');
            if (line.Length == 0 || line.StartsWith("#") || eq <= 0) continue;
            values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
        }
        if (!values.TryGetValue("KEYSTORE_PATH", out string path) || !File.Exists(path))
        {
            Debug.LogError("[ProjectSetup] keystore.env의 KEYSTORE_PATH에 파일이 없습니다: " + path);
            return;
        }
        values.TryGetValue("KEYSTORE_PASSWORD", out string storePass);
        values.TryGetValue("KEY_ALIAS", out string alias);
        values.TryGetValue("KEY_PASSWORD", out string keyPass);
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = path;
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasName = alias;
        PlayerSettings.Android.keyaliasPass = keyPass;
    }

    /// <summary>플레이 스토어에 올릴 AAB. 서명 키는 Keystore/keystore.env 또는 Publishing Settings.</summary>
    [MenuItem("Tools/Hwatu Shisen-Sho/Build Release AAB (Play Store)")]
    public static void BuildReleaseAab()
    {
        if (!CheckAndroidTarget()) return;
        ApplyBuildSettings();
        LoadKeystoreEnv();
        if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
        {
            Fail("스토어용 빌드는 내 키로 서명해야 합니다. Keystore/keystore.env를 만들거나 Player Settings > Publishing Settings에서 키스토어를 지정하세요.");
            return;
        }
        var config = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/AdConfig.json");
        if (config != null && System.Text.RegularExpressions.Regex.IsMatch(config.text, "\"useTestAds\"\\s*:\\s*true"))
        {
            if (Application.isBatchMode) Debug.LogWarning("[ProjectSetup] AdConfig.json의 useTestAds가 true입니다. 실제 광고가 나오지 않습니다.");
            else if (!EditorUtility.DisplayDialog("테스트 광고가 켜져 있습니다",
                "AdConfig.json의 useTestAds가 true입니다. 이대로 올리면 실제 광고가 나오지 않아 수익이 생기지 않습니다.\n\n그래도 빌드할까요?",
                "그대로 빌드", "취소")) return;
        }
        string output = "Builds/HwatuSolitaire-" + PlayerSettings.bundleVersion + ".aab";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        EditorUserBuildSettings.buildAppBundle = true;
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output, BuildTarget.Android, BuildOptions.None);
        EditorUserBuildSettings.buildAppBundle = false;
        Debug.Log("[ProjectSetup] release build " + report.summary.result + " errors=" + report.summary.totalErrors + " -> " + output);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>
    /// 활성 대상이 Android가 아니면 AdMob 플러그인이 앱 ID를 매니페스트에 넣지 않아 APK가 실행 직후 죽는다.
    /// 배치에서는 반드시 -buildTarget Android 로 실행한다.
    /// </summary>
    static bool CheckAndroidTarget()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) return true;
        Fail("활성 빌드 대상이 Android가 아닙니다. File > Build Profiles 에서 Android로 바꾸거나, 배치 모드면 -buildTarget Android 를 붙이세요.");
        return false;
    }

    static void Fail(string message)
    {
        Debug.LogError("[ProjectSetup] " + message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        else EditorUtility.DisplayDialog("빌드할 수 없습니다", message, "확인");
    }
}
