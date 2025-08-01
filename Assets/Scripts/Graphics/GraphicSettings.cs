using UnityEngine;

public class GraphicSettings : MonoBehaviour
{
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120; // No FPS Limit
    }
}
