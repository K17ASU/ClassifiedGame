using UnityEngine;

/// <summary>
/// Управляет мобильным возвратом инструментов на стол.
///
/// Return Zone — это отдельный GameObject с RectTransform + Image.
/// В сцене Return Zone можно держать выключенными.
/// На мобильном устройстве нужная зона автоматически включается,
/// пока соответствующий инструмент активен.
/// На PC зоны всегда выключены.
/// </summary>
public sealed class MobileToolReturnController : MonoBehaviour
{
    private enum ActiveTool
    {
        None,
        Ultraviolet,
        Magnifier,
        Decoder,
        Pencil
    }

    [Header("UV")]
    [SerializeField] private UltravioletTool ultravioletTool;
    [SerializeField] private RectTransform ultravioletReturnArea;

    [Header("Лупа")]
    [SerializeField] private MagnifierTool magnifierTool;
    [SerializeField] private RectTransform magnifierReturnArea;

    [Header("Декодер")]
    [SerializeField] private DecoderTool decoderTool;
    [SerializeField] private RectTransform decoderReturnArea;

    [Header("Карандаш")]
    [SerializeField] private PencilTool pencilTool;
    [SerializeField] private RectTransform pencilReturnArea;

    private Vector2 touchStartPosition;
    private bool hasTouchStart;

    private ActiveTool activeToolAtTouchStart =
        ActiveTool.None;

    private void Awake()
    {
        HideAllReturnAreas();
    }

    private void Update()
    {
        RefreshReturnAreas();

        if (ScreenPointer.TouchWasPressedThisFrame &&
            ScreenPointer.TryGetTouchPosition(
                out Vector2 startPosition))
        {
            touchStartPosition =
                startPosition;

            hasTouchStart =
                true;

            activeToolAtTouchStart =
                GetActiveTool();
        }

        if (!ScreenPointer.TouchWasReleasedThisFrame)
        {
            return;
        }

        if (!ScreenPointer.TryGetTouchPosition(
                out Vector2 releasePosition))
        {
            ResetTouchTracking();
            return;
        }

        Vector2 ultravioletReleasePosition =
    MobileToolPointerOffset.Apply(
        ultravioletReturnArea,
        releasePosition
    );

        Vector2 magnifierReleasePosition =
            MobileToolPointerOffset.Apply(
                magnifierReturnArea,
                releasePosition
            );

        Vector2 decoderReleasePosition =
            MobileToolPointerOffset.Apply(
                decoderReturnArea,
                releasePosition
            );



        TryReturnUltraviolet(
            ultravioletReleasePosition
        );

        TryReturnMagnifier(
            magnifierReleasePosition
        );

        TryReturnDecoder(
            decoderReleasePosition
        );
        TryReturnPencil(
            releasePosition
        );

        ResetTouchTracking();
        RefreshReturnAreas();
    }

    private void RefreshReturnAreas()
    {
        bool mobile =
            Application.isMobilePlatform;

        SetReturnAreaVisible(
            ultravioletReturnArea,
            mobile &&
            ultravioletTool != null &&
            ultravioletTool.IsActive
        );

        SetReturnAreaVisible(
            magnifierReturnArea,
            mobile &&
            magnifierTool != null &&
            magnifierTool.IsActive
        );

        SetReturnAreaVisible(
            decoderReturnArea,
            mobile &&
            decoderTool != null &&
            decoderTool.IsActive
        );

        SetReturnAreaVisible(
            pencilReturnArea,
            mobile &&
            pencilTool != null &&
            pencilTool.IsActive
        );
    }

    private void HideAllReturnAreas()
    {
        SetReturnAreaVisible(ultravioletReturnArea, false);
        SetReturnAreaVisible(magnifierReturnArea, false);
        SetReturnAreaVisible(decoderReturnArea, false);
        SetReturnAreaVisible(pencilReturnArea, false);
    }

    private void SetReturnAreaVisible(
        RectTransform returnArea,
        bool visible)
    {
        if (returnArea == null)
        {
            return;
        }

        if (returnArea.gameObject.activeSelf != visible)
        {
            returnArea.gameObject.SetActive(
                visible
            );
        }
    }

    private void TryReturnUltraviolet(
        Vector2 releasePosition)
    {
        if (ultravioletTool == null ||
            !ultravioletTool.IsActive)
        {
            return;
        }

        if (!ShouldReturnTool(
                ActiveTool.Ultraviolet,
                ultravioletReturnArea,
                releasePosition))
        {
            return;
        }

        ultravioletTool.DisableMode();
    }

    private void TryReturnMagnifier(
        Vector2 releasePosition)
    {
        if (magnifierTool == null ||
            !magnifierTool.IsActive)
        {
            return;
        }

        if (!ShouldReturnTool(
                ActiveTool.Magnifier,
                magnifierReturnArea,
                releasePosition))
        {
            return;
        }

        magnifierTool.DisableMode();
    }

    private void TryReturnDecoder(
        Vector2 releasePosition)
    {
        if (decoderTool == null ||
            !decoderTool.IsActive)
        {
            return;
        }

        if (!ShouldReturnTool(
                ActiveTool.Decoder,
                decoderReturnArea,
                releasePosition))
        {
            return;
        }

        decoderTool.DisableMode();
    }

    private void TryReturnPencil(
        Vector2 releasePosition)
    {
        if (pencilTool == null ||
            !pencilTool.IsActive)
        {
            return;
        }

        if (!ShouldReturnTool(
                ActiveTool.Pencil,
                pencilReturnArea,
                releasePosition))
        {
            return;
        }

        pencilTool.DisableMode();
    }

    private bool ShouldReturnTool(
        ActiveTool tool,
        RectTransform returnArea,
        Vector2 releasePosition)
    {
        if (returnArea == null ||
            !returnArea.gameObject.activeInHierarchy ||
            !IsInside(
                returnArea,
                releasePosition))
        {
            return false;
        }

        if (activeToolAtTouchStart == tool)
        {
            return true;
        }

        return !TouchStartedInside(
            returnArea
        );
    }

    private ActiveTool GetActiveTool()
    {
        if (ultravioletTool != null &&
            ultravioletTool.IsActive)
        {
            return ActiveTool.Ultraviolet;
        }

        if (magnifierTool != null &&
            magnifierTool.IsActive)
        {
            return ActiveTool.Magnifier;
        }

        if (decoderTool != null &&
            decoderTool.IsActive)
        {
            return ActiveTool.Decoder;
        }

        if (pencilTool != null &&
            pencilTool.IsActive)
        {
            return ActiveTool.Pencil;
        }

        return ActiveTool.None;
    }

    private bool TouchStartedInside(
        RectTransform area)
    {
        return hasTouchStart &&
               IsInside(
                   area,
                   touchStartPosition
               );
    }

    private bool IsInside(
        RectTransform area,
        Vector2 screenPosition)
    {
        Canvas canvas =
            area.GetComponentInParent<Canvas>();

        Camera eventCamera =
            null;

        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            eventCamera =
                canvas.worldCamera;
        }

        return RectTransformUtility
            .RectangleContainsScreenPoint(
                area,
                screenPosition,
                eventCamera
            );
    }

    private void ResetTouchTracking()
    {
        hasTouchStart =
            false;

        activeToolAtTouchStart =
            ActiveTool.None;
    }
}
