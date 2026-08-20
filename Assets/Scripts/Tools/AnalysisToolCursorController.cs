using UnityEngine;

public sealed class AnalysisToolCursorController : MonoBehaviour
{
    [Header("Инструменты")]
    [SerializeField] private UltravioletTool ultravioletTool;
    [SerializeField] private MagnifierTool magnifierTool;
    [SerializeField] private PencilTool pencilTool;

    [Header("Предметы на столе")]
    [SerializeField] private CanvasGroup deskUltraviolet;
    [SerializeField] private CanvasGroup deskMagnifier;

    private bool lastUvActive;
    private bool lastMagnifierActive;
    private bool stateInitialized;

    private void Awake()
    {
        if (!ValidateReferences())
            enabled = false;
    }

    private void OnEnable()
    {
        stateInitialized = false;
    }

    private void Update()
    {
        bool uvActive = ultravioletTool.IsActive;
        bool magnifierActive = magnifierTool.IsActive;

        if (!stateInitialized || uvActive != lastUvActive)
        {
            SetDeskItemVisible(deskUltraviolet, !uvActive);
            lastUvActive = uvActive;
        }

        if (!stateInitialized || magnifierActive != lastMagnifierActive)
        {
            SetDeskItemVisible(deskMagnifier, !magnifierActive);
            lastMagnifierActive = magnifierActive;
        }

        stateInitialized = true;
    }

    private void LateUpdate()
    {
        bool anyCursorToolActive =
            ultravioletTool.IsActive ||
            magnifierTool.IsActive ||
            (pencilTool != null && pencilTool.IsActive);

        Cursor.visible = !anyCursorToolActive;
    }

    private void SetDeskItemVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (ultravioletTool == null)
        {
            Debug.LogError(
                "AnalysisToolCursorController: не назначен Ultraviolet Tool.",
                this
            );
            valid = false;
        }

        if (magnifierTool == null)
        {
            Debug.LogError(
                "AnalysisToolCursorController: не назначен Magnifier Tool.",
                this
            );
            valid = false;
        }

        if (deskUltraviolet == null)
        {
            Debug.LogError(
                "AnalysisToolCursorController: не назначен Desk Ultraviolet.",
                this
            );
            valid = false;
        }

        if (deskMagnifier == null)
        {
            Debug.LogError(
                "AnalysisToolCursorController: не назначен Desk Magnifier.",
                this
            );
            valid = false;
        }

        return valid;
    }

    private void OnDisable()
    {
        SetDeskItemVisible(deskUltraviolet, true);
        SetDeskItemVisible(deskMagnifier, true);
        Cursor.visible = true;
    }
}
