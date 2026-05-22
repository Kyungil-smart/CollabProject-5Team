using UnityEngine;

public class _EmployeeManager : MonoBehaviour
{
    public static _EmployeeManager Instance { get; private set; }

     public EmployeeList  employeeList => _employeeList;
    private EmployeeList _employeeList;
     public HaveEmployees  haveEmployees => _haveEmployees;
    private HaveEmployees _haveEmployees;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

         _employeeList = new EmployeeList();
        _haveEmployees = new HaveEmployees();
    }
}
