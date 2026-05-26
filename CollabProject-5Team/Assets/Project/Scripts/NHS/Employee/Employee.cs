using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Employee : IClickable
{
    public EmployeeImmutableData ImmutableData { get; private set; }
    public   EmployeeMutableData   MutableData { get; set; }
    public Employee(EmployeeImmutableData soData)
    {
        this.ImmutableData = soData;

        this.MutableData = new EmployeeMutableData
        {
            property1  = soData.initProperty1,
            property2  = soData.initProperty2,
            property3  = soData.initProperty3,

            motivation = 50, 
            loyalty    = 50,    
            fatigue    = 0      
        };
    }

    public void OnClick()
    {
        Debug.Log($"[직원 클릭됨] 이름: {ImmutableData.employeeName} | 현재 피로도: {MutableData.fatigue}");
    }
}