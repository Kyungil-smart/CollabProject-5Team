using R3;
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

    private void Start()
    {
        Dialogue.DialogueEvents.OnStatChangeRequested
            .Subscribe(delta =>
            {
                Employee emp = _haveEmployees.haveEmployeeList
                    .Find(e => e.so.id == delta.employeeId);
                if (emp == null) return;

                var data = emp.MutableData;
                data.desire = Mathf.Clamp(data.desire + delta.desireDelta, 0, 100);
                data.fatigue = Mathf.Clamp(data.fatigue + delta.fatigueDelta, 0, 100);
                data.loyalty = Mathf.Clamp(data.loyalty + delta.loyaltyDelta, 0, 100);
                emp.MutableData = data;
            })
            .AddTo(this);
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

        return employee;
    }

    public bool FireEmployee(Employee employee)
    {
        if (employee == null)
        {
            Debug.LogWarning("[EM] 해고 실패: 대상 직원이 null입니다.");
            return false;
        }

        int id = employee.so.id;

        if (!_haveEmployees.RemoveEmployee(employee))
        {
            Debug.LogWarning($"[EM] 해고 실패: {employee.so.Name}은 고용 목록에 없습니다.");
            return false;
        }

        _employeeList.RestoreEmployee(id);
        Destroy(employee.gameObject);

        Debug.Log($"[EM] {employee.so.Name} 해고 프로세스 완료.");
        return true;
    }
}