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
        //int baseProperty = PerkPolicy.CalcBaseProperty(so.ability);

        MutableData = new EmployeeMutableData
        {
            ability   = so.ability,
            property1 = so.ability,
            property2 = so.ability,
            property3 = so.ability,
            desire    = so.desire,
            loyalty   = so.loyalty,
            fatigue   = so.fatigue,
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

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[직원 클릭됨] 이름: {so.Name} | 현재 피로도: {MutableData.fatigue}");
    }
}
