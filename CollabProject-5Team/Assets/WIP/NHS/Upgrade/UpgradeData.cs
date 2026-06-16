using UnityEngine;  
using System.Collections.Generic;

[System.Serializable]
public struct UpgradeRequired
{
    public int gold;
    public int dailyGold;
    public int reputation;
    public int loyality;

    public UpgradeRequired(int gold, int dailyGold, int reputation, int loyality)
    {
        this.gold       = gold;
        this.dailyGold  = dailyGold;
        this.reputation = reputation;
        this.loyality   = loyality;
    }
}
public class UpgradeData
{
    public List<UpgradeRequired> UpgradeRequiredDatas = new List<UpgradeRequired>();

    public void Init()
    {
        UpgradeRequiredDatas.Clear();

        UpgradeRequiredDatas.Add(new UpgradeRequired(    0, 200,  0, 0));
        UpgradeRequiredDatas.Add(new UpgradeRequired( 5000, 400, 10, 0));
        UpgradeRequiredDatas.Add(new UpgradeRequired(12000, 800, 20, 0));

        Debug.Log($"UpgradeData 초기화 완료: {UpgradeRequiredDatas.Count} 단계");
    }
}