using UnityEngine;

// 직원 게임오브젝트에 부착 방식
public class Employee : MonoBehaviour
{
    [Header("기본 데이터 (SO 할당)")]
    public EmployeeImmutableData so;

    public EmployeeMutableData   MutableData;

    [Header("플래그")]
    public bool hasTalkedThisWeek;

    public EmployeeWorkStatus WorkStatus;

    public void Init()
    {
        MutableData = new EmployeeMutableData
        {
            ability   = so.ability,
            property1 = so.ability,
            property2 = so.ability,
            property3 = so.ability,
            desire    = so.desire,
            loyalty   = so.loyalty,
            fatigue   = so.fatigue,

             preDesire = so.desire,
            preLoyalty = so.loyalty,
            preFatigue = so.fatigue
        };
    }

    // 주 능력치 변경 시 property도 같이 초기화
    public void UpdateStat(int newStat)
    {
        MutableData.ability   = newStat;
        MutableData.property1 = newStat;
        MutableData.property2 = newStat;
        MutableData.property3 = newStat;
    }

    public void AddAbilityDelta(int delta)
    {
        int ability = MutableData.ability;
        int adjustedDelta = delta;

        if (delta > 0)
        {
            float growthRate = ability <= 40 ? 1.0f :
                               ability <= 60 ? 0.8f :
                               ability <= 80 ? 0.6f : 0.4f;
            adjustedDelta = Mathf.CeilToInt(delta * growthRate);//올림
        }

        MutableData.ability = ability + adjustedDelta;
        MutableData.property1 = MutableData.ability;
        MutableData.property2 = MutableData.ability;
        MutableData.property3 = MutableData.ability;
    }

    public void SaveCurrentData()
    {
        MutableData.preDesire  = MutableData.desire;
        MutableData.preFatigue = MutableData.fatigue;
        MutableData.preLoyalty = MutableData.loyalty;
    }
}
