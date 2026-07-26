using UnityEngine;

public class DartsAudioManager : MonoBehaviour
{
    public AudioSource handSource;
    [Range(0f, 1f)] public float volume = 0.85f;

    private AudioClip _grabClip;
    private AudioClip _throwClip;
    private AudioClip _hitThudClip;
    private AudioClip _missClip;
    private AudioClip _goldClip;
    private AudioClip _roundEndClip;
    private AudioClip[] _ringChimeClips;

    private void Awake()
    {
        // 程序合成音效全部复用射箭的合成器，零音频资源依赖。
        _grabClip = ArcheryAudioSynth.CreateNockClick();
        _throwClip = ArcheryAudioSynth.CreateThrowWhoosh();
        _hitThudClip = ArcheryAudioSynth.CreateHitThud();
        _missClip = ArcheryAudioSynth.CreateMissThud();
        _goldClip = ArcheryAudioSynth.CreateGoldFanfare();
        _roundEndClip = ArcheryAudioSynth.CreateRoundEndArpeggio();

        var chimeFrequencies = new[] { 392f, 440f, 494f, 523f, 587f };
        _ringChimeClips = new AudioClip[chimeFrequencies.Length];
        for (var i = 0; i < chimeFrequencies.Length; i++)
        {
            _ringChimeClips[i] = ArcheryAudioSynth.CreateRingChime(chimeFrequencies[i], $"DartsChime_{i}");
        }
    }

    private void OnEnable()
    {
        DartsEvents.OnDartGrabbed += HandleDartGrabbed;
        DartsEvents.OnDartThrown += HandleDartThrown;
        DartsEvents.OnDartHit += HandleDartHit;
        DartsEvents.OnDartMissed += HandleDartMissed;
        DartsEvents.OnRoundFinished += HandleRoundFinished;
    }

    private void OnDisable()
    {
        DartsEvents.OnDartGrabbed -= HandleDartGrabbed;
        DartsEvents.OnDartThrown -= HandleDartThrown;
        DartsEvents.OnDartHit -= HandleDartHit;
        DartsEvents.OnDartMissed -= HandleDartMissed;
        DartsEvents.OnRoundFinished -= HandleRoundFinished;
    }

    private void HandleDartGrabbed()
    {
        PlayOnHand(_grabClip, 0.8f);
    }

    private void HandleDartThrown(DartThrownInfo info)
    {
        PlayOnHand(_throwClip, 0.7f + 0.3f * Mathf.InverseLerp(
            DartsGeometry.MinDartSpeedMetersPerSecond,
            DartsGeometry.MaxDartSpeedMetersPerSecond,
            info.Speed));
    }

    private void HandleDartHit(DartHitInfo info)
    {
        PlayAtPoint(_hitThudClip, info.hitPoint, 0.9f);

        if (info.score > 0 && _ringChimeClips != null)
        {
            var chimeIndex = Mathf.Clamp(info.score / 2 - 1, 0, _ringChimeClips.Length - 1);
            PlayAtPoint(_ringChimeClips[chimeIndex], info.hitPoint, 0.85f);
        }

        if (info.score >= DartsGeometry.BoardMaxRingScore)
        {
            PlayAtPoint(_goldClip, info.hitPoint, 0.9f);
        }
    }

    private void HandleDartMissed(DartMissedInfo info)
    {
        var position = info.position;
        position.y = Mathf.Max(0f, position.y);
        PlayAtPoint(_missClip, position, 0.55f);
    }

    private void HandleRoundFinished(DartsRoundResult result)
    {
        PlayOnHand(_roundEndClip, 0.95f);
        if (result.isNewBest)
        {
            PlayOnHand(_goldClip, 0.95f);
        }
    }

    private void PlayOnHand(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        if (handSource != null && handSource.isActiveAndEnabled)
        {
            handSource.PlayOneShot(clip, volume * volumeScale);
        }
    }

    private void PlayAtPoint(AudioClip clip, Vector3 position, float volumeScale)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, position, volume * volumeScale);
    }
}
