using UnityEngine;

/// <summary>
/// Ограничивает RectTransform безопасной областью экрана.
/// Фоновые изображения следует оставлять вне этого объекта,
/// чтобы они продолжали заполнять весь экран.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;

    private Rect lastSafeArea;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        Rect safeArea =
            Screen.safeArea;

        if (safeArea != lastSafeArea ||
            Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }

        if (Screen.width <= 0 ||
            Screen.height <= 0)
        {
            return;
        }

        Rect safeArea =
            Screen.safeArea;

        Vector2 anchorMin =
            safeArea.position;

        Vector2 anchorMax =
            safeArea.position +
            safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;

        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin =
            anchorMin;

        rectTransform.anchorMax =
            anchorMax;

        rectTransform.offsetMin =
            Vector2.zero;

        rectTransform.offsetMax =
            Vector2.zero;

        lastSafeArea =
            safeArea;

        lastScreenWidth =
            Screen.width;

        lastScreenHeight =
            Screen.height;
    }
}
