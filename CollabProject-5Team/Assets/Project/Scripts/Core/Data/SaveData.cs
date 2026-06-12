using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // DateTimeManager 저장
    [Header("Date Data")]
    public int          currentWeek;
    public DayOfWeek    currentDay;
    public TimeOfDay    currentTime;
    public int          day;
    public bool         isWorkCompleted;
    public List<string> talkedNpcsToday;
    public List<int>    talkedEmployeeIdsThisWeek;

    // Company 저장
    [Header("Company Data")]
    public string company_Name;
    public int    company_Gold;
    public int    company_Level;
    public int    company_Popularity;
    public int    company_Reputation;
    public int    company_DailyCost;
    public int    company_DailyProfit;
    public int    company_weeklyProfit;

    // 완료된 프로젝트 목록 TODO - 프로젝트가 완료 될때 여기로 넣어주세요
    public List<ProjectCompleted> completedProjectsData = new();
    // 현재 진행 중인 프로젝트 목록
    public CurrentProjectSaveData activeProjectsData = new();
}

[System.Serializable]
public class CurrentProjectSaveData
{
    [Header("초기값 데이터")]
    public int         project_Id;
    public string      project_Name;   
    public string      project_Desc;
    public ProjectSize project_Scale;
    public int         project_RequiredCost;
    public int         project_MaxEmployeePerpart;            
    public int         project_DurationDays;

    [Header(" 런타임 데이터 ")]
    public int project_day;
    public string project_userNamed;

    // 투입된 직원
    public List<int> project_PlanningEmployeeIds;
    public List<int> project_ProgrammerEmployeeIds;
    public List<int> project_ArtistEmployeeIds;

    // 진행도 연관 수치
    public float project_CurScore;

    public float project_QualityScore;
    public float project_StabilityScore;
    public float project_CharmScore;
}