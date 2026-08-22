using UnityEngine;

/// <summary>
/// Центральный проигрыватель коротких звуковых эффектов в текущей сцене.
/// Не переживает смену сцен: в MainMenu и GameScene может быть свой экземпляр.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "SfxPlayer: в сцене найден второй экземпляр. Он будет отключён.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void Play(
        AudioClip clip,
        float volumeScale = 1f)
    {
        if (clip == null ||
            Instance == null ||
            Instance.audioSource == null)
        {
            return;
        }

        Instance.audioSource.PlayOneShot(
            clip,
            Mathf.Clamp01(volumeScale)
        );
    }
}
