using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 코루틴으로 도는 작은 트윈. 모두 실제 시간(unscaled)으로 움직인다.
/// 대상 오브젝트가 사라지면 다음 프레임에 조용히 멈춘다.
/// </summary>
public class Anim : MonoBehaviour
{
    static Anim runner;

    static Anim Runner
    {
        get
        {
            if (runner == null)
            {
                var go = new GameObject("Anim");
                DontDestroyOnLoad(go);
                runner = go.AddComponent<Anim>();
            }
            return runner;
        }
    }

    /// <summary>duration 초 동안 step(0→1)을 부른다. owner가 파괴되면 멈춘다.</summary>
    public static Coroutine Run(UnityEngine.Object owner, float duration, Action<float> step, Action done = null, float delay = 0f)
        => Runner.StartCoroutine(Routine(owner, duration, step, done, delay));

    public static Coroutine Delay(UnityEngine.Object owner, float seconds, Action action)
        => Runner.StartCoroutine(Routine(owner, 0f, null, action, seconds));

    public static void Stop(Coroutine c)
    {
        if (c != null && runner != null) runner.StopCoroutine(c);
    }

    static IEnumerator Routine(UnityEngine.Object owner, float duration, Action<float> step, Action done, float delay)
    {
        bool hasOwner = owner != null;
        if (delay > 0f)
        {
            float wait = 0f;
            while (wait < delay)
            {
                yield return null;
                wait += Time.unscaledDeltaTime;
            }
        }
        float t = 0f;
        while (t < duration)
        {
            if (hasOwner && owner == null) yield break;
            step?.Invoke(Mathf.Clamp01(t / duration));
            yield return null;
            t += Time.unscaledDeltaTime;
        }
        if (hasOwner && owner == null) yield break;
        step?.Invoke(1f);
        done?.Invoke();
    }

    // ------------------------------------------------------------ 이징
    public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    public static float InCubic(float t) => t * t * t;
    public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    public static float OutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float OutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = 2f * Mathf.PI / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    /// <summary>0에서 1로 갔다가 다시 0으로 (펀치).</summary>
    public static float Punch(float t) => Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);

    // ------------------------------------------------------------ 자주 쓰는 모양
    public static Coroutine Scale(Transform target, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null, Action done = null, float delay = 0f)
    {
        ease = ease ?? OutCubic;
        return Run(target, duration, t => { if (target) target.localScale = Vector3.LerpUnclamped(from, to, ease(t)); }, done, delay);
    }

    public static Coroutine Move(RectTransform target, Vector2 from, Vector2 to, float duration, Func<float, float> ease = null, Action done = null, float delay = 0f)
    {
        ease = ease ?? OutCubic;
        return Run(target, duration, t => { if (target) target.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(t)); }, done, delay);
    }

    public static Coroutine Fade(CanvasGroup target, float from, float to, float duration, Action done = null, float delay = 0f)
        => Run(target, duration, t => { if (target) target.alpha = Mathf.Lerp(from, to, t); }, done, delay);

    /// <summary>눌렀을 때 통 튀는 느낌.</summary>
    public static Coroutine Pop(Transform target, float amount = 0.15f, float duration = 0.25f)
        => Run(target, duration, t => { if (target) target.localScale = Vector3.one * (1f + amount * Punch(t)); });
}
