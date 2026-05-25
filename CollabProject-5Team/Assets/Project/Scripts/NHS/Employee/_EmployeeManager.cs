using UnityEngine;

public class _EmployeeManager : MonoBehaviour
{
    public static _EmployeeManager Instance { get; private set; }

    public EmployeeList employeeList => _employeeList;
    private EmployeeList _employeeList;
    public HaveEmployees haveEmployees => _haveEmployees;
    private HaveEmployees _haveEmployees;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

         _employeeList = new EmployeeList();
        _haveEmployees = new HaveEmployees();
    }

    public void HireEmployee(int index)
    {
        if (!_employeeList.leftEmployees.ContainsKey(index))
        {
            Debug.LogWarning($"고용 실패: 마켓에 {index}번 직원이 없습니다.");
            return;
        }

        Employee target = _employeeList.leftEmployees[index];

        _haveEmployees.AddEmployee(target);

        _employeeList.DeleteEmployee(index);

        Debug.Log($"[시스템] {target.ImmutableData.name} 고용 프로세스 전과정 성공!");
    }
}
