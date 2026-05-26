    using UnityEngine;
    using System.Collections.Generic;
    using JetBrains.Annotations;

    public class HaveEmployees
    {
         public List<Employee>  haveEmployeeList => _haveEmployeeList;
        private List<Employee> _haveEmployeeList = new List<Employee>();

    public void AddEmployee(Employee targetEmployee)
    {
        if (targetEmployee == null) return;

        _haveEmployeeList.Add(targetEmployee);

        Debug.Log($"[인사과] {targetEmployee.ImmutableData.employeeName} 직원이 정식 발령되었습니다.");
    }
}
