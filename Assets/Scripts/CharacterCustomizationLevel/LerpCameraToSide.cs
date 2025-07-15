using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpCameraToSide : MonoBehaviour
{
    [SerializeField] private Quaternion targetRotation = Quaternion.Euler(0, 23.0f, 0);
    float lerpTime = 1.0f;
    float speedOfCameraLooking = 100.0f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.rotation != targetRotation)
        {
            lerpTime += Time.deltaTime * speedOfCameraLooking;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime);
        }
    }
}
