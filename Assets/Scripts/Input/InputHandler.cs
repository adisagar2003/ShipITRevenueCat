using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private LookCommand lookCommand;
    private MoveCommand moveCommand;

    [SerializeField] private CameraLook cameraLook;
    [SerializeField] private MouseLookWithTouch mouseLookWithTouch;
    private JoystickDetection joystickDetection;
    private PlayerMovement playerMovement;

    void Start()
    {
        joystickDetection = FindFirstObjectByType<JoystickDetection>();
        playerMovement = FindAnyObjectByType<PlayerMovement>();

        lookCommand = new LookCommand(cameraLook, mouseLookWithTouch);
        moveCommand = new MoveCommand(playerMovement, joystickDetection);
    }

    void Update()
    {
        lookCommand.Execute();
    }

    private void FixedUpdate()
    {
        moveCommand?.Execute();
    }
}
