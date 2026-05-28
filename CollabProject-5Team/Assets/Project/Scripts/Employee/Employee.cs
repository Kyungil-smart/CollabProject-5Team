using UnityEngine;
using UnityEngine.EventSystems;

// 직원 게임오브젝트에 부착 방식
public class Employee : MonoBehaviour, IPointerClickHandler
{
    public EmployeeMutableData   MutableData;
    [Header("직원 기본 데이터 (SO 할당)")]
    public EmployeeImmutableData so;

    public void Init()
    {
        int baseProperty = PerkPolicy.CalcBaseProperty(so.ability);

        MutableData = new EmployeeMutableData
        {
            ability   = so.ability,
            property1 = baseProperty,
            property2 = baseProperty,
            property3 = baseProperty,
            desire    = so.desire,
            loyalty   = so.loyalty,
            fatigue   = so.fatigue,
        };
    }

    // 주 능력치(stat) 변경 후 세부 능력치 재계산
    public void UpdateStat(int newStat)
    {
        MutableData.ability = newStat;
        RecalcProperties();
    }

    // 추가 변동값(bonus) 변경 후 세부 능력치 재계산
    public void UpdateBonus(int b1, int b2, int b3)
    {
        MutableData.bonus1 += b1;
        MutableData.bonus2 += b2;
        MutableData.bonus3 += b3;
        RecalcProperties();
    }

    // 세부 능력치 재계산: 기본값(stat 기반) + 추가 변동값
    void RecalcProperties()
    {
        MutableData.property1 = PerkPolicy.CalcFinalProperty(MutableData.ability, MutableData.bonus1);
        MutableData.property2 = PerkPolicy.CalcFinalProperty(MutableData.ability, MutableData.bonus2);
        MutableData.property3 = PerkPolicy.CalcFinalProperty(MutableData.ability, MutableData.bonus3);

        Debug.Log($"[{so.Name}] stat={MutableData.ability} " +
                  $"→ p1={MutableData.property1}(+{MutableData.bonus1}) " +
                  $"p2={MutableData.property2}(+{MutableData.bonus2}) " +
                  $"p3={MutableData.property3}(+{MutableData.bonus3})");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[직원 클릭됨] 이름: {so.Name} | 현재 피로도: {MutableData.fatigue}");
    }
}
