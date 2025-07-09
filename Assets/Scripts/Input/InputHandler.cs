using UnityEngine;
using Unity.Netcode;

public class InputHandler : NetworkBehaviour
{
    private LookCommand lookCommand;
    private MoveCommand moveCommand;

    [SerializeField] private CameraLook cameraLook;
    [SerializeField] private MouseLookWithTouch mouseLookWithTouch;
    private JoystickDetection joystickDetection;
    private PlayerMovement playerMovement;

    void Start()
    {
        
        joystickDetection = GetComponent<JoystickDetection>();
        playerMovement = GetComponent<PlayerMovement>();

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
