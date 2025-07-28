using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Tile floats in the air, used for obstacles in the game.
/// Can move along the Y-axis (up/down) or Z-axis (forward/backward) using sine wave motion.
/// Movement is deterministic based on time and parameters, ensuring network synchronization.
/// </summary>
public class FloatingTile : NetworkBehaviour
{
    public enum MovementAxis
    {
        Y_Axis,
        Z_Axis
    }

    [Header("Movement Configuration")]
    [SerializeField] private MovementAxis movementAxis = MovementAxis.Y_Axis;
    [SerializeField] private float amplitude = 1.0f;    
    [SerializeField] private float frequency = 1.0f;     
    [SerializeField] private float phaseOffset = 0.0f;

    [Header("Optional Settings")]
    [SerializeField] private bool randomizeStartPosition = false;
    [SerializeField] private bool useLocalSpace = true;

    private Vector3 basePosition;
    
    private NetworkVariable<float> networkPhaseOffset = new NetworkVariable<float>(0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    private void Start()
    {
        basePosition = useLocalSpace ? transform.localPosition : transform.position;
        
        if (IsServer && randomizeStartPosition)
        {
            networkPhaseOffset.Value = Random.Range(0f, Mathf.PI * 2);
        }
    }

    private void Update()
    {
        float finalPhaseOffset = randomizeStartPosition ? networkPhaseOffset.Value : phaseOffset;
        float timeComponent = Time.time * frequency * Mathf.PI * 2 + finalPhaseOffset;
        float offset = amplitude * Mathf.Sin(timeComponent);
        
        Vector3 newPosition = basePosition;
        
        if (movementAxis == MovementAxis.Y_Axis)
        {
            newPosition.y = basePosition.y + offset;
        }
        else
        {
            newPosition.z = basePosition.z + offset;
        }
        
        if (useLocalSpace)
        {
            transform.localPosition = newPosition;
        }
        else
        {
            transform.position = newPosition;
        }
    }
}
