using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System;
using System.Collections;
/// <summary>
/// Handles UI Button jump press, forwarding it to the player's Jump() method.
/// Attach this to your UI Jump Button.
/// </summary>
public class JumpButtonDetection : NetworkBehaviour
{
    private NetworkThirdPersonController playerMovement;
    private PlayerAnimationHandle playerAnimationHandle;
    private InputManager inputManager;
    private ICommand jumpCommand;
    private Button jumpButton;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        GameObject jbObject = GameObject.Find("JumpButton");
        if (jbObject != null)
        {
            jumpButton = jbObject.GetComponent<Button>();
            if (jumpButton == null) Debug.Log("<color=red>Jump button not found.</color>");
            if (jumpButton != null) Debug.Log("<color=green>Jump button found.</color>");
            jumpButton.onClick.AddListener(OnJumpButtonPressed);
        }
        else
        {
            StartCoroutine(KeepCheckingForJumpButton());
        }

        inputManager = GetComponent<InputManager>();
        playerAnimationHandle = GetComponent<PlayerAnimationHandle>();
        playerMovement = GetComponent<NetworkThirdPersonController>();
        jumpCommand = new JumpCommand(playerMovement, inputManager);
    }

    public IEnumerator KeepCheckingForJumpButton()
    {
        while (jumpButton == null)
        {
            GameObject jbObject = GameObject.Find("JumpButton");
            if (jbObject != null)
            {
                jumpButton = jbObject.GetComponent<Button>();
            }
            if (jumpButton != null)
            {
                jumpButton.onClick.AddListener(OnJumpButtonPressed);
                Debug.Log("JumpButton found and listener assigned.");
                yield break;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void OnDisable()
    {
        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpButtonPressed);
    }

    // FOR DEVELOPMENT PURPOSE
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJumpButtonPressed();
        }
    }

    public void OnJumpButtonPressed()
    {
        inputManager.SetJumpPressed();
        jumpCommand.Execute();
    }
}
