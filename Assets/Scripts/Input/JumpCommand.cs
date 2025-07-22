public class JumpCommand : ICommand
{
    private PlayerMovement playerMovement;
    private PlayerAnimationHandle playerAnimationHandle;

    public JumpCommand(PlayerMovement playerMovement, PlayerAnimationHandle playerAnimationHandle)
    {
        this.playerMovement = playerMovement;
        this.playerAnimationHandle = playerAnimationHandle;
    }

    public void Execute()
    {
        playerMovement.Jump();
        playerAnimationHandle.TriggerJump();
    }

    public void Undo()
    {
    }
}
