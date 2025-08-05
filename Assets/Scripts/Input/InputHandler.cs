// #define MULTIPLAYER

using System;
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
    private JumpCommand jumpCommand;

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
        // Initialize movement components
        var networkThirdPersonController = GetComponent<NetworkThirdPersonController>();
        var thirdPersonController = GetComponent<ThirdPersonController>();
        
        // Initialize MoveCommand based on available components (prioritize network-friendly)
        if (networkThirdPersonController != null && joystickDetection != null)
        {
            moveCommand = new MoveCommand(networkThirdPersonController, joystickDetection);
        }
        else if (thirdPersonController != null && joystickDetection != null)
        {
            moveCommand = new MoveCommand(thirdPersonController, joystickDetection);
        }
        else if (playerMovement != null && joystickDetection != null)
        {
            moveCommand = new MoveCommand(playerMovement, joystickDetection);
        }
        else if (playerMovementSinglePlayer != null && joystickDetection != null)
        {
            moveCommand = new MoveCommand(playerMovementSinglePlayer, joystickDetection);
        }
        else
        {
            Debug.LogWarning("MoveCommand not initialized - no compatible movement component found.");
        }
        
        // Initialize JumpCommand based on available components
        var playerAnimationHandle = GetComponent<PlayerAnimationHandle>();
        if (networkThirdPersonController != null && joystickDetection != null)
        {
            jumpCommand = new JumpCommand(networkThirdPersonController, joystickDetection);
        }
        else if (thirdPersonController != null && joystickDetection != null)
        {
            jumpCommand = new JumpCommand(thirdPersonController, joystickDetection);
        }
        else if (playerMovement != null && playerAnimationHandle != null && joystickDetection != null)
        {
            jumpCommand = new JumpCommand(playerMovement, playerAnimationHandle, joystickDetection);
        }
        else
        {
            Debug.LogWarning("JumpCommand not initialized - no compatible movement component found.");
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
        jumpCommand?.Execute();
    }
}
