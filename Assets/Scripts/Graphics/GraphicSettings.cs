using UnityEngine;

public class GraphicSettings : MonoBehaviour
{
    void Start()
    {
        QualitySettings.vSyncCount = GameConstants.Graphics.VSYNC_DISABLED;
        Application.targetFrameRate = GameConstants.Graphics.TARGET_FRAME_RATE;
    }
}
