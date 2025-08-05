using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : ICommand
{
    private PlayerMovement movement;
    private PlayerMovementSinglePlayer playerMovementSinglePlayer; // deprecated
    private NetworkThirdPersonController networkThirdPersonController;
    private InputManager inputManager;

    public MoveCommand(PlayerMovement movement, InputManager inputManager)
    {
        this.movement = movement;
        this.inputManager = inputManager;
    }

    public MoveCommand(PlayerMovementSinglePlayer movement, InputManager inputManager)
    {
        this.playerMovementSinglePlayer = movement;
        this.inputManager = inputManager;
    }

    public MoveCommand(NetworkThirdPersonController networkThirdPersonController, InputManager inputManager)
    {
        this.networkThirdPersonController = networkThirdPersonController;
        this.inputManager = inputManager;
    }

    public void Execute()
    {
        Vector2 input = inputManager.GetInputValue();

        if (networkThirdPersonController != null)
        {
            networkThirdPersonController.Move(input);
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
