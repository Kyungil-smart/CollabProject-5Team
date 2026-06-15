using R3;
using UnityEngine;

public class _EmployeeManager : MonoBehaviour
{
    public static _EmployeeManager Instance { get; private set; }

    [Header("모든 직원 원본 프리팹")]
    public GameObject[] allEmployeeObj;

    [Header("기본적으로 고용되있는 직원 목록")]
    public Employee[] defaultEmployees;

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

        Dialogue.DialogueEvents.OnStatChangeRequested
            .Subscribe(delta =>
            {
                Employee emp = _haveEmployees.haveEmployeeList
                    .Find(e => e.so.id == delta.employeeId);
                if (emp == null) return;

                var data = emp.MutableData;
                data.desire  += delta.desireDelta;
                data.fatigue += delta.fatigueDelta;
                data.loyalty += delta.loyaltyDelta;
                emp.MutableData = data;
            })
            .AddTo(this);

        // 기본 직원 고용
        foreach (var emp in defaultEmployees)
        {
            if (_haveEmployees.haveEmployeeList.Exists(e => e.so.id == emp.so.id))
                continue;

            HireEmployee(emp.so.id);
        }
    }

    public Employee HireEmployee(int id)
    {
        if (!_employeeList.leftEmployees.ContainsKey(id))
        {
            Debug.LogWarning($"[EM] 고용 실패: 마켓에 {id}번 직원이 없습니다.");
            return null;
        }

        GameObject prefab = _employeeList.leftEmployees[id];
        //GameObject employeeObject = Instantiate(prefab, Company.Instance.transform);

        Employee employee = prefab.GetComponent<Employee>();
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

    public void ExportEmployeeData(SaveData data)
    {
        data.savedEmployees.Clear();

        for(int i=0;i<haveEmployees.haveEmployeeList.Count;i++)
        {
            Employee emp = haveEmployees.haveEmployeeList[i];

            if (emp == null || emp.so == null) continue;

            var empSave = new EmployeeSaveData
            {
                employeeId = emp.so.id,
                
                ability   = emp.MutableData.ability,
                property1 = emp.MutableData.property1,
                property2 = emp.MutableData.property2,
                property3 = emp.MutableData.property3,
                
                desire    = emp.MutableData.desire,
                loyalty   = emp.MutableData.loyalty,
                fatigue   = emp.MutableData.fatigue,
                
                preDesire  = emp.MutableData.preDesire,
                preLoyalty = emp.MutableData.preLoyalty,
                preFatigue = emp.MutableData.preFatigue
            };

            data.savedEmployees.Add(empSave);
        }
    }

    public void ImportEmployeeData(SaveData data)
    {
       if (data == null || data.savedEmployees == null) return;

        haveEmployees.haveEmployeeList.Clear();

        _employeeList = new EmployeeList(allEmployeeObj); 

        for (int i = 0; i < data.savedEmployees.Count; i++)
        {
            EmployeeSaveData empSave = data.savedEmployees[i];
            
            Employee hiredEmp = HireEmployee(empSave.employeeId);

            if (hiredEmp != null)
            {
                hiredEmp.MutableData = new EmployeeMutableData
                {
                    ability   = empSave.ability,
                    property1 = empSave.property1,
                    property2 = empSave.property2,
                    property3 = empSave.property3,
                    
                    desire    = empSave.desire,
                    loyalty   = empSave.loyalty,
                    fatigue   = empSave.fatigue,
                    
                    preDesire  = empSave.preDesire,
                    preLoyalty = empSave.preLoyalty,
                    preFatigue = empSave.preFatigue
                };
            }
            else
            {
                Debug.LogWarning($"세이브 복원 실패: ID {empSave.employeeId}번 직원을 찾을 수 없습니다.");
            }
        }
    }
}