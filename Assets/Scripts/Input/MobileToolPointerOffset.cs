using TMPro;
using UnityEngine;

public static class MobileToolPointerOffset
{
    private static readonly Vector2 CanvasOffset =
        new Vector2(0f, 110f);

    public static Vector2 Apply(
        TMP_Text referenceText,
        Vector2 rawScreenPosition)
    {
        if (!Application.isMobilePlatform)
        {
            return rawScreenPosition;
        }

        return rawScreenPosition +
               ToolInfluenceScale.ToScreenPixels(
                   referenceText,
                   CanvasOffset
               );
    }

    public static Vector2 Apply(
        RectTransform referenceRect,
        Vector2 rawScreenPosition)
    {
        if (!Application.isMobilePlatform ||
            referenceRect == null)
        {
            return rawScreenPosition;
        }

        Canvas canvas =
            referenceRect.GetComponentInParent<Canvas>();

        float scale =
            canvas != null
                ? canvas.rootCanvas.scaleFactor
                : 1f;

        return rawScreenPosition +
               CanvasOffset * scale;
    }
}