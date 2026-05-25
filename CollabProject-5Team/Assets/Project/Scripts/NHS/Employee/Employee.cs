using UnityEngine;
using System;

public class Employee : IClickable
{
    public EmployeeImmutableData ImmutableData { get; private set; }
    public   EmployeeMutableData   MutableData { get; set; }
    public Employee(EmployeeImmutableData rawData)
    {
        this.ImmutableData = rawData;

        this.MutableData = new EmployeeMutableData
        {
            property1  = 10,
            property2  = 10,
            property3  = 100,
            motivation = 50, 
            loyalty    = 50,    
            fatigue    = 0      
        };
    }

    public void OnClick()
    {
        Debug.Log($"[직원 클릭됨] 이름: {ImmutableData.name} | 현재 피로도: {MutableData.fatigue}");
    }
}