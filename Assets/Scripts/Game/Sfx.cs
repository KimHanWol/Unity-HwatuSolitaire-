using UnityEngine;

/// <summary>
/// 효과음과 배경음. 오디오 파일 없이 코드로 합성한다.
/// - 짝: 화투패를 담요에 내리치는 "짝!" (높은 잡음 순간음 + 패 몸통 울림 + 낮은 쿵)
/// - 쌌다: 북 두 번 "둥둥" + 징
/// - 폭탄: 낮게 떨어지는 쾅 + 터지는 잡음 + 파편 튀는 소리
/// - 배경음: 가야금 같은 뜯는 소리(Karplus-Strong)로 평조(궁상각치우) 가락
/// 실제 녹음으로 바꾸려면 Resources/Audio/<이름>.wav|ogg 를 두면 그쪽을 우선 쓴다
/// (snap, select, deny, stack, bomb, shuffle, clear, star, fail, hint, click, tick, slide, bgm).
/// </summary>
public class Sfx : MonoBehaviour
{
    const int Rate = 44100;
    const float MusicVolume = 0.22f;

    static Sfx instance;

    AudioSource effects;
    AudioSource music;
    AudioClip[] snap;
    AudioClip[] combo;
    AudioClip[] star;
    AudioClip select, deny, stack, bomb, shuffle, clear, fail, hint, click, tick, slide, revive;
    float lastSnap;

    public static void Select() => Play(i => i.select, 0.45f);
    public static void Deny() => Play(i => i.deny, 0.5f);
    public static void Stack() => Play(i => i.stack, 0.9f);
    public static void Shuffle() => Play(i => i.shuffle, 0.6f);
    public static void Clear() => Play(i => i.clear, 0.8f);
    public static void Fail() => Play(i => i.fail, 0.7f);
    public static void Hint() => Play(i => i.hint, 0.5f);
    public static void Click() => Play(i => i.click, 0.45f);
    public static void Tick() => Play(i => i.tick, 0.5f);
    public static void Slide() => Play(i => i.slide, 0.25f);
    public static void Revive() => Play(i => i.revive, 0.7f);

    /// <summary>별 하나가 찍힐 때 (0, 1, 2번째로 음이 올라간다).</summary>
    public static void Star(int index)
    {
        if (instance == null) return;
        instance.PlayClip(instance.star[Mathf.Clamp(index, 0, instance.star.Length - 1)], 0.6f);
    }

    /// <summary>짝을 맞췄을 때. combo는 연속 몇 번째인지(1부터), 높을수록 종소리가 올라간다.</summary>
    public static void Snap(int comboCount)
    {
        if (instance == null) return;
        // 같은 프레임에 두 번 나지 않게
        if (Time.unscaledTime - instance.lastSnap < 0.03f) return;
        instance.lastSnap = Time.unscaledTime;
        instance.PlayClip(instance.snap[Random.Range(0, instance.snap.Length)], 1f);
        if (comboCount >= 2) instance.PlayClip(instance.combo[Mathf.Clamp(comboCount - 2, 0, instance.combo.Length - 1)], 0.35f);
    }

    /// <summary>폭탄. 여러 장이 연달아 터질 때는 첫 소리만 크게.</summary>
    public static void Bomb(float volume = 1f) => Play(i => i.bomb, 0.9f * volume);

    /// <summary>배경음악 설정이 바뀌었을 때 호출한다.</summary>
    public static void RefreshMusic()
    {
        if (instance == null) return;
        if (Settings.MusicOn && !instance.music.isPlaying) instance.music.Play();
        else if (!Settings.MusicOn && instance.music.isPlaying) instance.music.Stop();
    }

    static void Play(System.Func<Sfx, AudioClip> pick, float volume)
    {
        if (instance == null) return;
        instance.PlayClip(pick(instance), volume);
    }

    void PlayClip(AudioClip clip, float volume)
    {
        if (clip != null && Settings.SoundOn) effects.PlayOneShot(clip, volume);
    }

