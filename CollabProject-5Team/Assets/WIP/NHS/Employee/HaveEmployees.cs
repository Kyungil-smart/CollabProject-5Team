    using UnityEngine;
    using System.Collections.Generic;

    public class HaveEmployees
    {
        public List<Employee>  haveEmployeeList => _haveEmployeeList;
        private List<Employee> _haveEmployeeList = new List<Employee>();

        public void AddEmployee(Employee target)
        {
            if (target == null) return;

            _haveEmployeeList.Add(target);

            Debug.Log($"[인사과] {target.so.employeeName} 직원이 정식 발령되었습니다.");
        }

        public bool RemoveEmployee(Employee target)
        {
            if (target == null) return false;

            bool removed = _haveEmployeeList.Remove(target);
            if (removed)
                Debug.Log($"[인사과] {target.so.employeeName} 직원이 명단에서 제거되었습니다.");
            return removed;
        }
    }
