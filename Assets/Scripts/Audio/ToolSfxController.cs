using UnityEngine;

/// <summary>
/// Следит за фактическим состоянием инструментов
/// и проигрывает SFX при их включении и выключении.
/// Работает и для клика, и для RMB, и для переключения
/// с одного инструмента на другой.
/// </summary>
public sealed class ToolSfxController : MonoBehaviour
{
    [Header("Tools")]

    [SerializeField]
    private UltravioletTool ultravioletTool;

    [SerializeField]
    private MagnifierTool magnifierTool;

    [SerializeField]
    private DecoderTool decoderTool;

    [SerializeField]
    private PencilTool pencilTool;

    [Header("Ultraviolet")]

    [SerializeField]
    private AudioClip ultravioletOnSound;

    [SerializeField]
    private AudioClip ultravioletOffSound;

    [Header("Magnifier")]

    [SerializeField]
    private AudioClip magnifierOnSound;

    [SerializeField]
    private AudioClip magnifierOffSound;

    [Header("Decoder")]

    [SerializeField]
    private AudioClip decoderOnSound;

    [SerializeField]
    private AudioClip decoderOffSound;

    [Header("Pencil")]

    [SerializeField]
    private AudioClip pencilOnSound;

    [SerializeField]
    private AudioClip pencilOffSound;

    [Header("Volume")]

    [SerializeField]
    [Range(0f, 1f)]
    private float soundVolume = 0.8f;

    private bool previousUltravioletState;
    private bool previousMagnifierState;
    private bool previousDecoderState;
    private bool previousPencilState;

    private bool initialized;

    private void Start()
    {
        previousUltravioletState =
            ultravioletTool != null &&
            ultravioletTool.IsActive;

        previousMagnifierState =
            magnifierTool != null &&
            magnifierTool.IsActive;

        previousDecoderState =
            decoderTool != null &&
            decoderTool.IsActive;

        previousPencilState =
            pencilTool != null &&
            pencilTool.IsActive;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        CheckState(
            ultravioletTool != null &&
            ultravioletTool.IsActive,
            ref previousUltravioletState,
            ultravioletOnSound,
            ultravioletOffSound
        );

        CheckState(
            magnifierTool != null &&
            magnifierTool.IsActive,
            ref previousMagnifierState,
            magnifierOnSound,
            magnifierOffSound
        );

        CheckState(
            decoderTool != null &&
            decoderTool.IsActive,
            ref previousDecoderState,
            decoderOnSound,
            decoderOffSound
        );

        CheckState(
            pencilTool != null &&
            pencilTool.IsActive,
            ref previousPencilState,
            pencilOnSound,
            pencilOffSound
        );
    }

    private void CheckState(
        bool currentState,
        ref bool previousState,
        AudioClip onSound,
        AudioClip offSound)
    {
        if (currentState == previousState)
        {
            return;
        }

        SfxPlayer.Play(
            currentState
                ? onSound
                : offSound,
            soundVolume
        );

        previousState = currentState;
    }
}
