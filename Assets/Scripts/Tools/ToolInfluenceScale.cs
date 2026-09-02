using TMPro;
using UnityEngine;

/// <summary>
/// Конвертирует размеры, заданные в единицах Canvas
/// (относительно Reference Resolution),
/// в реальные экранные пиксели.
/// </summary>
public static class ToolInfluenceScale
{
    public static float GetScreenScale(
        TMP_Text referenceText)
    {
        if (referenceText == null ||
            referenceText.canvas == null)
        {
            return 1f;
        }

        Canvas canvas =
            referenceText.canvas.rootCanvas != null
                ? referenceText.canvas.rootCanvas
                : referenceText.canvas;

        return Mathf.Max(
            canvas.scaleFactor,
            0.0001f
        );
    }

    public static float ToScreenPixels(
        TMP_Text referenceText,
        float canvasUnits)
    {
        return canvasUnits *
               GetScreenScale(referenceText);
    }

    public static Vector2 ToScreenPixels(
        TMP_Text referenceText,
        Vector2 canvasUnits)
    {
        return canvasUnits *
               GetScreenScale(referenceText);
    }
}
