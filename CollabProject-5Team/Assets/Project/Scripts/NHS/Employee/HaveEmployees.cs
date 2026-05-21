using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

public class HaveEmployees : MonoBehaviour
{
     public List<Employee>  HaveEmployeeList => _haveEmployeeList;
    private List<Employee> _haveEmployeeList = new List<Employee>();

    private Stats _totalStat;

    void Start()
    {
    }

    public void AddEmployee(int index)
    {

    }

    public Stats AllStats()
    {
        return _totalStat;
    }
}
