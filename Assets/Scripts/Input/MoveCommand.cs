using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : ICommand
{
    private PlayerMovement movement;
    private PlayerMovementSinglePlayer playerMovementSinglePlayer; // deprecated
    private ThirdPersonController thirdPersonController;
    private NetworkThirdPersonController networkThirdPersonController;
    private JoystickDetection joystickDetection;

    public MoveCommand(PlayerMovement movement, JoystickDetection joystickDetection)
    {
        this.movement = movement;
        this.joystickDetection = joystickDetection;
    }

    public MoveCommand(PlayerMovementSinglePlayer movement, JoystickDetection joystickDetection)
    {
        this.playerMovementSinglePlayer = movement;
        this.joystickDetection = joystickDetection;
    }
    
    public MoveCommand(ThirdPersonController thirdPersonController, JoystickDetection joystickDetection)
    {
        this.thirdPersonController = thirdPersonController;
        this.joystickDetection = joystickDetection;
    }
    
    public MoveCommand(NetworkThirdPersonController networkThirdPersonController, JoystickDetection joystickDetection)
    {
        this.networkThirdPersonController = networkThirdPersonController;
        this.joystickDetection = joystickDetection;
    }

    public void Execute()
    {
        Vector2 input = joystickDetection.GetInputValue();
        
        if (networkThirdPersonController != null)
        {
            networkThirdPersonController.Move(input);
        }
        else if (thirdPersonController != null)
        {
            thirdPersonController.Move(input);
        }
        else if (movement != null)
        {
            movement.Move(input);
        }
        else if (playerMovementSinglePlayer != null)
        {
            playerMovementSinglePlayer.Move(input);
        }
    }

    public void Undo()
    {
        throw new System.NotImplementedException();
    }
}
