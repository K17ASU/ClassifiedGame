using UnityEngine;

/// <summary>
/// Управляет визуальным состоянием физической UV-лампы:
/// скрывает лампу на столе, пока UV-инструмент активен,
/// и управляет видимостью системного курсора.
///
/// Само перемещение UV-курсора и выявление текста
/// остаются в UltravioletTool.
/// </summary>
public sealed class UltravioletCursorController : MonoBehaviour
{
    [Header("Связи")]

    [SerializeField]
    private UltravioletTool ultravioletTool;

    [SerializeField]
    private PencilTool pencilTool;

    [SerializeField]
    private CanvasGroup deskUltraviolet;

    private bool lastUvActive;
    private bool stateInitialized;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        stateInitialized = false;
    }

    private void Update()
    {
        bool isUvActive = ultravioletTool.IsActive;

        if (!stateInitialized ||
            isUvActive != lastUvActive)
        {
            ApplyDeskUltravioletState(isUvActive);

            lastUvActive = isUvActive;
            stateInitialized = true;
        }
    }

    private void LateUpdate()
    {
        bool isAnyCursorToolActive =
            ultravioletTool.IsActive ||
            (pencilTool != null && pencilTool.IsActive);

        Cursor.visible = !isAnyCursorToolActive;
    }

    private void ApplyDeskUltravioletState(bool isUvActive)
    {
        if (deskUltraviolet == null)
        {
            return;
        }

        deskUltraviolet.alpha =
            isUvActive ? 0f : 1f;

        deskUltraviolet.interactable =
            !isUvActive;

        deskUltraviolet.blocksRaycasts =
            !isUvActive;
    }

    private bool ValidateReferences()
    {
        bool referencesAreValid = true;

        if (ultravioletTool == null)
        {
            Debug.LogError(
                "UltravioletCursorController: не назначен Ultraviolet Tool.",
                this
            );

            referencesAreValid = false;
        }

        if (deskUltraviolet == null)
        {
            Debug.LogError(
                "UltravioletCursorController: не назначен Desk Ultraviolet.",
                this
            );

            referencesAreValid = false;
        }

        if (pencilTool == null)
        {
            Debug.LogWarning(
                "UltravioletCursorController: не назначен Pencil Tool. " +
                "UV будет работать, но общий системный курсор " +
                "не сможет учитывать активный карандаш.",
                this
            );
        }

        return referencesAreValid;
    }

    private void OnDisable()
    {
        if (deskUltraviolet != null)
        {
            deskUltraviolet.alpha = 1f;
            deskUltraviolet.interactable = true;
            deskUltraviolet.blocksRaycasts = true;
        }

        Cursor.visible = true;
    }
}
