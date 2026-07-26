using UnityEngine;

public class ArcheryAudioManager : MonoBehaviour
{
    public AudioSource bowSource;
    public AudioSource drawTickSource;
    [Range(0f, 1f)] public float volume = 0.85f;
    public float drawTickInterval01 = 0.08f;

    private AudioClip _nockClip;
    private AudioClip _drawTickClip;
    private AudioClip _releaseClip;
    private AudioClip _hitThudClip;
    private AudioClip _missClip;
    private AudioClip _goldClip;
    private AudioClip _roundEndClip;
    private AudioClip[] _ringChimeClips;
    private float _lastTickDraw01;

    private void Awake()
    {
        _nockClip = ArcheryAudioSynth.CreateNockClick();
        _drawTickClip = ArcheryAudioSynth.CreateDrawTick();
        _releaseClip = ArcheryAudioSynth.CreateReleaseTwang();
        _hitThudClip = ArcheryAudioSynth.CreateHitThud();
        _missClip = ArcheryAudioSynth.CreateMissThud();
        _goldClip = ArcheryAudioSynth.CreateGoldFanfare();
        _roundEndClip = ArcheryAudioSynth.CreateRoundEndArpeggio();

        // 2/4/6/8/10 环对应逐级升高的音符（G4 A4 B4 C5 D5），命中环数越高音越亮。
        var chimeFrequencies = new[] { 392f, 440f, 494f, 523f, 587f };
        _ringChimeClips = new AudioClip[chimeFrequencies.Length];
        for (var i = 0; i < chimeFrequencies.Length; i++)
        {
            _ringChimeClips[i] = ArcheryAudioSynth.CreateRingChime(chimeFrequencies[i], $"ArcheryChime_{i}");
        }
    }

    private void OnEnable()
    {
        ArcheryEvents.OnArrowNocked += HandleArrowNocked;
        ArcheryEvents.OnDrawChanged += HandleDrawChanged;
        ArcheryEvents.OnArrowReleased += HandleArrowReleased;
        ArcheryEvents.OnArrowHit += HandleArrowHit;
        ArcheryEvents.OnArrowMissed += HandleArrowMissed;
        ArcheryEvents.OnRoundFinished += HandleRoundFinished;
    }

    private void OnDisable()
    {
        ArcheryEvents.OnArrowNocked -= HandleArrowNocked;
        ArcheryEvents.OnDrawChanged -= HandleDrawChanged;
        ArcheryEvents.OnArrowReleased -= HandleArrowReleased;
        ArcheryEvents.OnArrowHit -= HandleArrowHit;
        ArcheryEvents.OnArrowMissed -= HandleArrowMissed;
        ArcheryEvents.OnRoundFinished -= HandleRoundFinished;
    }

    private void HandleArrowNocked()
    {
        _lastTickDraw01 = 0f;
        PlayOnBow(_nockClip, 0.9f);
    }

    private void HandleDrawChanged(float draw01)
    {
        if (draw01 <= 0.001f)
        {
            _lastTickDraw01 = 0f;
            return;
        }

        if (draw01 - _lastTickDraw01 < drawTickInterval01) return;

        _lastTickDraw01 = draw01;
        if (drawTickSource != null && drawTickSource.isActiveAndEnabled && _drawTickClip != null)
        {
            drawTickSource.pitch = 0.85f + 0.55f * draw01;
            drawTickSource.PlayOneShot(_drawTickClip, volume * (0.35f + 0.4f * draw01));
        }
    }

    private void HandleArrowReleased(ArrowReleasedInfo info)
    {
        PlayOnBow(_releaseClip, 0.6f + 0.4f * info.draw01);
    }

    private void HandleArrowHit(ArrowHitInfo info)
    {
        PlayAtPoint(_hitThudClip, info.hitPoint, 0.9f);

        if (info.score > 0 && _ringChimeClips != null)
        {
            var chimeIndex = Mathf.Clamp(info.score / 2 - 1, 0, _ringChimeClips.Length - 1);
            PlayAtPoint(_ringChimeClips[chimeIndex], info.hitPoint, 0.85f);
        }

        if (info.score >= ArcheryGeometry.TargetMaxRingScore)
        {
            PlayAtPoint(_goldClip, info.hitPoint, 0.9f);
        }
    }

    private void HandleArrowMissed(ArrowMissedInfo info)
    {
        var position = info.position;
        position.y = Mathf.Max(0f, position.y);
        PlayAtPoint(_missClip, position, 0.55f);
    }

