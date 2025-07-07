using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This gameObject follows the x and z values of player's position
/// </summary>
public class SpringFollowPlayer : MonoBehaviour
{
    // Start is called before the first frame updat
    [SerializeField] private GameObject playerMesh;
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = new Vector3(
            playerMesh.transform.position.x,
            transform.position.y,
            playerMesh.transform.position.z
         );
    }
}
