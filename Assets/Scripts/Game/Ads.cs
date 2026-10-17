// 광고 플러그인은 안드로이드·iOS·에디터에서만 돈다 (PC 확인용 빌드에서는 끈다).
#if ADMOB && (UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR)
#define ADS_ON
#endif
using System;
using System.Collections;
using UnityEngine;
#if ADS_ON
using GoogleMobileAds.Api;
#endif

/// <summary>
/// AdMob 하단 배너 / 전면(다음 스테이지로 넘어갈 때) / 보상형(시간 충전, 부활).
/// 광고 ID와 빈도는 Assets/Resources/AdConfig.json 한 곳에 적는다 (docs/ADS.md).
/// Google Mobile Ads 플러그인(Packages/manifest.json)이 있고 Assets/csc.rsp에 -define:ADMOB이 있어야 실제 광고가 나온다.
/// 에디터에서는 플러그인이 보여주는 가짜 광고 화면이 뜬다. ADMOB이 없으면 에디터에서는 "광고를 다 본 것"으로 바로 처리해
/// 흐름만 시험할 수 있고, 기기 빌드에서는 광고 기능이 꺼진다.
/// </summary>
public class Ads : MonoBehaviour
{
// ADMOB이 꺼져 있으면 쓰이지 않는 필드가 생긴다.
#pragma warning disable 0414, 0649, 0169
    const string ConfigResource = "AdConfig";
    // 구글이 공개한 테스트 광고 ID
#if UNITY_IOS
    const string TestBannerId = "ca-app-pub-3940256099942544/2435281174";
    const string TestRewardedId = "ca-app-pub-3940256099942544/1712485313";
    const string TestInterstitialId = "ca-app-pub-3940256099942544/4411468910";
#else
    const string TestBannerId = "ca-app-pub-3940256099942544/9214589741";
    const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";
    const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
#endif

    [Serializable]
    class Config
    {
        // true면 아래 ID와 상관없이 구글 테스트 광고를 쓴다. 출시할 때 false로 바꾼다.
        public bool useTestAds = true;
        // 여기 적은 기기에서는 실제 광고 단위 ID를 써도 테스트 광고가 나온다 (내 폰으로 출시 설정을 시험할 때).
        public string[] testDeviceIds = new string[0];
        // 이 스테이지를 깨고 다음으로 넘어갈 때부터 전면 광고를 보여준다 (처음 몇 판은 광고 없이).
        public int interstitialFromStage = 3;
        // 전면 광고 사이의 최소 간격(초)
        public float interstitialMinInterval = 60f;
        // 보상형 광고를 본 직후 이 시간(초) 동안은 전면 광고를 건너뛴다.
        public float quietAfterRewarded = 45f;
        public Units android = new Units();
        public Units ios = new Units();
    }

    [Serializable]
    class Units
    {
        // 앱 ID(ca-app-pub-숫자~숫자). 실행 중에는 쓰지 않고, 에디터의 AdsSetup이 플러그인 설정으로 옮겨 준다.
        public string appId = "";
        // 화면 맨 아래에 늘 떠 있는 배너. 비워 두면(테스트 모드가 아닐 때) 배너를 띄우지 않는다.
        public string banner = "";
        // 다음 스테이지로 넘어갈 때
        public string interstitial = "";
        // 보상형은 쓰임새별로 광고 단위를 나눠야 AdMob에서 어느 쪽이 얼마나 보였는지 따로 볼 수 있다.
        public string rewardedTime = "";
        public string rewardedRevive = "";
    }

    /// <summary>보상형 광고가 쓰이는 자리.</summary>
    public enum Reward
    {
        TimeCharge, // 결과 창의 시간 충전 (걸린 시간 줄이기)
        Revive  // 시간 초과 뒤 이어하기
    }

    static Config config;

    static Config LoadConfig()
    {
        if (config != null) return config;
        var file = Resources.Load<TextAsset>(ConfigResource);
        config = new Config();
        if (file == null) Debug.LogWarning("[Ads] Resources/AdConfig.json이 없어 테스트 광고를 씁니다.");
        else
        {
            try { JsonUtility.FromJsonOverwrite(file.text, config); }
            catch (Exception e) { Debug.LogError("[Ads] AdConfig.json을 읽지 못해 테스트 광고를 씁니다: " + e.Message); }
        }
        return config;
    }

    /// <summary>설정 파일의 실제 ID를 고른다. 테스트 모드이거나 ID가 비어 있으면 테스트 광고 ID를 돌려준다.</summary>
    static string PickId(Func<Units, string> pick, string testId, string label)
    {
        var c = LoadConfig();
        if (c.useTestAds) return testId;
#if UNITY_IOS
        string id = pick(c.ios);
#else
        string id = pick(c.android);
#endif
        id = id == null ? "" : id.Trim();
        if (id.StartsWith("ca-app-pub-") && id.Contains("/")) return id;
        Debug.LogWarning("[Ads] AdConfig.json의 " + label + " 광고 ID가 비어 있거나 형식이 달라 테스트 광고를 씁니다.");
        return testId;
    }

