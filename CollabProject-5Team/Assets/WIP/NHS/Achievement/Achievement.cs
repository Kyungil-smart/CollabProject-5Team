public enum AchievementNotifyType
{
         OnGoldChanged,     
    OnTotalGoldChanged,
        OnLevelChanged,    
     OnPlayTimeChanged  
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