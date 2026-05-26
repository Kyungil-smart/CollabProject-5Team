using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Employee : IClickable
{
    public EmployeeImmutableData ImmutableData { get; private set; }
    public   EmployeeMutableData   MutableData { get; set; }

    public Employee(EmployeeImmutableData soData)
    {
        this.ImmutableData = soData;

        int initStat = PerkPolicy.InitStatFromRank(soData.rankParsed);

        this.MutableData = new EmployeeMutableData
        {
            baseStat           = initStat,
            property1      = PerkPolicy.CalcBaseProperty(initStat),
            property2      = PerkPolicy.CalcBaseProperty(initStat),
            property3      = PerkPolicy.CalcBaseProperty(initStat),
            bonus1 = 0,
            bonus2 = 0,
            bonus3 = 0,
            motivation     = 50,
            loyalty        = 50,
            fatigue        = 0,
        };
    }

    public void OnClick()
    {
        Debug.Log($"[직원 클릭됨] 이름: {ImmutableData.employeeName} | 현재 피로도: {MutableData.fatigue}");
    }
}
