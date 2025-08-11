using System;
using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")] public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Game References")] 
    public GameObject roomMapManager;

    [Header("Settings")] public string mainMenuSceneName = "MainMenu";
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;
    private bool canPause = true;
    

    private void Start()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey) && canPause)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (!canPause || isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if(!isPaused)
            return;
        isPaused = false;
        Time.timeScale = 1f;
        
        if(pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("Game Resumed");
    }

    public void RestartGame()
    {
        Debug.Log("Restarting Game...");
        Time.timeScale = 1f;
        
        RoomMapManager roomMapManagerScript = roomMapManager.GetComponent<RoomMapManager>();
        
        if (roomMapManagerScript != null)
        {
            isPaused = false;
            if(pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            roomMapManagerScript.RestartGame();
            Debug.Log("Game restarted successfully.");
        }
        else
        {
            Debug.LogError("RoomMapManager script not found on the assigned GameObject.");
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game from pause menu...");
        Time.timeScale = 1f;
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
