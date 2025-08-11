using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button startGameButton;
    public Button viewStatsButton;
    public Button exitGameButton;
    
    [Header("Stats Panel")]
    public GameObject statsPanel; // 统计面板
    public TextMeshProUGUI statsText; // 统计信息文本
    public Button closeStatsButton; // 关闭统计面板按钮
    public Button resetStatsButton; // 重置统计按钮
    
    [Header("Settings")]
    public string gameSceneName = "Game"; // 游戏场景名称
    
    void Start()
    {
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        if(viewStatsButton != null)
            viewStatsButton.onClick.AddListener(ViewStats);
        if (exitGameButton != null)
            exitGameButton.onClick.AddListener(ExitGame);
        if(closeStatsButton != null)
            closeStatsButton.onClick.AddListener(CloseStats);
        if(resetStatsButton != null)
            resetStatsButton.onClick.AddListener(ResetStats);
    }

    public void ResetStats()
    {
        Debug.Log("重置统计信息…");
        
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.ResetProgressData();
            if (statsText != null)
            {
                statsText.text = PlayerProgressManager.Instance.GetStatsText();
            }
        }
        else
        {
            Debug.LogWarning("PlayerProgressManager 实例未找到!");
        }
    }
    
    public void StartGame()
    {
        Debug.Log("加载游戏…");
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
    }
    
    public void ExitGame()
    {
        Debug.Log("退出游戏…");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    public void ViewStats()
    {
        Debug.Log("查看统计信息…");
        
        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
            
            // 更新统计信息文本
            if (statsText != null && PlayerProgressManager.Instance != null)
            {
                statsText.text = PlayerProgressManager.Instance.GetStatsText();
            }
            else if (statsText != null)
            {
                statsText.text = "未找到进度数据";
            }
        }
        else
        {
            Debug.LogWarning("Stats Panel 未设置!");
        }
    }
    
    public void CloseStats()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }
    
}
