using System;
using System.Collections.Generic;
using HwatuSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 앱 시작점. 씬에는 이 컴포넌트 하나만 있고, 캔버스·배경·소리·광고·화면은 모두 여기서 코드로 만든다.
/// 화면: 타이틀 → 스테이지 목록 → 게임. 팝업은 그 위 층.
/// </summary>
public class App : MonoBehaviour
{
    public static App I { get; private set; }

    /// <summary>게임 이름. 바꾸려면 여기와 ProjectSetup.ProductName.</summary>
    public static string Title => Loc.T("화투 사천성", "Hwatu Solitaire");
    public static string Subtitle => Loc.T("층층이 쌓인 화투, 같은 달끼리 짝!", "Match stacked Korean flower cards");

    public const float RefWidth = 1080f, RefHeight = 1920f;

    Canvas canvas;
    RectTransform canvasRoot;
    RectTransform safe;
    RectTransform screenLayer;
    RectTransform popupLayer;
    MonoBehaviour current;
    Rect lastSafe;
    float lastBanner = -1f;
    Vector2Int lastScreen;
    readonly List<Popup> popups = new List<Popup>();

    public StageList Stages { get; private set; }

    /// <summary>화면·팝업이 놓이는 안전 영역(노치·배너 제외).</summary>
    public RectTransform Safe => safe;
    public RectTransform PopupLayer => popupLayer;
    public bool PopupOpen => popups.Count > 0;

    /// <summary>안전 영역이나 배너 높이가 바뀌었을 때 (화면 다시 배치).</summary>
    public event Action LayoutChanged;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Input.multiTouchEnabled = false;

