using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Единый источник позиции указателя для desktop и mobile.
/// Touch имеет приоритет, затем мышь, затем перо.
/// На touch-устройствах запоминает последнюю позицию пальца.
/// </summary>
public static class ScreenPointer
{
    private static bool hasLastTouchPosition;
    private static Vector2 lastTouchPosition;

    public static bool TouchWasPressedThisFrame
    {
        get
        {
            return Touchscreen.current != null &&
                   Touchscreen.current.primaryTouch.press
                       .wasPressedThisFrame;
        }
    }

    public static bool TouchWasReleasedThisFrame
    {
        get
        {
            return Touchscreen.current != null &&
                   Touchscreen.current.primaryTouch.press
                       .wasReleasedThisFrame;
        }
    }

    public static bool IsTouchPressed
    {
        get
        {
            return Touchscreen.current != null &&
                   Touchscreen.current.primaryTouch.press
                       .isPressed;
        }
    }

    public static bool TryGetTouchPosition(
        out Vector2 screenPosition)
    {
        if (Touchscreen.current == null)
        {
            screenPosition =
                Vector2.zero;

            return false;
        }

        screenPosition =
            Touchscreen.current.primaryTouch.position
                .ReadValue();

        if (Touchscreen.current.primaryTouch.press
            .isPressed)
        {
            lastTouchPosition =
                screenPosition;

            hasLastTouchPosition =
                true;
        }

        return true;
    }

    public static bool TryGetPosition(
        out Vector2 screenPosition)
    {
        if (Touchscreen.current != null)
        {
            var primaryTouch =
                Touchscreen.current.primaryTouch;

            if (primaryTouch.press.isPressed)
            {
                lastTouchPosition =
                    primaryTouch.position.ReadValue();

                hasLastTouchPosition =
                    true;

                screenPosition =
                    lastTouchPosition;

                return true;
            }
        }

        if (Mouse.current != null)
        {
            screenPosition =
                Mouse.current.position.ReadValue();

            return true;
        }

        if (Pen.current != null)
        {
            screenPosition =
                Pen.current.position.ReadValue();

            return true;
        }

        if (Touchscreen.current != null &&
            hasLastTouchPosition)
        {
            screenPosition =
                lastTouchPosition;

            return true;
        }

        screenPosition =
            Vector2.zero;

        return false;
    }
}
