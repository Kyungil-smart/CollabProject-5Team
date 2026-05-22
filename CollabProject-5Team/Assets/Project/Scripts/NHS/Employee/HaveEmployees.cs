    using UnityEngine;
    using System.Collections.Generic;
    using JetBrains.Annotations;

    public class HaveEmployees
    {
         public List<Employee>  haveEmployeeList => _haveEmployeeList;
        private List<Employee> _haveEmployeeList = new List<Employee>();

        private Stats _totalStat;

        public void AddEmployee(int index)
        {
            
        }

        public Stats GetAllStats()
        {
            return _totalStat;
        }
    }
