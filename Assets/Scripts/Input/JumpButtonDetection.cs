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
    private PlayerMovement playerMovement;
    private PlayerAnimationHandle playerAnimationHandle;
    private ICommand jumpCommand;
    private Button jumpButton;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        GameObject jbObject = GameObject.Find("JumpButton");
        if (jbObject != null)
        {
            jumpButton = jbObject.GetComponent<Button>();
            jumpButton.onClick.AddListener(OnJumpButtonPressed);
        }
        else
        {
            StartCoroutine(KeepCheckingForJumpButton());
        }

        playerAnimationHandle = GetComponent<PlayerAnimationHandle>();
        playerMovement = GetComponent<PlayerMovement>();
        jumpCommand = new JumpCommand(playerMovement, playerAnimationHandle);
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
        jumpCommand.Execute();
    }
}
