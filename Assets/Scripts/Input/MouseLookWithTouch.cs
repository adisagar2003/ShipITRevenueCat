using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLookWithTouch : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.67f;
    [SerializeField] private bool debugDraw = true;

    private Vector2 lookDelta;
    private Vector2 mouseDelta;
    private Vector2 touchDelta;

    void Update()
    {
        mouseDelta = Vector2.zero;

        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }

        touchDelta = Vector2.zero;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    Debug.Log("Detecting current touch");
                    Vector2 pos = touch.position.ReadValue();
                    if (pos.x > Screen.width * 0.5f) // right half of the screen
                    {
                        Vector2 delta = touch.delta.ReadValue();
                        touchDelta += delta;
                    }
                }
            }
        }

        lookDelta = (mouseDelta + touchDelta) * sensitivity;
    }
    private void OnGUI()
    {
        if (!debugDraw) return;

        GUI.Label(new Rect(10, 10, 500, 20), $"Mouse Delta: {mouseDelta}");
        GUI.Label(new Rect(10, 30, 500, 20), $"Touch Delta: {touchDelta}");
        GUI.Label(new Rect(10, 50, 500, 20), $"Look Delta: {lookDelta}");
    }

    public Vector2 GetLookDelta()
    {
        return lookDelta * sensitivity;
    }
}
