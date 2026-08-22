using System.Collections;
using UnityEngine;

public sealed class GameFlowSfxController : MonoBehaviour
{
    public static GameFlowSfxController Instance { get; private set; }

    [Header("Submit")]
    [SerializeField] private AudioClip submitSound;

    [Header("Result")]
    [SerializeField] private AudioClip acceptedSound;
    [SerializeField] private AudioClip failedSound;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float submitVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float resultVolume = 0.8f;
    [SerializeField, Min(0f)] private float resultDelay = 0.25f;

    private Coroutine resultCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "GameFlowSfxController: в сцене найден второй экземпляр. Он будет отключён.",
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

    public static void PlaySubmit()
    {
        if (Instance == null)
        {
            return;
        }

        SfxPlayer.Play(
            Instance.submitSound,
            Instance.submitVolume
        );
    }

    public static void PlayResult(bool accepted)
    {
        if (Instance == null)
        {
            return;
        }

        if (Instance.resultCoroutine != null)
        {
            Instance.StopCoroutine(
                Instance.resultCoroutine
            );
        }

        Instance.resultCoroutine =
            Instance.StartCoroutine(
                Instance.PlayResultAfterDelay(
                    accepted
                )
            );
    }

    private IEnumerator PlayResultAfterDelay(bool accepted)
    {
        if (resultDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                resultDelay
            );
        }

        SfxPlayer.Play(
            accepted ? acceptedSound : failedSound,
            resultVolume
        );

        resultCoroutine = null;
    }
}
