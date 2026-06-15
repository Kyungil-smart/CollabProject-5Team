using System.Collections.Generic;
using UnityEngine;

public class HaveEmployees
{
    public List<Employee> haveEmployeeList = new();
    public List<Employee> standbyEmployees = new();
    public List<Employee> projectEmployees = new();
    public List<Employee> trainingEmployees = new();

    public void AddEmployee(Employee employee)
    {
        haveEmployeeList.Add(employee);
        SetStatus(employee, EmployeeWorkStatus.Standby);
        Debug.Log($"[인사] {employee.so.Name} 직원이 입사했습니다.");
    }

    public void RemoveEmployee(Employee employee)
    {
        haveEmployeeList.Remove(employee);
        standbyEmployees.Remove(employee);
        projectEmployees.Remove(employee);
        trainingEmployees.Remove(employee);
        Debug.Log($"[인사] {employee.so.Name} 직원이 퇴사했습니다.");
    }

    public void Clear()
    {
        haveEmployeeList.Clear();
        standbyEmployees.Clear();
        projectEmployees.Clear();
        trainingEmployees.Clear();
    }

    // 한 직원은 한 상태 리스트에만 존재하도록 정리한 뒤 새 상태에 넣는다.
    public void SetStatus(Employee employee, EmployeeWorkStatus status)
    {
        standbyEmployees.Remove(employee);
        projectEmployees.Remove(employee);
        trainingEmployees.Remove(employee);

        employee.WorkStatus = status;
        GetList(status).Add(employee);
    }

    List<Employee> GetList(EmployeeWorkStatus status)
    {
        return status switch
        {
            EmployeeWorkStatus.InProject => projectEmployees,
            EmployeeWorkStatus.InTraining => trainingEmployees,
            _ => standbyEmployees,
        };
    }
}
