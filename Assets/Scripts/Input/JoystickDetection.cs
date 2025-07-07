using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Development specific class to check if input is running or nah
/// 
/// </summary>
public class JoystickDetection : MonoBehaviour
{
    [SerializeField] private bool usingJoystick;
    [SerializeField] private bool usingKeyboard;
    private FixedJoystick fixedJoystick;
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
        inputValue = new Vector2(0, 0);
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
        // This case I am using joystick,  override inputVal
        CheckForJoystick();
    }

    private void CheckForJoystick()
    {
        if ((fixedJoystick.horizontal < 0.01f && fixedJoystick.vertical < 0.01f) == false)
        {
            usingJoystick = true;
            inputValue = new Vector2(fixedJoystick.horizontal, fixedJoystick.vertical);
        }
        else
        {
            usingJoystick = false;
            if (!usingKeyboard)
            {
                inputValue = Vector2.zero;
            }
        }
    }

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

    public Vector2 GetInputValue()
    {
        return inputValue;
    }
}