        LoadStages();
        EnsureCamera();
        BuildCanvas();
        Ui.EnsureEventSystem();
        gameObject.AddComponent<Sfx>();
        gameObject.AddComponent<Ads>();
        Fx.Create(canvasRoot);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DevAutoShot.StartIfRequested(this);
#endif
        ShowTitle();
    }

    void LoadStages()
    {
        var file = Resources.Load<TextAsset>("Stages/stages");
        Stages = file != null ? JsonUtility.FromJson<StageList>(file.text) : null;
        if (Stages == null || Stages.stages == null || Stages.stages.Length == 0)
        {
            // 파일이 없으면(개발 중) 그 자리에서 만든다. 별 기준 계산은 시간이 걸려 대충 잡는다.
            Debug.LogWarning("[App] Resources/Stages/stages.json이 없어 스테이지를 즉석에서 만듭니다. Tools/StageTool build 를 돌리세요.");
            var list = new StageDef[StageGenerator.StageCount];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = StageGenerator.Build(i + 1);
                int cards = list[i].CreateBoard().CardCount;
                list[i].star3 = cards * 2 + 10;
                list[i].star2 = cards * 3 + 15;
                list[i].timeLimit = cards * 5 + 30;
            }
            Stages = new StageList { version = 0, stages = list };
        }
    }

    public StageDef Stage(int number) => Stages.stages[Mathf.Clamp(number, 1, Stages.stages.Length) - 1];

    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Art.FeltDark;
        cam.orthographic = true;
        cam.gameObject.AddComponent<AudioListener>();
    }

    void BuildCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform));
        go.layer = 5;
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        // 폭 기준으로 맞추되, 화면이 넓적하면(태블릿·접는 폰) 높이 기준으로 바꿔 넘치지 않게 한다.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
        go.AddComponent<GraphicRaycaster>();
        canvasRoot = (RectTransform)go.transform;

        // 배경: 초록 담요 + 가장자리 어둡게
        var felt = Ui.Rect("Felt", canvasRoot);
        Ui.Stretch(felt);
        var raw = felt.gameObject.AddComponent<RawImage>();
        raw.texture = Art.FeltTexture;
        raw.raycastTarget = false;
        felt.gameObject.AddComponent<FeltTiler>();
        var light = Ui.Image(canvasRoot, Art.Glow, new Color(1f, 1f, 0.85f, 0.10f), "Light");
        Ui.Place(light.rectTransform, 0.5f, 0.55f, new Vector2(1800f, 2200f));
        var vignette = Ui.Image(canvasRoot, Art.Vignette, Color.white, "Vignette");
        Ui.Stretch(vignette.rectTransform);

        safe = Ui.Rect("Safe", canvasRoot);
        Ui.Stretch(safe);
        screenLayer = Ui.Rect("Screens", safe);
        Ui.Stretch(screenLayer);
        // 팝업 층은 화면 전체를 덮어야 뒤가 고르게 어두워진다 (노치·하단 여백·배너 자리까지).
        // 실제 기기의 배너 광고는 게임 화면 위에 따로 그려지므로 그 밑을 어둡게 해도 광고는 그대로 보인다.
        popupLayer = Ui.Rect("Popups", canvasRoot);
        Ui.Stretch(popupLayer);
        var pc = popupLayer.gameObject.AddComponent<Canvas>();
        pc.overrideSorting = true;
        pc.sortingOrder = 40;
        popupLayer.gameObject.AddComponent<GraphicRaycaster>();
        ApplySafeArea();
    }

    void Update()
    {
        if (lastSafe != Screen.safeArea || !Mathf.Approximately(lastBanner, Ads.BannerHeightPixels)
            || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            ApplySafeArea();

        // 안드로이드 뒤로 가기
        if (Input.GetKeyDown(KeyCode.Escape) && !Ads.Showing)
        {
            if (popups.Count > 0) popups[popups.Count - 1].Back();
            else if (current is IBackHandler back) back.OnBack();
        }
    }

    void ApplySafeArea()
    {
        lastSafe = Screen.safeArea;
        lastBanner = Ads.BannerHeightPixels;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        // 넓적한 화면이면 높이 기준으로
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.matchWidthOrHeight = Screen.width / (float)Screen.height > RefWidth / RefHeight * 1.08f ? 1f : 0f;

        Rect s = Screen.safeArea;
        float bottom = Mathf.Max(s.yMin, Ads.BannerHeightPixels);
        safe.anchorMin = new Vector2(s.xMin / Screen.width, bottom / Screen.height);
        safe.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        LayoutChanged?.Invoke();
    }

    // ------------------------------------------------------------ 화면 전환
    T Swap<T>() where T : MonoBehaviour
    {
        CloseAllPopups();
        if (current != null) Destroy(current.gameObject);
        var rt = Ui.Rect(typeof(T).Name, screenLayer);
        Ui.Stretch(rt);
        var screen = rt.gameObject.AddComponent<T>();
        current = screen;
        // 새 화면이 살짝 떠오르며 나타난다.
        var group = rt.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        Anim.Fade(group, 0f, 1f, 0.22f);
        return screen;
    }

    public void ShowTitle() => Swap<TitleScreen>();

    public void ShowStages(int focus = 0) => Swap<StageSelectScreen>().Build(focus > 0 ? focus : Progress.LastPlayed);

    /// <summary>튜토리얼 판으로 시작.</summary>
    public void PlayTutorial() => Swap<GameScreen>().BeginTutorial();

    /// <summary>스테이지 시작. bonusSeconds를 주면 그만큼 시간을 미리 받고 시작한다 (결과 창의 "추가 시간 받고 다시 하기").</summary>
    public void Play(int stage, int bonusSeconds = 0)
    {
        stage = Mathf.Clamp(stage, 1, Stages.stages.Length);
        Progress.LastPlayed = stage;
        Swap<GameScreen>().Begin(Stage(stage), bonusSeconds);
    }

    // ------------------------------------------------------------ 팝업
    public void Register(Popup p) => popups.Add(p);
    public void Unregister(Popup p) => popups.Remove(p);

    public void CloseAllPopups()
    {
        for (int i = popups.Count - 1; i >= 0; i--) popups[i].Close(true);
        popups.Clear();
    }
}

/// <summary>안드로이드 뒤로 가기를 받는 화면.</summary>
public interface IBackHandler
{
    void OnBack();
}

/// <summary>담요 무늬를 화면 크기에 맞춰 반복한다 (늘어나지 않게).</summary>
public class FeltTiler : MonoBehaviour
{
    RawImage raw;
    RectTransform rt;

    void Awake()
    {
        raw = GetComponent<RawImage>();
        rt = (RectTransform)transform;
    }

    void LateUpdate()
    {
        Rect r = rt.rect;
        raw.uvRect = new Rect(0f, 0f, r.width / 384f, r.height / 384f);
    }
}
