using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausePlayManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    private bool isGamePaused = false;
    // Start is called before the first frame update
    void Awake()
    {
        pauseMenu.SetActive(false);
    }

    public void TogglePauseMenu()
    {
        isGamePaused = !isGamePaused;
        pauseMenu.SetActive(isGamePaused);
    }
    public void HidePausePanel()
    {
        isGamePaused = false;
        pauseMenu.SetActive(false);
    }
    public void ShowPausePanel()     {
        isGamePaused = true;
        pauseMenu.SetActive(true);
    }
}
