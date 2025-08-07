using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

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
public class InputManager : NetworkBehaviour
{
    [Header("Input Sources")]
    [SerializeField] private MonoBehaviour mobileJoystick; // Mobile joystick component (FixedJoystick, VariableJoystick, etc.)
    [SerializeField] private bool prioritizeMobileInput = true; // Mobile takes priority when available

    [SerializeField] private bool usingKeyboard;
    private InputActions inputActions;
    private Vector2 inputValue;
    private bool jumpPressed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only initialize input for the owner of this network object
        if (!IsOwner) return;

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

#if DEBUG
        Debug.Log($"[InputManager] Network spawned for {(IsOwner ? "OWNER" : "NON-OWNER")} on {gameObject.name}");
#endif
    }

    /// <summary>
    /// Finds the FixedJoystick component using the FixedJoystica, kIdentifier workaround
    /// </summary>
    private void FindFixedJoystick()
    {
#if DEBUG
        Debug.Log($"<color=cyan>[InputManager]</color> <color=white>FindFixedJoystick() - Searching for FixedJoystickIdentifier...</color>");
#endif

        FixedJoystickIdentifier identifier = FindFirstObjectByType<FixedJoystickIdentifier>();
        if (identifier != null)
        {
#if DEBUG
            Debug.Log($"<color=cyan>[InputManager]</color> <color=green>Found FixedJoystickIdentifier on: {identifier.gameObject.name}</color>");
#endif

            // Look specifically for FixedJoystick component using reflection to avoid compilation issues
            var allComponents = identifier.GetComponents<MonoBehaviour>();
            
#if DEBUG
            Debug.Log($"<color=cyan>[InputManager]</color> <color=white>Found {allComponents.Length} MonoBehaviour components on {identifier.gameObject.name}</color>");
            foreach (var comp in allComponents)
            {
                Debug.Log($"<color=cyan>[InputManager]</color> <color=white>  - Component: {comp.GetType().Name}</color>");
            }
#endif
            
            // Find the FixedJoystick component by name
            foreach (var component in allComponents)
            {
                if (component.GetType().Name.Contains("FixedJoystick") || 
                    component.GetType().Name.Contains("Joystick"))
                {
                    mobileJoystick = component;
#if DEBUG
                    Debug.Log($"<color=cyan>[InputManager]</color> <color=green>✅ Successfully found joystick component: {mobileJoystick.GetType().Name} on {identifier.gameObject.name}</color>");
#endif
                    break;
                }
            }
            
            if (mobileJoystick == null)
            {
#if DEBUG
                Debug.LogWarning($"<color=orange>[InputManager]</color> <color=white>⚠️ FixedJoystickIdentifier found but no joystick component on {identifier.gameObject.name}</color>");
                Debug.Log($"<color=yellow>[InputManager]</color> <color=white>Available components: {string.Join(", ", System.Linq.Enumerable.Select(allComponents, c => c.GetType().Name))}</color>");
#endif
            }
        }
        else
        {
#if DEBUG
            Debug.LogWarning($"<color=orange>[InputManager]</color> <color=white>⚠️ No FixedJoystickIdentifier found - mobile joystick input unavailable</color>");
#endif
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
        // Only process input for the owner
        if (!IsOwner) return;

        // Handle mobile joystick input if available and prioritized
        if (mobileJoystick != null && prioritizeMobileInput)
        {
            Vector2 joystickInput = GetJoystickInput();
            if (joystickInput.sqrMagnitude > 0.01f)
            {
                inputValue = joystickInput;
                usingKeyboard = false; // Using mobile joystick

#if DEBUG
                Debug.Log($"<color=lime>[InputManager]</color> <color=white>📱 Mobile joystick input detected: {joystickInput} (magnitude: {joystickInput.magnitude:F3})</color>");
#endif
            }
            else
            {
                // IMPORTANT: Reset inputValue to zero when joystick is released
                // This fixes the issue where player keeps moving in last direction
                if (!usingKeyboard) // Only reset if we're not using keyboard input
                {
                    inputValue = Vector2.zero;
#if DEBUG
                    Debug.Log($"<color=orange>[InputManager]</color> <color=white>🛑 Joystick released, resetting inputValue to zero</color>");
#endif
                }
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
        // Only return input for the owner
        if (!IsOwner) return Vector2.zero;

        // Prioritize mobile joystick if available
        if (mobileJoystick != null && prioritizeMobileInput)
        {
            Vector2 joystickInput = GetJoystickInput();
            if (joystickInput.sqrMagnitude > 0.01f)
            {
#if DEBUG
                Debug.Log($"<color=lime>[InputManager]</color> <color=white>📱 Returning mobile joystick input: {joystickInput}</color>");
#endif
                return joystickInput;
            }
        }

        // Fall back to Input System (keyboard/gamepad)
        if (inputValue.sqrMagnitude > 0.01f)
        {
#if DEBUG
            Debug.Log($"<color=lightblue>[InputManager]</color> <color=white>⌨️ Returning keyboard/gamepad input: {inputValue}</color>");
#endif
        }
        return inputValue;
    }

    public bool GetJumpPressed()
    {
        // Only return jump input for the owner
        if (!IsOwner) return false;

        bool pressed = jumpPressed;
        jumpPressed = false; // Reset after reading
        return pressed;
    }

    /// <summary>
    /// Safely gets joystick input using reflection to avoid compilation issues
    /// </summary>
    private Vector2 GetJoystickInput()
    {
        if (mobileJoystick == null)
        {
#if DEBUG
            Debug.LogWarning($"<color=red>[InputManager]</color> <color=white>❌ GetJoystickInput() called but mobileJoystick is null</color>");
#endif
            return Vector2.zero;
        }

#if DEBUG
        // Log joystick component details for debugging (only when there's potential input)
        Debug.Log($"<color=cyan>[InputManager]</color> <color=white>🔍 Getting input from joystick: {mobileJoystick.GetType().Name}</color>");
#endif

        try
        {
            // Try to get the Direction property first (most efficient)
            var directionProperty = mobileJoystick.GetType().GetProperty("Direction");
            if (directionProperty != null)
            {
                var direction = (Vector2)directionProperty.GetValue(mobileJoystick, null);
#if DEBUG
                if (direction.sqrMagnitude > 0.01f)
                {
                    Debug.Log($"<color=lime>[InputManager]</color> <color=white>✅ Direction property found: {direction} (magnitude: {direction.magnitude:F3})</color>");
                }
                else if (direction.sqrMagnitude > 0.001f)
                {
                    Debug.Log($"<color=yellow>[InputManager]</color> <color=white>⚪ Small direction value: {direction} (magnitude: {direction.magnitude:F6}) - below threshold</color>");
                }
#endif
                return direction;
            }
            else
            {
#if DEBUG
                Debug.Log($"<color=orange>[InputManager]</color> <color=white>⚠️ Direction property not found, trying horizontal/vertical...</color>");
#endif
            }

            // Fallback to individual horizontal/vertical properties
            var horizontalProperty = mobileJoystick.GetType().GetProperty("horizontal");
            var verticalProperty = mobileJoystick.GetType().GetProperty("vertical");

            if (horizontalProperty != null && verticalProperty != null)
            {
                float h = (float)horizontalProperty.GetValue(mobileJoystick, null);
                float v = (float)verticalProperty.GetValue(mobileJoystick, null);
                var result = new Vector2(h, v);

#if DEBUG
                if (result.sqrMagnitude > 0.01f)
                {
                    Debug.Log($"<color=lime>[InputManager]</color> <color=white>✅ H/V properties found: h={h:F3}, v={v:F3}, result={result}</color>");
                }
                else if (result.sqrMagnitude > 0.001f)
                {
                    Debug.Log($"<color=yellow>[InputManager]</color> <color=white>⚪ Small H/V values: h={h:F6}, v={v:F6}, magnitude={result.magnitude:F6} - below threshold</color>");
                }
#endif
                return result;
            }
            else
            {
#if DEBUG
                Debug.LogWarning($"<color=red>[InputManager]</color> <color=white>❌ Neither Direction nor horizontal/vertical properties found on {mobileJoystick.GetType().Name}</color>");

                // Log all available properties for debugging
                var allProperties = mobileJoystick.GetType().GetProperties();
                Debug.Log($"<color=yellow>[InputManager]</color> <color=white>📋 Available properties on {mobileJoystick.GetType().Name}: {string.Join(", ", System.Linq.Enumerable.Select(allProperties, p => p.Name))}</color>");
#endif
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.LogError($"<color=red>[InputManager]</color> <color=white>💥 Failed to get joystick input via reflection: {ex.Message}</color>");
            Debug.LogError($"<color=red>[InputManager]</color> <color=white>Stack trace: {ex.StackTrace}</color>");
#endif
        }

#if DEBUG
        Debug.LogWarning($"<color=orange>[InputManager]</color> <color=white>⚠️ Returning Vector2.zero - no joystick input detected</color>");
#endif
        return Vector2.zero;
    }

    public override void OnNetworkDespawn()
    {
        // Only cleanup input actions if this was the owner
        if (inputActions != null)
        {
            inputActions.Player.Move.performed -= MovePerformed;
            inputActions.Player.Move.canceled -= MoveCanceled;
            inputActions.Player.Jump.performed -= JumpPerformed;
            inputActions.Dispose();
            inputActions = null;
        }

        base.OnNetworkDespawn();
    }
}
