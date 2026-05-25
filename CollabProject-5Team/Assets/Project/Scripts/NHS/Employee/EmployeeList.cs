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
        
    public EmployeeList(List<EmployeeImmutableData> rawData)
    {
         _allEmployees.Clear();
        _leftEmployees.Clear();

        for (int i = 0; i < rawData.Count; i++)
        {
            Employee newEmployee = new Employee(rawData[i]);

            _allEmployees.Add(newEmployee);
        } 

        for (int i=0;i<allEmployees.Count;i++)
        {
            Employee employee = _allEmployees[i];
            int employeeId = employee.ImmutableData.id;

            _leftEmployees.Add(employeeId, employee);
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