    // 광고를 못 불러왔을 때 다시 시도하기까지 (초)
    const float RetryDelay = 30f;

    static Ads instance;

    Action<bool> rewardedDone;
    Action interstitialClosed;
    bool rewardEarned;
    bool showing;
    float lastInterstitialTime = -1000f;
    float lastRewardedTime = -1000f;

    /// <summary>화면 아래 배너가 차지하는 높이(픽셀). 배너가 없으면 0. UI는 이만큼 위로 비켜 배너에 가리지 않게 한다.</summary>
    public static float BannerHeightPixels { get; private set; }

    static void SetBannerHeight(float pixels) => BannerHeightPixels = Mathf.Max(0f, pixels);

    /// <summary>광고가 화면을 덮고 있는 동안은 게임 시간과 입력을 멈춘다.</summary>
    public static bool Showing => instance != null && instance.showing;

    /// <summary>지금 바로 보여줄 수 있는 보상형 광고가 있는지.</summary>
    public static bool RewardedReady(Reward kind)
    {
#if ADS_ON
        if (instance == null) return false;
        var ad = instance.rewarded[(int)kind];
        return ad != null && ad.CanShowAd();
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
        return instance != null;
#else
        return false;
#endif
    }

    /// <summary>보상형 광고를 보여준다. 광고가 닫히면 done(끝까지 봐서 보상을 받았는지)이 불린다.</summary>
    public static void ShowRewarded(Reward kind, Action<bool> done)
    {
        if (!RewardedReady(kind) || instance.showing)
        {
            done?.Invoke(false);
            return;
        }
#if ADS_ON
        instance.rewardedDone = done;
        instance.rewardEarned = false;
        instance.Begin();
        instance.rewarded[(int)kind].Show(reward => instance.rewardEarned = true);
#else
        Debug.Log("[Ads] 에디터: 보상형 광고(" + kind + ")를 본 것으로 처리");
        instance.lastRewardedTime = Time.realtimeSinceStartup;
        done?.Invoke(true);
#endif
    }

    /// <summary>
    /// clearedStage를 깨고 다음 스테이지로 넘어가는 자리에서 부른다. 조건이 맞고 광고가 준비돼 있으면 전면 광고를 보여주고
    /// true를 돌려주며, 광고가 닫힌 뒤 closed가 불린다. false면 광고 없이 바로 진행하면 된다.
    /// </summary>
    public static bool TryShowInterstitial(int clearedStage, Action closed)
    {
        if (instance == null || instance.showing) return false;
        var c = LoadConfig();
        float now = Time.realtimeSinceStartup;
        if (clearedStage < c.interstitialFromStage) return false;
        if (now - instance.lastInterstitialTime < c.interstitialMinInterval) return false;
        if (now - instance.lastRewardedTime < c.quietAfterRewarded) return false;
#if ADS_ON
        if (instance.interstitial == null || !instance.interstitial.CanShowAd()) return false;
        instance.lastInterstitialTime = now;
        instance.interstitialClosed = closed;
        instance.Begin();
        instance.interstitial.Show();
        return true;
#else
        Debug.Log("[Ads] 전면 광고가 나올 자리 (ADMOB 미설정이라 건너뜀)");
        instance.lastInterstitialTime = now;
        return false;
#endif
    }

    void Awake() => instance = this;

#if ADS_ON
    // Reward 값 순서대로 하나씩 미리 불러 둔다.
    readonly RewardedAd[] rewarded = new RewardedAd[2];
    InterstitialAd interstitial;

    static string RewardedId(Reward kind) => kind == Reward.TimeCharge
        ? PickId(u => u.rewardedTime, TestRewardedId, "보상형(시간 충전)")
        : PickId(u => u.rewardedRevive, TestRewardedId, "보상형(부활)");
    static string InterstitialId => PickId(u => u.interstitial, TestInterstitialId, "전면");

