using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Проигрывает музыкальные треки в случайном порядке без повторов
/// и плавно сводит соседние треки через crossfade.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class RandomMusicPlayer : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField]
    private List<AudioClip> tracks = new List<AudioClip>();

    [Header("Playback")]
    [SerializeField]
    [Range(0f, 1f)]
    private float volume = 0.5f;

    [SerializeField]
    [Min(0.1f)]
    private float crossfadeDuration = 2f;

    private AudioSource currentSource;
    private AudioSource nextSource;

    private readonly List<int> playlist =
        new List<int>();

    private int playlistPosition;
    private int lastPlayedTrack = -1;
    private bool isCrossfading;

    private void Awake()
    {
        currentSource =
            GetComponent<AudioSource>();

        ConfigureSource(currentSource);

        nextSource =
            gameObject.AddComponent<AudioSource>();

        CopySourceSettings(
            currentSource,
            nextSource
        );

        ConfigureSource(nextSource);
    }

    private void Start()
    {
        if (!HasValidTracks())
        {
            Debug.LogWarning(
                "RandomMusicPlayer: список Tracks пуст или содержит только пустые элементы.",
                this
            );

            return;
        }

        CreateShuffledPlaylist();
        PlayFirstTrack();
    }

    private void Update()
    {
        if (isCrossfading ||
            currentSource == null ||
            currentSource.clip == null)
        {
            return;
        }

        if (!currentSource.isPlaying)
        {
            PlayNextImmediately();
            return;
        }

        float remainingTime =
            currentSource.clip.length -
            currentSource.time;

        if (remainingTime <=
            crossfadeDuration)
        {
            StartCoroutine(
                CrossfadeToNextTrack()
            );
        }
    }

    private void ConfigureSource(
        AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    private void CopySourceSettings(
        AudioSource source,
        AudioSource destination)
    {
        destination.outputAudioMixerGroup =
            source.outputAudioMixerGroup;

        destination.mute =
            source.mute;

        destination.bypassEffects =
            source.bypassEffects;

        destination.bypassListenerEffects =
            source.bypassListenerEffects;

        destination.bypassReverbZones =
            source.bypassReverbZones;

        destination.priority =
            source.priority;

        destination.panStereo =
            source.panStereo;

        destination.spatialBlend =
            source.spatialBlend;

        destination.reverbZoneMix =
            source.reverbZoneMix;
    }

    private bool HasValidTracks()
    {
        for (int i = 0;
             i < tracks.Count;
             i++)
        {
            if (tracks[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateShuffledPlaylist()
    {
        playlist.Clear();

        for (int i = 0;
             i < tracks.Count;
             i++)
        {
            if (tracks[i] != null)
            {
                playlist.Add(i);
            }
        }

        for (int i = playlist.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(0, i + 1);

            int temporary =
                playlist[i];

            playlist[i] =
                playlist[randomIndex];

            playlist[randomIndex] =
                temporary;
        }

        if (playlist.Count > 1 &&
            playlist[0] ==
            lastPlayedTrack)
        {
            int swapIndex =
                Random.Range(
                    1,
                    playlist.Count
                );

            int temporary =
                playlist[0];

            playlist[0] =
                playlist[swapIndex];

            playlist[swapIndex] =
                temporary;
        }

        playlistPosition = 0;
    }

    private AudioClip GetNextTrack()
    {
        if (playlist.Count == 0 ||
            playlistPosition >=
            playlist.Count)
        {
            CreateShuffledPlaylist();
        }

        if (playlist.Count == 0)
        {
            return null;
        }

        int trackIndex =
            playlist[
                playlistPosition
            ];

        playlistPosition++;

        lastPlayedTrack =
            trackIndex;

        return tracks[trackIndex];
    }

    private void PlayFirstTrack()
    {
        AudioClip clip =
            GetNextTrack();

        if (clip == null)
        {
            return;
        }

        currentSource.clip =
            clip;

        currentSource.volume =
            volume;

        currentSource.Play();
    }

    private void PlayNextImmediately()
    {
        AudioClip clip =
            GetNextTrack();

        if (clip == null)
        {
            return;
        }

        currentSource.clip =
            clip;

        currentSource.volume =
            volume;

        currentSource.Play();
    }

    private IEnumerator CrossfadeToNextTrack()
    {
        isCrossfading = true;

        AudioClip nextClip =
            GetNextTrack();

        if (nextClip == null)
        {
            isCrossfading = false;
            yield break;
        }

        nextSource.clip =
            nextClip;

        nextSource.volume = 0f;
        nextSource.Play();

        float elapsedTime = 0f;

        float fadeDuration =
            Mathf.Max(
                0.1f,
                crossfadeDuration
            );

        while (elapsedTime <
               fadeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsedTime /
                    fadeDuration
                );

            currentSource.volume =
                Mathf.Lerp(
                    volume,
                    0f,
                    t
                );

            nextSource.volume =
                Mathf.Lerp(
                    0f,
                    volume,
                    t
                );

            yield return null;
        }

        currentSource.Stop();
        currentSource.clip = null;
        currentSource.volume = 0f;

        AudioSource oldSource =
            currentSource;

        currentSource =
            nextSource;

        nextSource =
            oldSource;

        currentSource.volume =
            volume;

        isCrossfading = false;
    }
}
