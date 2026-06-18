using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string realSaveTime;

    // DateTimeManager 저장
    [Header("Date Data")]
    public int          currentWeek;
    public DayOfWeek    currentDay;
    public TimeOfDay    currentTime;
    public int          day;
    public bool         isWorkCompleted;
    public List<string> talkedNpcsToday;
    public List<int>    talkedEmployeeIdsThisWeek;
    public float        playTime;

    // Employee 저장
    public List<EmployeeSaveData> savedEmployees = new List<EmployeeSaveData>();

    // Company 저장
    [Header("Company Data")]
    public string company_Name;
    public int    company_Gold;
    public int    company_Level;
    public int    company_Popularity;
    public int    company_Reputation;
    public int    company_WeeklyCost;
    public int    company_DailyProfit;
    public int    company_WeeklyProfit;
    public int    company_TotalRevenue;

    // 완료된 프로젝트 목록 TODO - 프로젝트가 완료 될때 여기로 넣어주세요
    public List<ProjectCompletedSaveData> completedProjectsData = new();
    // 현재 진행 중인 프로젝트 목록
    public CurrentProjectSaveData activeProjectsData = new();
}

[System.Serializable]
public class EmployeeSaveData
{
    public int employeeId;

    public int ability;
    public int property1;
    public int property2;
    public int property3;

    public int desire;
    public int loyalty;
    public int fatigue;

    public int preDesire;
    public int preLoyalty;
    public int preFatigue;

    public EmployeeWorkStatus workStatus;

    public int    trainingRemainingWeeks;
    public int    trainingStartedWeek;
    public string trainingCourseName;
    public int    trainingCost;
    public int    trainingMinAbilityDelta;
    public int    trainingMaxAbilityDelta;
    public float  trainingFailureRate;
    public EmployeeMutableData trainingStartData;
}

[System.Serializable]
public class CurrentProjectSaveData
{
    // so 대신에 프로젝트 존재 여부와 규모를 확인
    public bool hasActiveProject;
    public ProjectSize project_Scale;

    [Header(" 런타임 데이터 ")]
    public int    project_day;
    public string project_userNamed;
    public int    project_NightCount;
    public string project_Genre;
    public string project_ArtStyle;
    public string project_Engine;

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

[Serializable]
public class ProjectCompletedSaveData
{
    public string      projectName;
    public ProjectSize scale;
    public int         qualityScore;
    public int         stabilityScore;
    public int         charmScore;
    public string      grade; 

    public float rating;
    public float retentionFactor;
    public int   users;
    public int   dailySales;
    public int   goodsSales;
    public int   dailyGold;
    public int   dailyCost;
    public int   weeklyGoldAccum;
    public int   prevWeekUsers;
    public int   prevWeekGold;

    public List<int> weeklyGoldHistoryList;
    public bool isServiceOver;
}