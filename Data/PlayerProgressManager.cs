using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerProgressData
{
    public int totalRoomsCleared = 0;
    //总开始次数
    public int totalRunsStarted = 0;
    public int permanentHealthBonus = 0;
    public int permanentAttackBonus = 0;
    public int permanentDefenseBonus = 0;
    public float permanentAttackRangeBonus = 0f;
    public int totalDeaths = 0;
    public int currentRunRoomsCleared = 0;
    public int bestRunRoomsCleared = 0;
    
    //获得的buff次数
    public int healthBuffCount = 0;
    public int attackBuffCount = 0;
    public int defenseBuffCount = 0;
    public int rangeBuffCount = 0;
    public int healBuffCount = 0; 
}

public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance { get; private set; }
    
    [Header("Progress Data")]
    public PlayerProgressData progressData = new PlayerProgressData();
    
    private const string SAVE_KEY = "PlayerProgressData";
    
    void Awake()
    {
        //单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgressData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    
    //房间清理完成
    public void OnRoomCleared(PlayerBuffManager.BuffType buffType)
    {
        progressData.totalRoomsCleared++;
        progressData.currentRunRoomsCleared++;
        
        //根据buff类型更新永久属性
        ApplyPermanentBuff(buffType);
        
        //更新最佳记录
        if (progressData.currentRunRoomsCleared > progressData.bestRunRoomsCleared)
        {
            progressData.bestRunRoomsCleared = progressData.currentRunRoomsCleared;
        }
        
        SaveProgressData();
        Debug.Log($"房间清理完成! 总清理房间数: {progressData.totalRoomsCleared}, 当前连续: {progressData.currentRunRoomsCleared}");
    }
    
    //应用永久buff效果
    private void ApplyPermanentBuff(PlayerBuffManager.BuffType buffType)
    {
        switch (buffType)
        {
            case PlayerBuffManager.BuffType.IncreaseMaxHealth:
                progressData.permanentHealthBonus += 10;
                progressData.healthBuffCount++;
                break;
            case PlayerBuffManager.BuffType.IncreaseAttackPower:
                progressData.permanentAttackBonus += 5; 
                progressData.attackBuffCount++;
                break;
            case PlayerBuffManager.BuffType.IncreaseDefensePower:
                progressData.permanentDefenseBonus += 1;
                progressData.defenseBuffCount++;
                break;
            case PlayerBuffManager.BuffType.IncreaseAttackRange:
                progressData.permanentAttackRangeBonus += 0.5f;
                progressData.rangeBuffCount++;
                break;
            case PlayerBuffManager.BuffType.Heal:
                progressData.healBuffCount++; //只统计使用次数，没有永久效果
                break;
        }
    }
    
    //玩家死亡
    public void OnPlayerDeath()
    {
        progressData.totalDeaths++;
        progressData.currentRunRoomsCleared = 0; 
        SaveProgressData();
        Debug.Log($"玩家死亡! 总死亡次数: {progressData.totalDeaths}");
    }
    
    //开始新游戏
    public void OnStartNewGame()
    {
        progressData.totalRunsStarted++;
        progressData.currentRunRoomsCleared = 0;
        SaveProgressData();
    }
    
    //获取玩家的永久加成后的基础属性
    public (int health, int attackLight, int attackHeavy, int defense, float range) GetPermanentStats()
    {
        //基础属性（与Player.cs中的初始值保持一致）
        int baseHealth = 100;
        int baseAttackLight = 10;
        int baseAttackHeavy = 20;
        int baseDefense = 5;
        float baseRange = 2f;
        
        //应用永久加成
        int totalHealth = baseHealth + progressData.permanentHealthBonus;
        int totalAttackLight = baseAttackLight + progressData.permanentAttackBonus;
        int totalAttackHeavy = baseAttackHeavy + (progressData.permanentAttackBonus * 2); 
        int totalDefense = baseDefense + progressData.permanentDefenseBonus;
        float totalRange = baseRange + progressData.permanentAttackRangeBonus;
        
        return (totalHealth, totalAttackLight, totalAttackHeavy, totalDefense, totalRange);
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            //ResetProgressData();
        }
    }


    //保存进度数据
    private void SaveProgressData()
    {
        string jsonData = JsonUtility.ToJson(progressData);
        PlayerPrefs.SetString(SAVE_KEY, jsonData);
        PlayerPrefs.Save();
    }
    

    //加载进度数据
    private void LoadProgressData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SAVE_KEY);
            progressData = JsonUtility.FromJson<PlayerProgressData>(jsonData);
            Debug.Log("进度数据加载成功!");
        }
        else
        {
            Debug.Log("没有找到保存的进度数据，使用默认值。");
        }
    }
    

    //重置
    [ContextMenu("Reset Progress Data")]
    public void ResetProgressData()
    {
        progressData = new PlayerProgressData();
        SaveProgressData();
        Debug.Log("进度数据已重置!");
    }
    

    //获取统计信息文本
    public string GetStatsText()
    {
        var stats = GetPermanentStats();
        return $"=== Player Statistics ===\n" +
               $"Total Rooms Cleared: {progressData.totalRoomsCleared}\n" +
               $"Total Runs Started: {progressData.totalRunsStarted}\n" +
               $"Best RunRooms Cleared: {progressData.bestRunRoomsCleared}\n" +
               $"Total Deaths: {progressData.totalDeaths}\n\n" +
               $"=== Permenent Stats ===\n" +
               $"Health: {stats.health} (+{progressData.permanentHealthBonus})\n" +
               $"Attack Light: {stats.attackLight} (+{progressData.permanentAttackBonus})\n" +
               $"Attack Heavy: {stats.attackHeavy} (+{progressData.permanentAttackBonus * 2})\n" +
               $"Defense: {stats.defense} (+{progressData.permanentDefenseBonus})\n" +
               $"Range: {stats.range:F1} (+{progressData.permanentAttackRangeBonus:F1})\n\n" +
               $"=== Buff Times ===\n" +
               $"Health Buff Count: {progressData.healthBuffCount} times\n" +
               $"Attack Buff Count: {progressData.attackBuffCount} times\n" +
               $"Defense Buff Count: {progressData.defenseBuffCount} times\n" +
               $"Range Buff Count: {progressData.rangeBuffCount} times\n" +
               $"Heal Buff Count: {progressData.healBuffCount} times";
    }
}