using UnityEngine;
using System.Collections.Generic;

public class _EmployeeManager : MonoBehaviour
{
    public static _EmployeeManager Instance { get; private set; }

    [Header("모든 직원 원본 프리팹")]
    [SerializeField] public GameObject[] allEmployeeObj;

    public EmployeeList   employeeList  => _employeeList;
    private EmployeeList  _employeeList;
    public  HaveEmployees haveEmployees => _haveEmployees;
    private HaveEmployees _haveEmployees;

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
        _haveEmployees = new HaveEmployees();

        _employeeList = new EmployeeList(allEmployeeObj);
        Debug.Log("[EM] 초기 직원 데이터를 로드 합니다.");
    }

    public Employee HireEmployee(int id)
    {
        if (!_employeeList.leftEmployees.ContainsKey(id))
        {
            Debug.LogWarning($"[EM] 고용 실패: 마켓에 {id}번 직원이 없습니다.");
            return null;
        }

        GameObject prefab = _employeeList.leftEmployees[id];
        GameObject employeeObject = Instantiate(prefab, Company.Instance.transform);

        Employee employee = employeeObject.GetComponent<Employee>();
        employee.Init();

        _haveEmployees.AddEmployee(employee);
        _employeeList.DeleteEmployee(id);

        Debug.Log($"[EM] {employee.so.employeeName} 고용 프로세스 전과정 성공!");
        return employee;
    }
}