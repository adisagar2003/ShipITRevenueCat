using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System;

public class LobbyDebugUI : NetworkBehaviour
{
    private Canvas canvas;
    private Text debugTextTMP;
    private Text debugTextLegacy;
    private bool isVisible = false;

    [SerializeField] private float refreshRate = 1f;

    private void Start()
    {
        CreateCanvas();
        InvokeRepeating(nameof(UpdateDebugText), 1f, refreshRate);
        UpdateDebugText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isVisible = !isVisible;
            canvas.enabled = isVisible;
            UpdateDebugText();
        }
    }

    private void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("DebugOverlayCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Removed: canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("DebugText");
        textGO.transform.SetParent(canvasGO.transform);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(600, 800);

        // Using UI Text for Unity 6 compatibility
        debugTextTMP = textGO.AddComponent<Text>();
        debugTextTMP.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugTextTMP.fontSize = 18;
        debugTextTMP.color = Color.green;
        debugTextTMP.text = "Debug Overlay Initialized.";
        debugTextTMP.raycastTarget = false;
        
        // Fallback to legacy Text if TextMeshPro fails to load
        if (debugTextTMP == null)
        {
            debugTextLegacy = textGO.AddComponent<Text>();
            debugTextLegacy.fontSize = 18;
            debugTextLegacy.color = Color.green;
            debugTextLegacy.text = "Debug Overlay Initialized.";
            debugTextLegacy.raycastTarget = false;
        }

        canvas.enabled = false; // Initially hidden
    }

    private void UpdateDebugText()
    {
        string debugInfo = "";

        debugInfo += $"<b>Scene:</b> {SceneManager.GetActiveScene().name}\n";
        debugInfo += $"<b>Frame:</b> {Time.frameCount}\n";
        debugInfo += $"<b>Time:</b> {Time.time:F2}s\n";

        if (NetworkManager.Singleton != null)
        {
            debugInfo += $"<b>IsServer:</b> {NetworkManager.Singleton.IsServer}\n";
            debugInfo += $"<b>IsClient:</b> {NetworkManager.Singleton.IsClient}\n";
            debugInfo += $"<b>IsHost:</b> {NetworkManager.Singleton.IsHost}\n";
            debugInfo += $"<b>Connected Clients:</b> {NetworkManager.Singleton}\n";

            if (IsServer)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    debugInfo += $"- ClientId: {client.ClientId}\n";
                }
            }
        }
        else
        {
            debugInfo += $"<b>NetworkManager:</b> Not Found.\n";
        }

#if USING_SERVICES_CORE
        try
        {
            var auth = Unity.Services.Authentication.AuthenticationService.Instance;
            debugInfo += $"<b>PlayerID:</b> {auth.PlayerId}\n";
        }
        catch
        {
            debugInfo += $"<b>PlayerID:</b> Not Authenticated.\n";
        }
#endif

        debugInfo += $"Press <b>Tab</b> to toggle overlay.";

        if (debugTextTMP != null)
            debugTextTMP.text = debugInfo;
        else if (debugTextLegacy != null)
            debugTextLegacy.text = debugInfo;
    }
}
