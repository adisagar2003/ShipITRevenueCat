using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Timer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float startingTime = 60f;

    private NetworkVariable<float> levelTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool timerRunning = false;

    private void Update()
    {
        if (!IsServer) return;

        if (timerRunning && levelTimer.Value > 0f)
        {
            levelTimer.Value -= Time.deltaTime;
            if (levelTimer.Value <= 0f)
            {
                levelTimer.Value = 0f;
                timerRunning = false;
                Debug.Log("Timer has ended!");
            }
        }
    }

    private void OnGUI()
    {
        int timeLeft = Mathf.CeilToInt(levelTimer.Value);
        GUI.Label(new Rect(20, 20, 200, 40), $"Time Left: {timeLeft}");
    }

    [ContextMenu("Start Timer")]
    public void StartTimer()
    {
        if (!IsServer) return;

        if (levelTimer.Value <= 0f)
        {
            levelTimer.Value = startingTime;
        }
        timerRunning = true;
        Debug.Log("Timer started.");
    }

    public void StopTimer()
    {
        if (!IsServer) return;

        timerRunning = false;
        Debug.Log("Timer stopped.");
    }

    public void ResetTimer()
    {
        if (!IsServer) return;

        levelTimer.Value = startingTime;
        timerRunning = false;
        Debug.Log("Timer reset.");
    }

}