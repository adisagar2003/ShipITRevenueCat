using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : ICommand
{
    private PlayerMovement movement;
    private PlayerMovementSinglePlayer playerMovementSinglePlayer;
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

    public void Execute()
    {

        if (movement != null) movement.Move(joystickDetection.GetInputValue()); 
        else
        {
            playerMovementSinglePlayer.Move(joystickDetection.GetInputValue());
        }
    }

    public void Undo()
    {
        throw new System.NotImplementedException();
    }
}