    void Awake()
    {
        instance = this;
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.volume = MusicVolume;

        seed = 12345;
        snap = new[] { Load("snap", () => BuildSnap(1f)), Load("snap2", () => BuildSnap(1.08f)), Load("snap3", () => BuildSnap(0.93f)) };

        // 연속으로 맞출수록 평조 음계를 타고 올라가는 맑은 소리
        int[] ladder = { 79, 81, 84, 86, 88, 91, 93, 96 };
        combo = new AudioClip[ladder.Length];
        for (int i = 0; i < ladder.Length; i++)
        {
            var b = new float[(int)(0.5f * Rate)];
            Bell(b, 0f, Freq(ladder[i]), 0.45f, 0.5f);
            combo[i] = ToClip("combo" + i, b);
        }

        star = new AudioClip[3];
        for (int i = 0; i < 3; i++)
        {
            var b = new float[(int)(0.9f * Rate)];
            Bell(b, 0f, Freq(84 + i * 4), 0.8f, 0.6f);
            Bell(b, 0.02f, Freq(96 + i * 4), 0.5f, 0.2f);
            star[i] = Load(i == 0 ? "star" : "star" + (i + 1), () => ToClip("star" + i, Normalize(b, 0.8f)));
        }

        select = Load("select", BuildSelect);
        deny = Load("deny", BuildDeny);
        stack = Load("stack", BuildStack);
        bomb = Load("bomb", BuildBomb);
        shuffle = Load("shuffle", BuildShuffle);
        clear = Load("clear", BuildClear);
        fail = Load("fail", BuildFail);
        hint = Load("hint", BuildHint);
        click = Load("click", BuildClick);
        tick = Load("tick", BuildTick);
        slide = Load("slide", BuildSlide);
        revive = Load("revive", BuildRevive);
        music.clip = Load("bgm", BuildMusic);

        RefreshMusic();
    }

    static AudioClip Load(string name, System.Func<AudioClip> build)
    {
        var clip = Resources.Load<AudioClip>("Audio/" + name);
        return clip != null ? clip : build();
    }

    // ------------------------------------------------------------ 합성 도구
    uint seed;

