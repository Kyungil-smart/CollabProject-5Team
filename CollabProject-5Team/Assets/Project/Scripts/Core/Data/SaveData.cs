using System;

// 세이브 데이터
[Serializable]
public class SaveData
{
    //회사
    public Company company;

    // 진행중인 프로젝트
    public Project[] projects;

    // 직원
    public HaveEmployees haveEmployees;
}