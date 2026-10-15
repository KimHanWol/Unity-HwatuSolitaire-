#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// 개발 빌드 확인용: 실행 인자 -autoshot &lt;폴더&gt; 가 있으면 주요 화면을 차례로 열어 스크린샷을 남기고 종료한다.
/// PC 개발 빌드 전용이라 저장 데이터(PlayerPrefs)는 처음에 지운다.
///   Builds/Windows/HwatuSolitaire.exe -screen-width 540 -screen-height 1170 -screen-fullscreen 0 -autoshot shots
/// </summary>
public class DevAutoShot : MonoBehaviour
{
    string dir;
    int index;

    public static void StartIfRequested(App app)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-autoshot");
        if (i < 0 || i + 1 >= args.Length) return;
        PlayerPrefs.DeleteAll();
        Settings.MusicOn = false;
        Settings.SoundOn = false;
        Loc.Set(true);
        Sfx.RefreshMusic();
        var shot = app.gameObject.AddComponent<DevAutoShot>();
        shot.dir = args[i + 1];
    }

    IEnumerator Start()
    {
        Application.runInBackground = true;
        Directory.CreateDirectory(dir);
        yield return Wait(1.5f);
        yield return Shot("title");

        Progress.Record(1, 3, 20);
        Progress.Record(2, 2, 40);
        Progress.Record(3, 1, 60);
        App.I.ShowStages(4);
        yield return Wait(0.6f);
        yield return Shot("stages");

        App.I.Play(1);
        yield return Wait(1.0f);
        yield return Shot("rules");
        App.I.CloseAllPopups();
        yield return Wait(0.3f);
        var game = FindAnyObjectByType<GameScreen>();
        yield return Shot("stage1");
        game.DevSelectFirst();
        yield return Wait(0.2f);
        yield return Shot("selected");
        game.DevPlayMove();
        yield return Wait(0.2f);
        yield return Shot("snap");
        yield return Wait(0.3f);
        yield return Shot("pending");
        game.DevStackThird();
        yield return Wait(0.4f);
        yield return Shot("stacked");
        yield return Wait(1.3f);
        yield return Shot("bomb_ready");
        game.DevDetonate();
        yield return Wait(0.25f);
        yield return Shot("boom");
        yield return Wait(1.0f);
        game.DevHint();
        yield return Wait(0.3f);
        yield return Shot("hint");
        game.DevPause();
        yield return Wait(0.5f);
        yield return Shot("pause");
        App.I.CloseAllPopups();
        game.DevTimeUp();
        yield return Wait(0.8f);
        yield return Shot("timeup");

        foreach (int stage in new[] { 60, 130, 260, 300 })
        {
            App.I.Play(stage);
            yield return Wait(1.4f);
            App.I.CloseAllPopups();
            yield return Wait(0.3f);
            yield return Shot("stage" + stage);
        }
        game = FindAnyObjectByType<GameScreen>();
        game.DevWin();
        yield return Wait(2.6f);
        yield return Shot("clear");

        Loc.Set(false);
        App.I.ShowTitle();
        yield return Wait(1.5f);
        yield return Shot("title_en");
        App.I.Play(10);
        yield return Wait(1.4f);
        App.I.CloseAllPopups();
        yield return Wait(0.3f);
        yield return Shot("stage10_en");

        Debug.Log("[DevAutoShot] done: " + index + " shots in " + dir);
        PlayerPrefs.DeleteAll();
        Application.Quit();
    }

    static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
        }
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        string path = Path.Combine(dir, (++index).ToString("00") + "_" + name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        yield return null;
        yield return null;
    }
}
#endif
