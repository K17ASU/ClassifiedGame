using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public sealed class MobileDocumentPinchZoom : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField, Min(1f)] private float minZoom = 1f;
    [SerializeField, Min(1f)] private float maxZoom = 2f;

    [SerializeField, Range(0.25f, 1.5f)]
    private float zoomSensitivity = 1f;

    [Header("Pan")]
    [SerializeField] private bool enableTwoFingerPan = true;

    private static MobileDocumentPinchZoom activeInstance;

    private RectTransform target;
    private RectTransform parentRect;

    private Vector2 baseAnchoredPosition;
    private Vector3 baseLocalScale;

    private float zoomFactor = 1f;

    private bool gestureActive;
    private float previousTouchDistance;
    private Vector2 previousMidpoint;

    public float ZoomFactor => zoomFactor;

    private void Awake()
    {
        target = GetComponent<RectTransform>();
        parentRect = target.parent as RectTransform;

        baseAnchoredPosition = target.anchoredPosition;
        baseLocalScale = target.localScale;

        minZoom = Mathf.Max(1f, minZoom);
        maxZoom = Mathf.Max(minZoom, maxZoom);

        zoomFactor = minZoom;

        ApplyScale();
        ClampPan();
    }

    private void OnEnable()
    {
        activeInstance = this;
    }

    private void OnDisable()
    {
        gestureActive = false;

        if (activeInstance == this)
        {
            activeInstance = null;
        }

        MobileDocumentGestureState.Reset();
    }

    private void Update()
    {
        if (!Application.isMobilePlatform) return;

        int touchCount =
            MobileDocumentGestureState.PressedTouchCount;

        if (touchCount < 2)
        {
            gestureActive = false;

            if (touchCount == 0)
            {
                MobileDocumentGestureState
                    .ReleaseGestureIfNoTouches();
            }

            return;
        }

        MobileDocumentGestureState.CaptureGesture();

        if (!TryGetTwoPressedTouches(
                out Vector2 firstPosition,
                out Vector2 secondPosition))
        {
            return;
        }

        float currentDistance =
            Vector2.Distance(
                firstPosition,
                secondPosition
            );

        Vector2 currentMidpoint =
            (firstPosition + secondPosition) * 0.5f;

        if (!gestureActive)
        {
            gestureActive = true;
            previousTouchDistance = currentDistance;
            previousMidpoint = currentMidpoint;
            return;
        }

        if (previousTouchDistance > 0.01f &&
            currentDistance > 0.01f)
        {
            float rawRatio =
                currentDistance / previousTouchDistance;

            float adjustedRatio =
                Mathf.Pow(
                    rawRatio,
                    zoomSensitivity
                );

            float newZoom =
                Mathf.Clamp(
                    zoomFactor * adjustedRatio,
                    minZoom,
                    maxZoom
                );

            SetZoomAroundScreenPoint(
                newZoom,
                currentMidpoint
            );
        }

        if (enableTwoFingerPan)
        {
            PanBetweenScreenPoints(
                previousMidpoint,
                currentMidpoint
            );
        }

        ClampPan();

        previousTouchDistance = currentDistance;
        previousMidpoint = currentMidpoint;
    }

    public void ResetView()
    {
        if (target == null) return;

        gestureActive = false;
        zoomFactor = minZoom;

        target.anchoredPosition =
            baseAnchoredPosition;

        ApplyScale();
        MobileDocumentGestureState.Reset();
    }

    public static void ResetActiveView()
    {
        activeInstance?.ResetView();
    }

    private void SetZoomAroundScreenPoint(
        float newZoom,
        Vector2 screenPoint)
    {
        if (target == null ||
            Mathf.Approximately(newZoom, zoomFactor))
        {
            return;
        }

        if (parentRect == null)
        {
            zoomFactor = newZoom;
            ApplyScale();
            return;
        }

        Camera eventCamera = GetEventCamera();

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    target,
                    screenPoint,
                    eventCamera,
                    out Vector2 targetLocalPoint))
        {
            zoomFactor = newZoom;
            ApplyScale();
            return;
        }

        Vector3 worldPointBefore =
            target.TransformPoint(
                targetLocalPoint
            );

        zoomFactor = newZoom;
        ApplyScale();

        Vector3 worldPointAfter =
            target.TransformPoint(
                targetLocalPoint
            );

        Vector3 worldCorrection =
            worldPointBefore - worldPointAfter;

        Vector3 parentCorrection =
            parentRect.InverseTransformVector(
                worldCorrection
            );

        target.anchoredPosition +=
            new Vector2(
                parentCorrection.x,
                parentCorrection.y
            );
    }

    private void PanBetweenScreenPoints(
        Vector2 previousScreenPoint,
        Vector2 currentScreenPoint)
    {
        if (target == null ||
            parentRect == null ||
            zoomFactor <= minZoom + 0.0001f)
        {
            return;
        }

        Camera eventCamera = GetEventCamera();

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parentRect,
                    previousScreenPoint,
                    eventCamera,
                    out Vector2 previousLocalPoint) ||
            !RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parentRect,
                    currentScreenPoint,
                    eventCamera,
                    out Vector2 currentLocalPoint))
        {
            return;
        }

        target.anchoredPosition +=
            currentLocalPoint - previousLocalPoint;
    }

    private void ApplyScale()
    {
        if (target == null) return;

        target.localScale =
            new Vector3(
                baseLocalScale.x * zoomFactor,
                baseLocalScale.y * zoomFactor,
                baseLocalScale.z
            );
    }

    private void ClampPan()
    {
        if (target == null) return;

        Rect rect = target.rect;
        Vector2 pivot = target.pivot;

        float baseWidth =
            rect.width * Mathf.Abs(baseLocalScale.x);

        float baseHeight =
            rect.height * Mathf.Abs(baseLocalScale.y);

        float scaledWidth = baseWidth * zoomFactor;
        float scaledHeight = baseHeight * zoomFactor;

        Vector2 position = target.anchoredPosition;

        if (scaledWidth <= baseWidth + 0.001f)
        {
            position.x = baseAnchoredPosition.x;
        }
        else
        {
            float baseLeft =
                baseAnchoredPosition.x -
                baseWidth * pivot.x;

            float baseRight =
                baseAnchoredPosition.x +
                baseWidth * (1f - pivot.x);

            float minimumX =
                baseRight -
                scaledWidth * (1f - pivot.x);

            float maximumX =
                baseLeft +
                scaledWidth * pivot.x;

            position.x =
                Mathf.Clamp(
                    position.x,
                    minimumX,
                    maximumX
                );
        }

        if (scaledHeight <= baseHeight + 0.001f)
        {
            position.y = baseAnchoredPosition.y;
        }
        else
        {
            float baseBottom =
                baseAnchoredPosition.y -
                baseHeight * pivot.y;

            float baseTop =
                baseAnchoredPosition.y +
                baseHeight * (1f - pivot.y);

            float minimumY =
                baseTop -
                scaledHeight * (1f - pivot.y);

            float maximumY =
                baseBottom +
                scaledHeight * pivot.y;

            position.y =
                Mathf.Clamp(
                    position.y,
                    minimumY,
                    maximumY
                );
        }

        target.anchoredPosition = position;
    }

    private bool TryGetTwoPressedTouches(
        out Vector2 firstPosition,
        out Vector2 secondPosition)
    {
        firstPosition = Vector2.zero;
        secondPosition = Vector2.zero;

        Touchscreen touchscreen =
            Touchscreen.current;

        if (touchscreen == null) return false;

        int foundTouches = 0;

        foreach (var touch in touchscreen.touches)
        {
            if (!touch.press.isPressed) continue;

            Vector2 position =
                touch.position.ReadValue();

            if (foundTouches == 0)
            {
                firstPosition = position;
            }
            else
            {
                secondPosition = position;
                return true;
            }

            foundTouches++;
        }

        return false;
    }

    private Camera GetEventCamera()
    {
        if (target == null) return null;

        Canvas canvas =
            target.GetComponentInParent<Canvas>();

        if (canvas == null ||
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }
}
