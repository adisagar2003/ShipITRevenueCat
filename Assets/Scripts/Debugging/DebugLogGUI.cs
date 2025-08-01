using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DebugLogGUI : MonoBehaviour
{
    [Header("UI Settings")]
    public int maxLines = 30;
    public float backgroundAlpha = 0.7f;
    public Color backgroundColor = Color.black;
    public Color textColor = Color.white;
    public int fontSize = 16;
    [SerializeField] public float logLifetime = 4f; // Seconds before a log is cleared

    private Queue<(string message, float timestamp)> logLines = new Queue<(string, float)>();
    private Text logText;
    private GameObject canvasObj;
    private GameObject panelObj;
    private bool isVisible = false;

    void Awake()
    {
        // Create Canvas
        canvasObj = new GameObject("DebugLogCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Panel
        panelObj = new GameObject("DebugLogPanel");
        panelObj.transform.SetParent(canvasObj.transform);
        var panel = panelObj.AddComponent<Image>();
        backgroundColor.a = backgroundAlpha;
        panel.color = backgroundColor;
        var rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.4f);
        rect.offsetMin = new Vector2(10, 10);
        rect.offsetMax = new Vector2(-10, 0);

        // Create Text
        var textObj = new GameObject("DebugLogText");
        textObj.transform.SetParent(panelObj.transform);
        logText = textObj.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        logText.fontSize = fontSize;
        logText.color = textColor;
        logText.alignment = TextAnchor.LowerLeft;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10); 

        // Register log callback
        Application.logMessageReceived += HandleLog;

        // Start hidden
        SetPanelVisibility(false);
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        if (canvasObj != null)
            Destroy(canvasObj);
    }

    void Update()
    {
        // Toggle visibility with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isVisible = !isVisible;
            SetPanelVisibility(isVisible);
        }

        // Remove logs older than logLifetime
        bool changed = false;
        while (logLines.Count > 0 && Time.unscaledTime - logLines.Peek().timestamp > logLifetime)
        {
            logLines.Dequeue();
            changed = true;
        }
        if (changed)
        {
            UpdateLogText();
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string colorTag = type == LogType.Error || type == LogType.Exception ? "<color=red>" : type == LogType.Warning ? "<color=yellow>" : "<color=white>";
        string formatted = $"{colorTag}{logString}</color>";
        logLines.Enqueue((formatted, Time.unscaledTime));
        while (logLines.Count > maxLines)
            logLines.Dequeue();
        UpdateLogText();
    }

    void UpdateLogText()
    {
        List<string> messages = new List<string>();
        foreach (var entry in logLines)
            messages.Add(entry.message);
        logText.text = string.Join("\n", messages);
    }

    void SetPanelVisibility(bool visible)
    {
        if (panelObj != null)
            panelObj.SetActive(visible);
    }
}
