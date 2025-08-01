using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DebugLogGUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private int maxLines = GameConstants.Debug.MAX_DEBUG_LOG_LINES;
    [SerializeField] private float backgroundAlpha = GameConstants.Debug.DEBUG_BACKGROUND_ALPHA;
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = GameConstants.Debug.DEBUG_FONT_SIZE;
    [SerializeField] private float logLifetime = GameConstants.Debug.LOG_LIFETIME_SECONDS;

    private Queue<(string message, float timestamp)> logLines = new Queue<(string, float)>();
    private Text logText;
    private GameObject canvasObj;
    private GameObject panelObj;
    private bool isVisible = false;
    private bool logTextNeedsUpdate = false;
    private float lastLogCheckTime = 0f;
    private const float LOG_CHECK_INTERVAL = 0.1f; // Check for expired logs every 100ms instead of every frame

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

        // Only check for expired logs periodically, not every frame
        if (Time.unscaledTime - lastLogCheckTime > LOG_CHECK_INTERVAL)
        {
            lastLogCheckTime = Time.unscaledTime;
            
            // Remove logs older than logLifetime
            bool changed = false;
            while (logLines.Count > 0 && Time.unscaledTime - logLines.Peek().timestamp > logLifetime)
            {
                logLines.Dequeue();
                changed = true;
            }
            
            if (changed)
            {
                logTextNeedsUpdate = true;
            }
        }
        
        // Update log text only when needed and visible
        if (logTextNeedsUpdate && isVisible)
        {
            UpdateLogText();
            logTextNeedsUpdate = false;
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Use static color tags to avoid string allocation
        string colorTag = GetColorTagForLogType(type);
        string formatted = $"{colorTag}{logString}</color>";
        
        logLines.Enqueue((formatted, Time.unscaledTime));
        
        // Trim excess logs
        while (logLines.Count > maxLines)
            logLines.Dequeue();
        
        // Mark for update instead of updating immediately
        logTextNeedsUpdate = true;
        
        // If visible, update immediately for responsiveness
        if (isVisible)
        {
            UpdateLogText();
            logTextNeedsUpdate = false;
        }
    }
    
    private string GetColorTagForLogType(LogType type)
    {
        return type switch
        {
            LogType.Error or LogType.Exception => "<color=red>",
            LogType.Warning => "<color=yellow>",
            _ => "<color=white>"
        };
    }

    void UpdateLogText()
    {
        if (logText == null) return;
        
        // Use StringBuilder for better performance with string concatenation
        var sb = new System.Text.StringBuilder(logLines.Count * 50); // Pre-allocate capacity
        
        bool first = true;
        foreach (var entry in logLines)
        {
            if (!first)
                sb.AppendLine();
            sb.Append(entry.message);
            first = false;
        }
        
        logText.text = sb.ToString();
    }

    void SetPanelVisibility(bool visible)
    {
        if (panelObj != null)
            panelObj.SetActive(visible);
    }
}
