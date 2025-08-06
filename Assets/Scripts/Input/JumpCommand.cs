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
        bool jumpPressed = inputManager.GetJumpPressed();
        
        if (jumpPressed)
        {
#if DEBUG
            UnityEngine.Debug.Log($"<color=cyan>[JumpCommand]</color> <color=white>Jump input detected - executing jump</color>");
#endif
            
            if (playerMovement != null)
            {
#if DEBUG
                UnityEngine.Debug.Log($"<color=cyan>[JumpCommand]</color> <color=white>Using PlayerMovement.Jump()</color>");
#endif
                playerMovement.Jump();
                playerAnimationHandle?.TriggerJump();
            }
            else if (networkThirdPersonController != null)
            {
#if DEBUG
                UnityEngine.Debug.Log($"<color=cyan>[JumpCommand]</color> <color=white>Using NetworkThirdPersonController.Jump()</color>");
#endif
                networkThirdPersonController.Jump();
            }
            else
            {
                UnityEngine.Debug.LogWarning($"<color=red>[JumpCommand]</color> <color=white>No jump controller available!</color>");
            }
        }
    }

    public void Undo()
    {
    }
}
