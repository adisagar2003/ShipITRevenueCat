using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Development-specific class to check if input is running or not.
/// </summary>
public class JoystickDetection : MonoBehaviour
{
    [SerializeField] private bool usingJoystick;
    [SerializeField] private bool usingKeyboard;
#if JOYSTICK_PACK
    private FixedJoystick fixedJoystick;
#endif
    private InputActions inputActions;
    private Vector2 inputValue;
    float x;
    float y;

    private void Start()
    {
        inputActions = new InputActions();
        inputActions.Enable();
        fixedJoystick = FindFirstObjectByType<FixedJoystick>();
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
        if (usingJoystick) return;
        usingKeyboard = true;
        inputValue = context.ReadValue<Vector2>();
    }

    private void Update()
    {
        // If using joystick, override inputValue
        CheckForJoystick();
    }

    private void CheckForJoystick()
    {
        if (fixedJoystick == null)
        {
            fixedJoystick = FindFirstObjectByType<FixedJoystick>(); // keep checking each tick, Optimize for later.
            return;
        }

        if (fixedJoystick.horizontal < 0.01f && fixedJoystick.vertical < 0.01f)
        {
            usingJoystick = false;
            if (!usingKeyboard)
            {
                inputValue = Vector2.zero;
            }
        }
        else
        {
            usingJoystick = true;
            inputValue = new Vector2(fixedJoystick.horizontal, fixedJoystick.vertical);
        }
    }

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
