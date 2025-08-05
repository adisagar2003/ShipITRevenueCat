using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages all player input from multiple sources including keyboard, gamepad, and mobile touch controls.
/// Handles movement input (WASD keys, mobile joystick) and jump input (spacebar) through Unity's new Input System.
/// Prioritizes mobile joystick when available for seamless cross-platform gameplay.
/// </summary>
/// <remarks>
/// This class serves as a unified input interface for the command pattern system, supporting:
/// - New Input System: Keyboard (WASD) and gamepad input
/// - Mobile Controls: Fixed joystick for movement
/// - Jump Input: Spacebar detection with single-frame consumption
/// - Priority System: Mobile input takes precedence when joystick is actively used
/// </remarks>
public class InputManager : MonoBehaviour
{
    [Header("Input Sources")]
    [SerializeField] private Joystick fixedJoystick; // Mobile joystick reference (supports FixedJoystick and other Joystick types)
    [SerializeField] private bool prioritizeMobileInput = true; // Mobile takes priority when available

    [SerializeField] private bool usingKeyboard;
    private InputActions inputActions;
    private Vector2 inputValue;
    private bool jumpPressed;

    private void Start()
    {
        inputActions = new InputActions();
        inputActions.Enable();
        inputActions.Player.Move.performed += MovePerformed;
        inputActions.Player.Move.canceled += MoveCanceled;
        inputActions.Player.Jump.performed += JumpPerformed;
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

    private void JumpPerformed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    private void Update()
    {
        // Handle mobile joystick input if available and prioritized
        if (fixedJoystick != null && prioritizeMobileInput)
        {
            Vector2 joystickInput = new Vector2(fixedJoystick.horizontal, fixedJoystick.vertical);
            if (joystickInput.sqrMagnitude > 0.01f)
            {
                inputValue = joystickInput;
                usingKeyboard = false; // Using mobile joystick
            }
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
        // Prioritize mobile joystick if available
        if (fixedJoystick != null && prioritizeMobileInput)
        {
            Vector2 joystickInput = new Vector2(fixedJoystick.horizontal, fixedJoystick.vertical);
            if (joystickInput.sqrMagnitude > 0.01f)
            {
                return joystickInput;
            }
        }

        // Fall back to Input System (keyboard/gamepad)
        return inputValue;
    }

    public bool GetJumpPressed()
    {
        bool pressed = jumpPressed;
        jumpPressed = false; // Reset after reading
        return pressed;
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Player.Move.performed -= MovePerformed;
            inputActions.Player.Move.canceled -= MoveCanceled;
            inputActions.Player.Jump.performed -= JumpPerformed;
            inputActions.Dispose();
        }
    }
}
