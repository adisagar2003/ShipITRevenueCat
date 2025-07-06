using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        int width = Screen.width, height = Screen.height;
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(width - 120, 10, 110, 25); // Top-right position
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = height / 30;

        float fps = 1.0f / deltaTime;

        // Color based on FPS thresholds
        if (fps < 30)
            style.normal.textColor = Color.red;
        else if (fps < 60)
            style.normal.textColor = new Color(1f, 0.64f, 0f); // orange
        else
            style.normal.textColor = Color.green;

        string text = $"{fps:0.} FPS";
        GUI.Label(rect, text, style);
    }
}
