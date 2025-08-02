using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Development-specific class to detect keyboard input.
/// </summary>
public class JoystickDetection : MonoBehaviour
{
    [SerializeField] private bool usingKeyboard;
    private InputActions inputActions;
    private Vector2 inputValue;

    private void Start()
    {
        inputActions = new InputActions();
        inputActions.Enable();
        inputActions.Player.Move.performed += MovePerformed;
        inputActions.Player.Move.canceled += MoveCanceled;
    }

    private void MoveCanceled(InputAction.CallbackContext obj)
    {
        inputValue = Vector2.zero;
        usingKeyboard = false;
    }

    private void MovePerformed(InputAction.CallbackContext context)
    {
        usingKeyboard = true;
        inputValue = context.ReadValue<Vector2>();
    }

    // Update method removed - no longer needed without joystick

    // CheckForJoystick method removed - no longer using joystick input

#if GUIDebug
    private void OnGUI()
    {
        int width = Screen.width, height = Screen.height;
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(width - 130, 40, 110, 25); // Top-right position
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = height / 30;

        string text = $"Input: [{inputValue.x}, {inputValue.y}]";
        GUI.Label(rect, text, style);
    }
#endif

    public Vector2 GetInputValue()
    {
        return inputValue;
    }
}
