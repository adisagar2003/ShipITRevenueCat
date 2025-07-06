using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private LookCommand lookCommand;

    [SerializeField] private CameraLook cameraLook;
    [SerializeField] private MouseLookWithTouch mouseLookWithTouch;

    void Start()
    {
        lookCommand = new LookCommand(cameraLook, mouseLookWithTouch);
    }

    void Update()
    {
        Debug.Log("look command.execture running");
        lookCommand.Execute();
    }
}