    void Start()
    {
        // iOS는 광고가 떠도 앱이 멈추지 않으므로 직접 멈추게 한다.
        MobileAds.SetiOSAppPauseOnBackground(true);

        var devices = new System.Collections.Generic.List<string>();
        foreach (string id in LoadConfig().testDeviceIds)
            if (!string.IsNullOrWhiteSpace(id)) devices.Add(id.Trim());
        if (devices.Count > 0) MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = devices });

        MobileAds.Initialize(status => OnMain(() =>
        {
            LoadRewarded(Reward.TimeCharge);
            LoadRewarded(Reward.Revive);
            LoadInterstitial();
            LoadBanner();
        }));
    }

    BannerView banner;

    /// <summary>화면 폭에 맞춘 배너를 맨 아래에 띄운다. 설정에 배너 ID가 없으면(테스트 모드 제외) 아무것도 하지 않는다.</summary>
    void LoadBanner()
    {
        var c = LoadConfig();
#if UNITY_IOS
        string real = c.ios.banner;
#else
        string real = c.android.banner;
#endif
        real = real == null ? "" : real.Trim();
        bool valid = real.StartsWith("ca-app-pub-") && real.Contains("/");
        if (!c.useTestAds && !valid) return;

        AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(MobileAds.Utils.GetDeviceSafeWidth());
        banner = new BannerView(c.useTestAds ? TestBannerId : real, size, AdPosition.Bottom);

        // 광고가 뜨기 전부터 자리를 비워 둬야 화면이 갑자기 밀리지 않는다.
        SetBannerHeight(size.Height * MobileAds.Utils.GetDeviceScale());
        banner.OnBannerAdLoaded += () => OnMain(() =>
        {
            SetBannerHeight(banner.GetHeightInPixels());
#if UNITY_EDITOR
            KeepEditorBannerOnTop();
#endif
        });
        banner.OnBannerAdLoadFailed += error => OnMain(() =>
        {
            Debug.LogWarning("[Ads] 배너 광고 로드 실패: " + error);
            SetBannerHeight(0f);
        });
        banner.LoadAd(new AdRequest());
    }

#if UNITY_EDITOR
    // 기기에서는 배너가 게임 화면 위의 별도 창이라 게임 UI(팝업의 어두운 막 등)가 가릴 수 없다.
    // 에디터의 가짜 배너는 플러그인이 띄우는 UI 캔버스(정렬 순서 0)라 게임 팝업 층(40)·효과 층(50)이 덮을 수 있어서,
    // 기기와 똑같이 보이도록 맨 위로 올린다.
    float lastBannerCheck;

    void Update()
    {
        if (banner == null || Time.unscaledTime - lastBannerCheck < 1f) return;
        lastBannerCheck = Time.unscaledTime;
        KeepEditorBannerOnTop();
    }

    static void KeepEditorBannerOnTop()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.transform.root.name.IndexOf("BANNER", StringComparison.OrdinalIgnoreCase) < 0) continue;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;
        }
    }
#endif

    void OnDestroy()
    {
        if (banner != null) banner.Destroy();
        BannerHeightPixels = 0f;
    }

    // 광고 콜백은 다른 스레드에서 올 수 있어서 유니티 메인 스레드로 넘겨서 처리한다.
    static void OnMain(Action action) => GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(action);

    void LoadRewarded(Reward kind)
    {
        int slot = (int)kind;
        if (rewarded[slot] != null)
        {
            rewarded[slot].Destroy();
            rewarded[slot] = null;
        }

        RewardedAd.Load(RewardedId(kind), new AdRequest(), (ad, error) => OnMain(() =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[Ads] 보상형 광고(" + kind + ") 로드 실패: " + error);
                StartCoroutine(RetryRewarded(kind));
                return;
            }
            rewarded[slot] = ad;
            ad.OnAdFullScreenContentClosed += () => OnMain(() => StartCoroutine(FinishRewarded(kind)));
            ad.OnAdFullScreenContentFailed += e => OnMain(() => StartCoroutine(FinishRewarded(kind)));
        }));
    }

    IEnumerator RetryRewarded(Reward kind)
    {
        yield return new WaitForSecondsRealtime(RetryDelay);
        LoadRewarded(kind);
    }

    void LoadInterstitial()
    {
        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) => OnMain(() =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[Ads] 전면 광고 로드 실패: " + error);
                Invoke(nameof(LoadInterstitial), RetryDelay);
                return;
            }
            interstitial = ad;
            ad.OnAdFullScreenContentClosed += () => OnMain(FinishInterstitial);
            ad.OnAdFullScreenContentFailed += e => OnMain(FinishInterstitial);
        }));
    }

    // 닫힘 알림이 보상 알림보다 먼저 오는 경우가 있어 잠깐 기다렸다가 결과를 넘긴다.
    IEnumerator FinishRewarded(Reward kind)
    {
        yield return new WaitForSecondsRealtime(0.15f);
        End();
        if (rewardEarned) lastRewardedTime = Time.realtimeSinceStartup;
        var done = rewardedDone;
        rewardedDone = null;
        LoadRewarded(kind);
        done?.Invoke(rewardEarned);
    }

    void FinishInterstitial()
    {
        End();
        var closed = interstitialClosed;
        interstitialClosed = null;
        LoadInterstitial();
        closed?.Invoke();
    }
#endif

    // 광고가 떠 있는 동안 게임 소리를 멈춘다.
    void Begin()
    {
        showing = true;
        AudioListener.pause = true;
    }

    void End()
    {
        showing = false;
        AudioListener.pause = false;
    }
}
