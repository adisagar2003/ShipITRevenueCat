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
        cinemachineFreeLook.m_XAxis.m_InputAxisValue = delta.x;
        cinemachineFreeLook.m_YAxis.m_InputAxisValue = delta.y;
    }
}
