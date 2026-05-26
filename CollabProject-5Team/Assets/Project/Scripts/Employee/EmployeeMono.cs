using UnityEngine;
using UnityEngine.EventSystems;

// Employee의 MonoBehaviour 래퍼 클래스
// 직원 오브젝트에 붙여서 IClickable 처리 및 인스펙터 설정담당
public class EmployeeMono : MonoBehaviour, IPointerClickHandler
{
    public Employee employee;

    public void Init(Employee employee)
    {
        this.employee = employee;
    }

    // 주 능력치(stat) 변경 후 세부 능력치 재계산
    public void UpdateStat(int newStat)
    {
        var d = employee.MutableData;
        d.baseStat = newStat;
        employee.MutableData = d;
        RecalcProperties();
    }

    // 추가 변동값(bonus) 변경 후 세부 능력치 재계산
    public void UpdateBonus(int b1, int b2, int b3)
    {
        var d = employee.MutableData;
        d.bonus1 += b1;
        d.bonus2 += b2;
        d.bonus3 += b3;
        employee.MutableData = d;
        RecalcProperties();
    }

    // 세부 능력치 재계산: 기본값(stat 기반) + 추가 변동값
    void RecalcProperties()
    {
        var d = employee.MutableData;
        d.property1 = PerkPolicy.CalcFinalProperty(d.baseStat, d.bonus1);
        d.property2 = PerkPolicy.CalcFinalProperty(d.baseStat, d.bonus2);
        d.property3 = PerkPolicy.CalcFinalProperty(d.baseStat, d.bonus3);
        employee.MutableData = d;

        Debug.Log($"[{employee.ImmutableData.employeeName}] stat={d.baseStat} " +
                  $"→ p1={d.property1}(+{d.bonus1}) " +
                  $"p2={d.property2}(+{d.bonus2}) " +
                  $"p3={d.property3}(+{d.bonus3})");
    }

    // IClickable 구현
    public void OnPointerClick(PointerEventData eventData)
    {
        employee?.OnClick();
    }
}
