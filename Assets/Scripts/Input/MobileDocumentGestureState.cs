using UnityEngine;
using UnityEngine.InputSystem;

public static class MobileDocumentGestureState
{
    private static bool gestureCaptured;
    private static int blockThroughFrame = -1;

    public static int PressedTouchCount
    {
        get
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null) return 0;

            int count = 0;
            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.isPressed) count++;
            }

            return count;
        }
    }

    public static bool HasMultipleTouches =>
        PressedTouchCount >= 2;

    public static bool IsInputBlocked
    {
        get
        {
            if (!Application.isMobilePlatform) return false;

            return gestureCaptured ||
                   HasMultipleTouches ||
                   Time.frameCount <= blockThroughFrame;
        }
    }

    public static void CaptureGesture()
    {
        if (!Application.isMobilePlatform) return;

        gestureCaptured = true;
        blockThroughFrame = -1;
    }

    public static void ReleaseGestureIfNoTouches()
    {
        if (!Application.isMobilePlatform ||
            !gestureCaptured ||
            PressedTouchCount > 0)
        {
            return;
        }

        gestureCaptured = false;
        blockThroughFrame = Time.frameCount;
    }

    public static void Reset()
    {
        gestureCaptured = false;
        blockThroughFrame = -1;
    }
}
