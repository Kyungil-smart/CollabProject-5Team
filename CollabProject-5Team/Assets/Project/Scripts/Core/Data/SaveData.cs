using System;

// 세이브 데이터
[Serializable]
public class SaveData
{
    public Project[] projects; // 진행중인 프로젝트 상태

    // 회사 상태
    public string companyName;
    public int companyDay;
    public int companyGold;
    public int companyLevel;

    // 직원 상태
    //public EmployeeList employeeList;
}