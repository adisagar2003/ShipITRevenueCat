using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class SceneShift : MonoBehaviour
{
    [SerializeField] private string secondSceneName;

    public void MoveToNextLevel()
    {
       SceneManager.LoadScene(secondSceneName, LoadSceneMode.Single);
    }
}
