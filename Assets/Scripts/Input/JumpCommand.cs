public class JumpCommand : ICommand
{
    private PlayerMovement playerMovement;
    private ThirdPersonController thirdPersonController;
    private NetworkThirdPersonController networkThirdPersonController;
    private PlayerAnimationHandle playerAnimationHandle;
    private JoystickDetection joystickDetection;

    // Constructor for PlayerMovement
    public JumpCommand(PlayerMovement playerMovement, PlayerAnimationHandle playerAnimationHandle, JoystickDetection joystickDetection)
    {
        this.playerMovement = playerMovement;
        this.playerAnimationHandle = playerAnimationHandle;
        this.joystickDetection = joystickDetection;
    }

    // Constructor for ThirdPersonController
    public JumpCommand(ThirdPersonController thirdPersonController, JoystickDetection joystickDetection)
    {
        this.thirdPersonController = thirdPersonController;
        this.joystickDetection = joystickDetection;
    }

    // Constructor for NetworkThirdPersonController
    public JumpCommand(NetworkThirdPersonController networkThirdPersonController, JoystickDetection joystickDetection)
    {
        this.networkThirdPersonController = networkThirdPersonController;
        this.joystickDetection = joystickDetection;
    }

    public void Execute()
    {
        if (joystickDetection.GetJumpPressed())
        {
            if (playerMovement != null)
            {
                playerMovement.Jump();
                playerAnimationHandle?.TriggerJump();
            }
            else if (thirdPersonController != null)
            {
                thirdPersonController.Jump();
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
