using UnityEngine;

/// <summary>
/// Звуки действий непосредственно с документом.
/// Использует несколько вариантов клипов и ограничивает частоту,
/// чтобы при протягивании по словам звуки не накладывались слишком плотно.
/// </summary>
public sealed class DocumentSfxController : MonoBehaviour
{
    public static DocumentSfxController Instance { get; private set; }

    [Header("Redaction")]

    [SerializeField]
    private AudioClip[] redactSounds;

    [SerializeField]
    private AudioClip[] unredactSounds;

    [Header("Pencil")]

    [SerializeField]
    private AudioClip[] pencilMarkSounds;

    [SerializeField]
    private AudioClip[] pencilEraseSounds;

    [Header("Playback")]

    [SerializeField]
    [Range(0f, 1f)]
    private float soundVolume = 0.7f;

    [SerializeField]
    [Min(0f)]
    private float minimumInterval = 0.06f;

    private float lastPlayTime = -100f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "DocumentSfxController: в сцене найден второй экземпляр. Он будет отключён.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void PlayRedaction(bool applying)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayRandom(
            applying
                ? Instance.redactSounds
                : Instance.unredactSounds
        );
    }

    public static void PlayPencil(bool applying)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayRandom(
            applying
                ? Instance.pencilMarkSounds
                : Instance.pencilEraseSounds
        );
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null ||
            clips.Length == 0)
        {
            return;
        }

        if (Time.unscaledTime - lastPlayTime <
            minimumInterval)
        {
            return;
        }

        AudioClip clip = GetRandomValidClip(clips);

        if (clip == null)
        {
            return;
        }

        lastPlayTime = Time.unscaledTime;

        SfxPlayer.Play(
            clip,
            soundVolume
        );
    }

    private AudioClip GetRandomValidClip(
        AudioClip[] clips)
    {
        int validCount = 0;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int target =
            Random.Range(0, validCount);

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (target == 0)
            {
                return clips[i];
            }

            target--;
        }

        return null;
    }
}
