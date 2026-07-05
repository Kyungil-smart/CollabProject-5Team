using UnityEngine;

public enum AchievementType
{
    TotalGold,
    ReachGold,

    MaxLevel,

    HireEmployee,

    PlayTime
}

public class Achievement
{
    public string                    id;
    public string                 title;
    public string           description;

    public AchievementType         type;

    public bool              isUnlocked;
                          
    public int             currentValue;
    public int              targetValue;
}