    private void HandleRoundFinished(ArcheryRoundResult result)
    {
        PlayOnBow(_roundEndClip, 0.95f);
        if (result.isNewBest)
        {
            PlayOnBow(_goldClip, 0.95f);
        }
    }

    private void PlayOnBow(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        if (bowSource != null && bowSource.isActiveAndEnabled)
        {
            bowSource.PlayOneShot(clip, volume * volumeScale);
        }
    }

    private void PlayAtPoint(AudioClip clip, Vector3 position, float volumeScale)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, volume * volumeScale);
    }
}

public static class ArcheryAudioSynth
{
    public const int SampleRate = 22050;

    public static AudioClip CreateNockClick()
    {
        var random = new System.Random(11);
        return Create("ArcheryNockClick", 0.035f, t =>
            (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 150f) * 0.4f +
            Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 190f) * 0.35f);
    }

    public static AudioClip CreateDrawTick()
    {
        return Create("ArcheryDrawTick", 0.022f, t =>
            Mathf.Sin(2f * Mathf.PI * 240f * t) * Mathf.Exp(-t * 180f) * 0.5f);
    }

    public static AudioClip CreateReleaseTwang()
    {
        var random = new System.Random(23);
        const float duration = 0.42f;
        return Create("ArcheryReleaseTwang", duration, t =>
        {
            var f = 140f * (1f - 0.12f * (t / duration));
            var body =
                Mathf.Sin(2f * Mathf.PI * f * t) * 0.62f +
                Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.24f +
                Mathf.Sin(2f * Mathf.PI * f * 3.1f * t) * 0.1f;
            var snap = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 70f) * 0.3f;
            return body * Mathf.Exp(-t * 9f) + snap;
        });
    }

    public static AudioClip CreateHitThud()
    {
        var random = new System.Random(37);
        var walk = 0f;
        return Create("ArcheryHitThud", 0.15f, t =>
        {
            walk = Mathf.Clamp(walk + (float)(random.NextDouble() * 2.0 - 1.0) * 0.28f, -1f, 1f);
            var body = Mathf.Sin(2f * Mathf.PI * 72f * t) * Mathf.Exp(-t * 26f) * 0.7f;
            return body + walk * Mathf.Exp(-t * 34f) * 0.4f;
        });
    }

    public static AudioClip CreateMissThud()
    {
        return Create("ArcheryMissThud", 0.13f, t =>
            Mathf.Sin(2f * Mathf.PI * 96f * t) * Mathf.Exp(-t * 30f) * 0.55f);
    }

    public static AudioClip CreateRingChime(float baseFrequency, string name)
    {
        return Create(name, 0.55f, t =>
            (Mathf.Sin(2f * Mathf.PI * baseFrequency * t) * 0.55f +
             Mathf.Sin(2f * Mathf.PI * baseFrequency * 2f * t) * 0.22f +
             Mathf.Sin(2f * Mathf.PI * baseFrequency * 3f * t) * 0.1f) * Mathf.Exp(-t * 5.5f));
    }

    public static AudioClip CreateGoldFanfare()
    {
        return Create("ArcheryGoldFanfare", 0.7f, t =>
            Note(t, 0f, 659.25f, 0.32f) +
            Note(t, 0.14f, 783.99f, 0.32f) +
            Note(t, 0.28f, 987.77f, 0.4f));
    }

    public static AudioClip CreateRoundEndArpeggio()
    {
        return Create("ArcheryRoundEnd", 0.85f, t =>
            Note(t, 0f, 523.25f, 0.4f) +
            Note(t, 0.18f, 659.25f, 0.4f) +
            Note(t, 0.36f, 783.99f, 0.48f));
    }

    private static float Note(float t, float startSeconds, float frequency, float durationSeconds)
    {
        var local = t - startSeconds;
        if (local < 0f || local > durationSeconds) return 0f;

        return (Mathf.Sin(2f * Mathf.PI * frequency * local) * 0.4f +
                Mathf.Sin(2f * Mathf.PI * frequency * 2f * local) * 0.12f) * Mathf.Exp(-local * 7f);
    }

    private static AudioClip Create(string name, float seconds, System.Func<float, float> sampler)
    {
        var sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * seconds));
        var data = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            data[i] = Mathf.Clamp(sampler(i / (float)SampleRate), -1f, 1f);
        }

        var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
