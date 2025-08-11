using System.Collections;
using System.Collections.Generic;
using ooparts.dungen;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject deathUIPanel;
    public Button restartButton;
    public Button quitButton;
    public Button mainMenuButton;
    
    [Header("Game References")]
    public Player player;

    [Header("Settings")] 
    public string deathMessage = "You Died!";
    public bool pauseGameOnDeath = false;
    public GameObject roomMapManager;

    [Header("Scene Settings")] 
    public string mainMenuSceneName = "MainMenu";
    
    private void Start()
    {
        if (player == null)
        {
            FindPlayer();
        }

        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
            Debug.Log("Restart button initialized.");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("Quit button initialized.");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            Debug.Log("Main Menu button initialized.");
        }
        
    }

    public void ShowDeathUI()
    {
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(true);
            if (pauseGameOnDeath)
            {
                Time.timeScale = 0f; 
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Death UI is now visible.");
        }
        else
            Debug.LogWarning("Death UI Panel is not assigned.");
    }
    
    public void HideDeathUI()
    {
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
            if (pauseGameOnDeath)
            {
                Time.timeScale = 1f; 
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("Death UI is now hidden.");
        }
    }
    
    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        if(pauseGameOnDeath)
            Time.timeScale = 1f;
        HideDeathUI();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        RoomMapManager roomMapManagerScript = roomMapManager.GetComponent<RoomMapManager>();
        
        if (roomMapManagerScript != null)
        {
            roomMapManagerScript.RestartGame();
            Debug.Log("Game restarted successfully.");
        }
        else
        {
            Debug.LogError("RoomMapManager script not found on the assigned GameObject.");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        if (pauseGameOnDeath)
            Time.timeScale = 1f;
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 在编辑器中停止游戏
        #else
        Application.Quit(); // 在构建的游戏中退出
        #endif
    }
    
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        if (pauseGameOnDeath)
            Time.timeScale = 1f;
        
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    private void FindPlayer()
    {
        player = FindObjectOfType<Player>();
        
        if (player != null)
        {
            Debug.Log("Player found successfully in DeathUIManager!");
        }
        else
        {
            Debug.LogWarning("Player not found in scene. Searching by name...");
            
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<Player>();
                if (player != null)
                {
                    Debug.Log("Player found by name in DeathUIManager!");
                }
            }
            
            if (player == null)
            {
                GameObject playerByTag = GameObject.FindWithTag("Player");
                if (playerByTag != null)
                {
                    player = playerByTag.GetComponent<Player>();
                    if (player != null)
                    {
                        Debug.Log("Player found by tag in DeathUIManager!");
                    }
                }
            }
        }
    }

    public void SetPlayer(Player playerRef)
    {
        player = playerRef;
        Debug.Log("Player reference set in DeathUIManager");
    }
    
}
