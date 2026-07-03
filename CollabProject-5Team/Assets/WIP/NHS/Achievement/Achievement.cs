using UnityEngine;

public enum AchievementNotifyType
{
    OnGoldChanged,      // 골드가 변했을 때
    OnTotalGoldChanged, // 총 매출이 변했을 때
    OnLevelChanged,     // 회사 레벨이 변했을 때
    OnPlayTimeChanged   // 플레이타임이 정수(초) 단위로 누적될 때
}

[System.Serializable]
public class AchievementData
{
    public string id;
    public string title;
    public string description;

    public AchievementNotifyType targetEvent; 
    public int targetValue;                   

    [System.NonSerialized] public int currentValue = 0; 
    [System.NonSerialized] public bool isUnlocked = false;
}