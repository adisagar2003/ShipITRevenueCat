using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
/// <summary>
/// Handles UI Button jump press, forwarding it to the player's Jump() method.
/// Attach this to your UI Jump Button.
/// </summary>
public class JumpButtonDetection : NetworkBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerAnimationHandle playerAnimationHandle;
    private ICommand jumpCommand;

    // need to bind button
    private Button jumpButton;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        jumpButton = GameObject.Find("JumpButton").GetComponent<Button>();
        playerAnimationHandle = GetComponent<PlayerAnimationHandle>();
        playerMovement = GetComponent<PlayerMovement>(); // inefficient but works for now.
        jumpCommand = new JumpCommand(playerMovement, playerAnimationHandle);

        // bind command
        jumpButton.onClick.AddListener(OnJumpButtonPressed);
    }
    private void OnDisable()
    {
        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpButtonPressed);
    }

    // FOR DEVELOPMENT PURPOSES
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJumpButtonPressed();
        }
    }

    public void OnJumpButtonPressed()
    {
        jumpCommand.Execute();
    }
}
