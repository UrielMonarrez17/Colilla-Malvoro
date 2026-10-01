using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

// Supports the project's active Input System without changing ProjectSettings.
public static class FeralInput
{
    public static float Horizontal => Axis(KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow, true);
    public static float Vertical => Axis(KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow, false);

    static float Axis(KeyCode negative, KeyCode positive, KeyCode alternateNegative, KeyCode alternatePositive, bool horizontal)
    {
#if ENABLE_INPUT_SYSTEM
        float keyboard = (Held(positive) || Held(alternatePositive) ? 1f : 0f) - (Held(negative) || Held(alternateNegative) ? 1f : 0f);
        if (keyboard != 0f || Gamepad.current == null) return keyboard;
        return horizontal ? Gamepad.current.leftStick.ReadValue().x : Gamepad.current.leftStick.ReadValue().y;
#else
        return Input.GetAxisRaw(horizontal ? "Horizontal" : "Vertical");
#endif
    }

    public static bool Held(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return Control(key)?.isPressed ?? false;
#else
        return Input.GetKey(key);
#endif
    }

    public static bool Pressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        return Control(key)?.wasPressedThisFrame ?? false;
#else
        return Input.GetKeyDown(key);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    static KeyControl Control(KeyCode key)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return null;
        switch (key)
        {
            case KeyCode.A: return keyboard.aKey;
            case KeyCode.D: return keyboard.dKey;
            case KeyCode.W: return keyboard.wKey;
            case KeyCode.S: return keyboard.sKey;
            case KeyCode.LeftArrow: return keyboard.leftArrowKey;
            case KeyCode.RightArrow: return keyboard.rightArrowKey;
            case KeyCode.UpArrow: return keyboard.upArrowKey;
            case KeyCode.DownArrow: return keyboard.downArrowKey;
            case KeyCode.LeftShift: return keyboard.leftShiftKey;
            case KeyCode.C: return keyboard.cKey;
            case KeyCode.Space: return keyboard.spaceKey;
            case KeyCode.F: return keyboard.fKey;
            case KeyCode.I: return keyboard.iKey;
            default: return null;
        }
    }
#endif
}
