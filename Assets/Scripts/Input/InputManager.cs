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
    [SerializeField] private MonoBehaviour mobileJoystick; // Mobile joystick component (FixedJoystick, VariableJoystick, etc.)
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

        // Find FixedJoystick using the identifier component
        if (mobileJoystick == null)
        {
            FindFixedJoystick();
        }
    }

    /// <summary>
    /// Finds the FixedJoystick component using the FixedJoystickIdentifier workaround
    /// </summary>
    private void FindFixedJoystick()
    {
        FixedJoystickIdentifier identifier = FindFirstObjectByType<FixedJoystickIdentifier>();
        if (identifier != null)
        {
            // Get the joystick component from the same GameObject
            mobileJoystick = identifier.GetComponent<MonoBehaviour>();
            if (mobileJoystick != null)
            {
                Debug.Log($"InputManager: Found FixedJoystick via identifier on {identifier.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("InputManager: FixedJoystickIdentifier found but no joystick component on the same GameObject");
            }
        }
        else
        {
            Debug.Log("InputManager: No FixedJoystickIdentifier found - mobile joystick input unavailable");
        }
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
        if (mobileJoystick != null && prioritizeMobileInput)
        {
            Vector2 joystickInput = GetJoystickInput();
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
        if (mobileJoystick != null && prioritizeMobileInput)
        {
            Vector2 joystickInput = GetJoystickInput();
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

    /// <summary>
    /// Safely gets joystick input using reflection to avoid compilation issues
    /// </summary>
    private Vector2 GetJoystickInput()
    {
        if (mobileJoystick == null) return Vector2.zero;

        try
        {
            // Try to get the Direction property first (most efficient)
            var directionProperty = mobileJoystick.GetType().GetProperty("Direction");
            if (directionProperty != null)
            {
                return (Vector2)directionProperty.GetValue(mobileJoystick, null);
            }

            // Fallback to individual horizontal/vertical properties
            var horizontalProperty = mobileJoystick.GetType().GetProperty("horizontal");
            var verticalProperty = mobileJoystick.GetType().GetProperty("vertical");

            if (horizontalProperty != null && verticalProperty != null)
            {
                float h = (float)horizontalProperty.GetValue(mobileJoystick, null);
                float v = (float)verticalProperty.GetValue(mobileJoystick, null);
                return new Vector2(h, v);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"InputManager: Failed to get joystick input via reflection: {ex.Message}");
        }

        return Vector2.zero;
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
