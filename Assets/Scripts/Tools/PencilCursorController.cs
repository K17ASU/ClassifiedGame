using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Визуально заменяет системный курсор на карандаш,
/// пока PencilTool активен.
/// На мобильных устройствах визуальный карандаш не показывается.
/// Саму механику карандаша не изменяет.
/// </summary>
public sealed class PencilCursorController : MonoBehaviour
{
    [Header("Связи")]

    [SerializeField]
    private PencilTool pencilTool;

    [SerializeField]
    private GameObject deskPencil;

    [SerializeField]
    private RectTransform cursorOverlay;

    [SerializeField]
    private RectTransform pencilCursorRoot;

    private Canvas rootCanvas;
    private bool lastActiveState;
    private bool stateInitialized;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        rootCanvas = cursorOverlay.GetComponentInParent<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogError(
                "PencilCursorController: Cursor Overlay должен находиться внутри Canvas.",
                this
            );

            enabled = false;
        }
    }

    private void OnEnable()
    {
        stateInitialized = false;
    }

    private void Update()
    {
        if (pencilTool == null)
        {
            return;
        }

        bool isPencilActive =
            pencilTool.IsActive;

        if (!stateInitialized ||
            isPencilActive != lastActiveState)
        {
            ApplyCursorState(
                isPencilActive
            );
        }

        if (isPencilActive &&
            !Application.isMobilePlatform)
        {
            UpdateCursorPosition();
        }
    }

    private bool ValidateReferences()
    {
        bool referencesAreValid = true;

        if (pencilTool == null)
        {
            Debug.LogError(
                "PencilCursorController: не назначен Pencil Tool.",
                this
            );

            referencesAreValid = false;
        }

        if (deskPencil == null)
        {
            Debug.LogError(
                "PencilCursorController: не назначен Desk Pencil.",
                this
            );

            referencesAreValid = false;
        }

        if (cursorOverlay == null)
        {
            Debug.LogError(
                "PencilCursorController: не назначен Cursor Overlay.",
                this
            );

            referencesAreValid = false;
        }

        if (pencilCursorRoot == null)
        {
            Debug.LogError(
                "PencilCursorController: не назначен Pencil Cursor Root.",
                this
            );

            referencesAreValid = false;
        }

        return referencesAreValid;
    }

    private void ApplyCursorState(
        bool isPencilActive)
    {
        lastActiveState =
            isPencilActive;

        stateInitialized =
            true;

        // Физический карандаш на столе исчезает,
        // когда игрок его взял, на обеих платформах.
        deskPencil.SetActive(
            !isPencilActive
        );

        // На мобильном устройстве большой карандаш-курсор
        // вообще не показываем.
        bool showPencilCursor =
            isPencilActive &&
            !Application.isMobilePlatform;

        pencilCursorRoot.gameObject.SetActive(
            showPencilCursor
        );

        if (!Application.isMobilePlatform)
        {
            Cursor.visible =
                !isPencilActive;
        }
        else
        {
            Cursor.visible = true;
        }

        if (showPencilCursor)
        {
            UpdateCursorPosition();
        }
    }

    private void UpdateCursorPosition()
    {
        if (Mouse.current == null)
        {
            return;
        }

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Camera eventCamera =
            null;

        if (rootCanvas != null &&
            rootCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            eventCamera =
                rootCanvas.worldCamera;
        }

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                cursorOverlay,
                screenPosition,
                eventCamera,
                out Vector2 localPosition))
        {
            pencilCursorRoot.anchoredPosition =
                localPosition;
        }
    }

    private void OnDisable()
    {
        Cursor.visible = true;

        if (pencilCursorRoot != null)
        {
            pencilCursorRoot.gameObject.SetActive(
                false
            );
        }
    }
}