    float Noise()
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return (seed & 0xFFFFFF) / (float)0x800000 - 1f;
    }

    static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    static AudioClip ToClip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float[] Normalize(float[] data, float peak)
    {
        float max = 0f;
        foreach (float v in data) max = Mathf.Max(max, Mathf.Abs(v));
        if (max > 0f)
            for (int i = 0; i < data.Length; i++) data[i] *= peak / max;
        return data;
    }

    /// <summary>종소리: 기본음 + 맑은 배음, 지수 감쇠.</summary>
    static void Bell(float[] b, float start, float f, float decay, float amp)
    {
        int s0 = (int)(start * Rate);
        for (int i = s0; i < b.Length; i++)
        {
            float t = (i - s0) / (float)Rate;
            float env = Mathf.Exp(-t / (decay * 0.35f)) * Mathf.Min(1f, t * 400f);
            b[i] += amp * env * (Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6f));
        }
    }

    /// <summary>뜯는 현(Karplus-Strong). 가야금 같은 소리.</summary>
    void Pluck(float[] b, float start, float f, float duration, float amp, float damping = 0.996f, float brightness = 0.5f)
    {
        int period = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
        var ring = new float[period];
        for (int i = 0; i < period; i++) ring[i] = Noise();
        // 처음 잡음을 살짝 다듬어 너무 거칠지 않게
        for (int k = 0; k < 2; k++)
            for (int i = 1; i < period; i++) ring[i] = ring[i] * brightness + ring[i - 1] * (1f - brightness);
        int s0 = (int)(start * Rate);
        int n = (int)(duration * Rate);
        int idx = 0;
        for (int i = 0; i < n && s0 + i < b.Length; i++)
        {
            int next = (idx + 1) % period;
            float v = ring[idx];
            ring[idx] = (ring[idx] + ring[next]) * 0.5f * damping;
            idx = next;
            float fadeOut = i > n - 400 ? (n - i) / 400f : 1f;
            b[s0 + i] += v * amp * fadeOut;
        }
    }

    /// <summary>북: 높이가 뚝 떨어지는 낮은 사인 + 가죽 잡음.</summary>
    void Drum(float[] b, float start, float f, float amp)
    {
        int s0 = (int)(start * Rate);
        float phase = 0f;
        for (int i = s0; i < b.Length; i++)
        {
            float t = (i - s0) / (float)Rate;
            if (t > 0.7f) break;
            float freq = f * (1f + 1.2f * Mathf.Exp(-t / 0.02f));
            phase += 2f * Mathf.PI * freq / Rate;
            float body = Mathf.Sin(phase) * Mathf.Exp(-t / 0.18f);
            float skin = Noise() * Mathf.Exp(-t / 0.012f) * 0.5f;
            b[i] += amp * (body + skin);
        }
    }

    // ------------------------------------------------------------ 소리들
    AudioClip BuildSnap(float pitch)
    {
        var b = new float[(int)(0.25f * Rate)];
        float lp = 0f, bp = 0f, low = 0f;
        float cutoff = 2f * Mathf.Sin(Mathf.PI * 2100f * pitch / Rate);
        float phase = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)Rate;
            float n = Noise();
            // ① 패 가장자리가 담요를 때리는 날카로운 순간음 (고역 잡음, 아주 짧게)
            float hp = n - low;
            low += (n - low) * 0.25f;
            float crack = hp * Mathf.Exp(-t / 0.0045f) * 1.1f;
            // ② "짝" 몸통: 2kHz 근처로 걸러진 잡음 (상태 변수 필터)
            lp += cutoff * bp;
            float hpf = n - lp - 0.6f * bp;
            bp += cutoff * hpf;
            float body = bp * Mathf.Exp(-t / 0.028f) * 1.4f;
            // ③ 바닥에 닿는 낮은 쿵
            float f = 150f * pitch * (1f + 0.6f * Mathf.Exp(-t / 0.01f));
            phase += 2f * Mathf.PI * f / Rate;
            float thump = Mathf.Sin(phase) * Mathf.Exp(-t / 0.04f) * 0.55f;
            // ④ 나무패 같은 단단한 울림
            float knock = Mathf.Sin(2f * Mathf.PI * 820f * pitch * t) * Mathf.Exp(-t / 0.015f) * 0.25f;
            b[i] = crack + body + thump + knock;
        }
        return ToClip("snap", Normalize(b, 0.95f));
    }

    AudioClip BuildSelect()
    {
        var b = new float[(int)(0.08f * Rate)];
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)Rate;
            b[i] = Mathf.Sin(2f * Mathf.PI * 1650f * t) * Mathf.Exp(-t / 0.014f) * 0.7f + Noise() * Mathf.Exp(-t / 0.003f) * 0.4f;
        }
        return ToClip("select", Normalize(b, 0.6f));
    }

    AudioClip BuildClick()
    {
        var b = new float[(int)(0.07f * Rate)];
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)Rate;
            b[i] = Mathf.Sin(2f * Mathf.PI * 980f * t) * Mathf.Exp(-t / 0.018f) + Noise() * Mathf.Exp(-t / 0.002f) * 0.3f;
        }
        return ToClip("click", Normalize(b, 0.5f));
    }

    AudioClip BuildTick()
    {
        var b = new float[(int)(0.1f * Rate)];
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)Rate;
            b[i] = Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-t / 0.01f) + Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t / 0.02f) * 0.5f;
        }
        return ToClip("tick", Normalize(b, 0.55f));
    }

    AudioClip BuildDeny()
    {
        var b = new float[(int)(0.4f * Rate)];
        Pluck(b, 0f, Freq(50), 0.18f, 0.8f, 0.99f, 0.3f);
        Pluck(b, 0.11f, Freq(46), 0.28f, 0.8f, 0.99f, 0.3f);
        return ToClip("deny", Normalize(b, 0.6f));
    }

    AudioClip BuildStack()
    {
        var b = new float[(int)(1.8f * Rate)];
        Drum(b, 0f, 78f, 1f);
        Drum(b, 0.17f, 84f, 1.1f);
        // 징: 서로 어긋난 배음이 오래 운다
        float[] partials = { 196f, 292f, 413f, 527f, 701f };
        float[] amps = { 0.5f, 0.35f, 0.25f, 0.18f, 0.1f };
        int s0 = (int)(0.3f * Rate);
        for (int i = s0; i < b.Length; i++)
        {
            float t = (i - s0) / (float)Rate;
            float swell = Mathf.Min(1f, t * 30f) * Mathf.Exp(-t / 0.55f);
            float v = 0f;
            for (int k = 0; k < partials.Length; k++)
                v += amps[k] * Mathf.Sin(2f * Mathf.PI * partials[k] * (1f + 0.004f * Mathf.Sin(t * 6f)) * t);
            b[i] += v * swell * 0.7f;
        }
        return ToClip("stack", Normalize(b, 0.9f));
    }

    AudioClip BuildBomb()
    {
        var b = new float[(int)(1.2f * Rate)];
        float phase = 0f, lp = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)Rate;
            // 쾅: 120Hz → 35Hz
            float f = 35f + 85f * Mathf.Exp(-t / 0.12f);
            phase += 2f * Mathf.PI * f / Rate;
            float boom = Mathf.Sin(phase) * Mathf.Exp(-t / 0.35f) * 1.2f;
            // 터지는 잡음: 점점 어두워진다
            float cut = Mathf.Lerp(0.02f, 0.6f, Mathf.Exp(-t / 0.08f));
            lp += (Noise() - lp) * cut;
            float blast = lp * Mathf.Exp(-t / 0.22f) * 2.2f;
            // 파편: 드문드문 튀는 짧은 딱딱 소리
            float crackle = 0f;
            if (t > 0.05f && t < 0.8f && Noise() > 0.995f - 0.004f * Mathf.Exp(-t / 0.3f)) crackle = Noise() * Mathf.Exp(-t / 0.4f) * 1.5f;
            b[i] = boom + blast + crackle;
        }
        return ToClip("bomb", Normalize(b, 0.95f));
    }

    AudioClip BuildShuffle()
    {
        var b = new float[(int)(0.75f * Rate)];
        float tPos = 0.02f;
        while (tPos < 0.6f)
        {
            int s0 = (int)(tPos * Rate);
            float lp = 0f;
            for (int i = 0; i < (int)(0.012f * Rate) && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float n = Noise();
                lp += (n - lp) * 0.5f;
                b[s0 + i] += (n - lp) * Mathf.Exp(-t / 0.003f) * 0.6f;
            }
            tPos += 0.018f + 0.012f * Mathf.Abs(Noise());
        }
        return ToClip("shuffle", Normalize(b, 0.6f));
    }

    AudioClip BuildSlide()
    {
        var b = new float[(int)(0.18f * Rate)];
        float lp = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)Rate;
            lp += (Noise() - lp) * 0.08f;
            b[i] = lp * Mathf.Sin(Mathf.PI * t / 0.18f);
        }
        return ToClip("slide", Normalize(b, 0.4f));
    }

    AudioClip BuildClear()
    {
        var b = new float[(int)(2.0f * Rate)];
        // 평조: 도 레 파 솔 라
        int[] notes = { 72, 74, 77, 79, 81, 84, 86, 89 };
        for (int i = 0; i < notes.Length; i++) Pluck(b, i * 0.07f, Freq(notes[i]), 1.2f, 0.7f, 0.997f);
        Pluck(b, 0.62f, Freq(84), 1.3f, 0.8f, 0.998f);
        Pluck(b, 0.62f, Freq(77), 1.3f, 0.6f, 0.998f);
        Pluck(b, 0.62f, Freq(72), 1.3f, 0.6f, 0.998f);
        Drum(b, 0.62f, 90f, 0.6f);
        return ToClip("clear", Normalize(b, 0.85f));
    }

    AudioClip BuildFail()
    {
        var b = new float[(int)(1.6f * Rate)];
        int[] notes = { 69, 65, 62, 60 };
        for (int i = 0; i < notes.Length; i++) Pluck(b, i * 0.22f, Freq(notes[i]), 0.9f, 0.7f, 0.995f, 0.35f);
        return ToClip("fail", Normalize(b, 0.7f));
    }

    AudioClip BuildHint()
    {
        var b = new float[(int)(0.7f * Rate)];
        Bell(b, 0f, Freq(88), 0.3f, 0.4f);
        Bell(b, 0.07f, Freq(91), 0.3f, 0.4f);
        Bell(b, 0.14f, Freq(96), 0.5f, 0.45f);
        return ToClip("hint", Normalize(b, 0.6f));
    }

    AudioClip BuildRevive()
    {
        var b = new float[(int)(1.2f * Rate)];
        int[] notes = { 67, 72, 76, 79, 84 };
        for (int i = 0; i < notes.Length; i++) Bell(b, i * 0.06f, Freq(notes[i]), 0.6f, 0.35f);
        return ToClip("revive", Normalize(b, 0.7f));
    }

    /// <summary>
    /// 배경음: 느린 굿거리 느낌(12/8)으로 가야금 가락과 낮은 소리, 장구 같은 북을 깐 32마디 반복.
    /// 가락은 정해진 시드로 평조 음계 안에서 이웃 음으로 움직이며 만든다.
    /// </summary>
    AudioClip BuildMusic()
    {
        const float beat = 0.6f;      // 한 박 (점4분)
        const int bars = 16;
        const int beatsPerBar = 4;
        float length = bars * beatsPerBar * beat;
        var b = new float[(int)(length * Rate) + Rate];
        int[] scale = { 60, 62, 65, 67, 69, 72, 74, 77, 79, 81 }; // 평조
        seed = 777;
        int pos = 4;
        for (int bar = 0; bar < bars; bar++)
        {
            float barStart = bar * beatsPerBar * beat;
            // 낮은 소리: 마디 첫 박에 으뜸음이나 딸림음
            int bass = (bar % 4 == 3) ? 43 : (bar % 2 == 0 ? 48 : 41);
            Pluck(b, barStart, Freq(bass), beat * 3.5f, 0.45f, 0.998f, 0.25f);
            Pluck(b, barStart + beat * 2f, Freq(bass + 7), beat * 1.8f, 0.25f, 0.997f, 0.25f);
            // 장구: 덩(첫 박) 기덕(셋째 박) 쿵(넷째 박)
            Drum(b, barStart, 70f, 0.25f);
            Drum(b, barStart + beat * 2f, 110f, 0.12f);
            Drum(b, barStart + beat * 3f, 80f, 0.15f);
            // 가락: 한 박을 셋으로 나눈 자리에 음을 놓는다
            for (int sub = 0; sub < beatsPerBar * 3; sub++)
            {
                bool strong = sub % 3 == 0;
                if (!strong && Noise() > -0.1f) continue;
                if (bar % 8 == 7 && sub > 6) continue; // 여덟째 마디 끝은 숨 쉬기
                int step = Mathf.RoundToInt(Noise() * 1.6f);
                pos = Mathf.Clamp(pos + step, 2, scale.Length - 1);
                float when = barStart + sub * beat / 3f;
                Pluck(b, when, Freq(scale[pos]), beat * 2.2f, strong ? 0.4f : 0.28f, 0.9965f, 0.55f);
                // 농현 느낌으로 같은 음을 옥타브 위에서 살짝
                if (strong && Noise() > 0.6f) Pluck(b, when + 0.02f, Freq(scale[pos] + 12), beat, 0.08f, 0.995f, 0.7f);
            }
        }
        // 끝 부분 소리를 앞으로 접어 이음매 없이 돈다.
        int loopLen = (int)(length * Rate);
        var loop = new float[loopLen];
        for (int i = 0; i < b.Length; i++) loop[i % loopLen] += b[i];
        return ToClip("bgm", Normalize(loop, 0.7f));
    }
}
