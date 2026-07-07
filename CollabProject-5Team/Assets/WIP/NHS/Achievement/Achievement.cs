public enum AchievementNotifyType
{
            OnGoldChanged, // 완     
       OnTotalGoldChanged, // 완
    OnHireEmployeeChanged, // 완
    OnFireEmployeeChanged, // 완
           OnLevelChanged, // 완   
        OnPlayTimeChanged  // 완
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