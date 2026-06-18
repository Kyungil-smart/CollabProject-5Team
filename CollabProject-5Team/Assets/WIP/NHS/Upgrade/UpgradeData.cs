using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OfficeUpgradeData
{
    public int Level;

    public int GoldCost;
    public int maintainCost;
    public int RequiredReputation;

    public int ReputationBonus;
    public int    LoyaltyBonus;

    public int MaxEmployee;

    public string Effects;
    public string LockCondition1;
    public string LockCondition2;

    public OfficeUpgradeData(int level, int gold, int maintain, int reqRep, int repBonus, 
                           int loyalty, int maxEmp, string effects, 
                           string lock1, string lock2)
    {
        Level              = level;
        GoldCost           = gold;
        maintainCost       = maintain;
        RequiredReputation = reqRep;
        ReputationBonus    = repBonus;
        LoyaltyBonus       = loyalty;
        MaxEmployee        = maxEmp;
        Effects            = effects;
        LockCondition1     = lock1;
        LockCondition2     = lock2;
    }
}

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Scriptable Objects/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] public List<OfficeUpgradeData> _datas;

    public OfficeUpgradeData GetData(int level)
    {
        return _datas.Find(x => x.Level == level);
    }

    public OfficeUpgradeData GetNextData(int currentLevel)
    {
        return GetData(currentLevel + 1);
    }
}