using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : ICommand
{
    private PlayerMovement movement;
    private JoystickDetection joystickDetection;

    public MoveCommand(PlayerMovement movement, JoystickDetection joystickDetection)
    {
        this.movement = movement;
        this.joystickDetection = joystickDetection;
    }

    public void Execute()
    {
      
        movement.Move(joystickDetection.GetInputValue());
    }

    public void Undo()
    {
        throw new System.NotImplementedException();
    }
}
