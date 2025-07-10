#define MULTIPLAYER

using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles player input for movement and look,
/// with #define MULTIPLAYER toggle for singleplayer/multiplayer testing.
/// </summary>
public class InputHandler : NetworkBehaviour
{
    private LookCommand lookCommand;
    private MoveCommand moveCommand;

    [SerializeField] private CameraLook cameraLook;
    [SerializeField] private MouseLookWithTouch mouseLookWithTouch;

    private JoystickDetection joystickDetection;
    private PlayerMovement playerMovement;
    private PlayerMovementSinglePlayer playerMovementSinglePlayer;

    void Start()
    {
        joystickDetection = GetComponent<JoystickDetection>();
        playerMovement = GetComponent<PlayerMovement>();
        playerMovementSinglePlayer = GetComponent<PlayerMovementSinglePlayer>();
        #if MULTIPLAYER
            // Multiplayer: process input only on owner
            if (!IsOwner) return;
        #endif
        if (cameraLook != null && mouseLookWithTouch != null)
        {
            lookCommand = new LookCommand(cameraLook, mouseLookWithTouch);
        }
        else
        {
            Debug.LogWarning("LookCommand not initialized due to missing CameraLook or MouseLookWithTouch.");
        }
        if (playerMovement != null && joystickDetection != null)
        {
            moveCommand = new MoveCommand(playerMovement, joystickDetection);
        }
        else if (playerMovementSinglePlayer != null && joystickDetection != null)
        {
            moveCommand = new MoveCommand(playerMovementSinglePlayer, joystickDetection);
        }
        else
        {
            Debug.LogWarning("MoveCommand not initialized due to missing movement or joystick components.");
        }
    }

    void Update()
    {
    #if MULTIPLAYER
        if (!IsOwner) return;
    #endif
        lookCommand?.Execute();
    }

    private void FixedUpdate()
    {
        #if MULTIPLAYER
                if (!IsOwner) return;
        #endif
        moveCommand?.Execute();
    }
}
