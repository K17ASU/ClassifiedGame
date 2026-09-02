using UnityEngine;

/// <summary>
/// Mobile-only логика возврата аналитических инструментов на стол.
///
/// Если активный инструмент перетащить пальцем обратно
/// на его физическое место и отпустить, инструмент выключается.
///
/// Обычный тап по инструменту не считается "возвратом":
/// это оставлено стандартному Button.onClick.
/// </summary>
public sealed class MobileToolReturnController :
    MonoBehaviour
{
    [Header("UV")]

    [SerializeField]
    private UltravioletTool ultravioletTool;

    [SerializeField]
    private RectTransform ultravioletReturnArea;

    [Header("Лупа")]

    [SerializeField]
    private MagnifierTool magnifierTool;

    [SerializeField]
    private RectTransform magnifierReturnArea;

    [Header("Декодер")]

    [SerializeField]
    private DecoderTool decoderTool;

    [SerializeField]
    private RectTransform decoderReturnArea;

    private Vector2 touchStartPosition;
    private bool hasTouchStart;

    private void Update()
    {
        if (ScreenPointer.TouchWasPressedThisFrame &&
            ScreenPointer.TryGetTouchPosition(
                out Vector2 startPosition))
        {
            touchStartPosition =
                startPosition;

            hasTouchStart =
                true;
        }

        if (!ScreenPointer.TouchWasReleasedThisFrame)
        {
            return;
        }

        if (!ScreenPointer.TryGetTouchPosition(
                out Vector2 releasePosition))
        {
            hasTouchStart =
                false;

            return;
        }

        TryReturnUltraviolet(
            releasePosition
        );

        TryReturnMagnifier(
            releasePosition
        );

        TryReturnDecoder(
            releasePosition
        );

        hasTouchStart =
            false;
    }

    private void TryReturnUltraviolet(
        Vector2 releasePosition)
    {
        if (ultravioletTool == null ||
            !ultravioletTool.IsActive ||
            ultravioletReturnArea == null)
        {
            return;
        }

        if (!IsInside(
                ultravioletReturnArea,
                releasePosition))
        {
            return;
        }

        if (TouchStartedInside(
                ultravioletReturnArea))
        {
            return;
        }

        ultravioletTool.DisableMode();
    }

    private void TryReturnMagnifier(
        Vector2 releasePosition)
    {
        if (magnifierTool == null ||
            !magnifierTool.IsActive ||
            magnifierReturnArea == null)
        {
            return;
        }

        if (!IsInside(
                magnifierReturnArea,
                releasePosition))
        {
            return;
        }

        if (TouchStartedInside(
                magnifierReturnArea))
        {
            return;
        }

        magnifierTool.DisableMode();
    }

    private void TryReturnDecoder(
        Vector2 releasePosition)
    {
        if (decoderTool == null ||
            !decoderTool.IsActive ||
            decoderReturnArea == null)
        {
            return;
        }

        if (!IsInside(
                decoderReturnArea,
                releasePosition))
        {
            return;
        }

        if (TouchStartedInside(
                decoderReturnArea))
        {
            return;
        }

        decoderTool.DisableMode();
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
        if (area == null)
        {
            return false;
        }

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
}
