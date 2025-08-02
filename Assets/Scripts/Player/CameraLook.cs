using UnityEngine;
using Cinemachine;

public class CameraLook : MonoBehaviour
{
    private CinemachineFreeLook cinemachineFreeLook;
    [SerializeField] private float cameraFloatSpeedX = 1.0f;
    [SerializeField] private float cameraFloatSpeedY = 1.0f;

    private void Start()
    {
        cinemachineFreeLook = GetComponent<CinemachineFreeLook>();
    }
    
    public void Look(Vector2 delta)
    {
        if (cinemachineFreeLook == null) return;
        
        // Unity 6 / Cinemachine 3.x compatible input handling
        // Apply input directly to axis values with speed modifiers
        cinemachineFreeLook.m_XAxis.m_InputAxisValue = delta.x * cameraFloatSpeedX;
        cinemachineFreeLook.m_YAxis.m_InputAxisValue = delta.y * cameraFloatSpeedY;
    }
}
