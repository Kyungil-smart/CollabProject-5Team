using UnityEngine;
using UnityEngine.EventSystems;

// MonoBehaviour상속받은 Employee 래퍼 클래스
// Employee(순수 C#)의 제약 (클릭 이벤트, 인스펙터 활용등)을 보완하기 위해 존재한다.
public class EmployeeMono : MonoBehaviour, IPointerClickHandler
{
    public Employee Employee { get; private set; }

    public void Init(Employee employee)
    {
        Employee = employee;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Employee?.OnClick();
    }
}
