public class JumpCommand : ICommand
{
    private PlayerMovement playerMovement;
    private NetworkThirdPersonController networkThirdPersonController;
    private PlayerAnimationHandle playerAnimationHandle;
    private InputManager inputManager;

    // Constructor for PlayerMovement
    public JumpCommand(PlayerMovement playerMovement, PlayerAnimationHandle playerAnimationHandle, InputManager inputManager)
    {
        this.playerMovement = playerMovement;
        this.playerAnimationHandle = playerAnimationHandle;
        this.inputManager = inputManager;
    }


    // Constructor for NetworkThirdPersonController
    public JumpCommand(NetworkThirdPersonController networkThirdPersonController, InputManager inputManager)
    {
        this.networkThirdPersonController = networkThirdPersonController;
        this.inputManager = inputManager;
    }

    public void Execute()
    {
        if (inputManager.GetJumpPressed())
        {
            if (playerMovement != null)
            {
                playerMovement.Jump();
                playerAnimationHandle?.TriggerJump();
            }
            else if (networkThirdPersonController != null)
            {
                networkThirdPersonController.Jump();
            }
        }
    }

    public void Undo()
    {
    }
}
