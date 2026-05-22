using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class EmployeeList
{
     public IReadOnlyList<Employee>  allEmployees => _allEmployees;
    private List<Employee> _allEmployees = new List<Employee>();

    public IReadOnlyDictionary<int, Employee> leftEmployees => _leftEmployees;
    private Dictionary<int, Employee> _leftEmployees = new Dictionary<int, Employee>();
        
    public EmployeeList()
    {
        for(int i=0;i<10;i++)
        {
            //Employee temp = new Employee();
            //
            //_allEmployees.Add(temp);
        }

        for(int i=0;i<allEmployees.Count;i++)
        {
            _leftEmployees.Add(i, _allEmployees[i]);
        }
    }

    public void DeleteEmployee(int index)
    {
        if (_leftEmployees.ContainsKey(index))
        {
            _leftEmployees.Remove(index);
        }
        else
        {
            Debug.LogWarning($"[경고] {index}번 지원자는 이미 고용되었거나 존재하지 않습니다.");
        }
    }
}
