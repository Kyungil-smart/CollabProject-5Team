using System;
using System.Collections.Generic;
using UnityEngine;
using static ManagementStatusData;

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
    public float        playTime;

    // Employee 저장
    public List<EmployeeSaveData> savedEmployees = new List<EmployeeSaveData>();
    public int lastHiredEmployeeId;
    public bool canLeaveSelf;
    public List<int> leavePendingEmployeeIds = new();

    // QuestManager 저장
    public Dictionary<Role, int> weeklyBonusPoints = new();
    public List<int> completedStoryQuestIds = new();
    public int curSpyQuestID;
    public int selectedSpyEmployeeId;

    // Company 저장
    [Header("Company Data")]
    public string company_PlayerName;
    public string company_CompanyName;
    public int    company_Gold;
    public int    company_Level;
    public int    company_Popularity;
    public int    company_Reputation;
    public int    company_WeeklyCost;
    public int    company_DailyProfit;
    public int    company_WeeklyProfit;
    public int    company_TotalRevenue;
    public ManagementStatusData company_CurManagementStatus = new();
    public ManagementStatusData company_PrevManagementStatus = new();
    public ManagementStatusData company_CumulativeManagementStatus = new();

    // 완료된 프로젝트 목록
    public List<ProjectCompletedSaveData> completedProjectsData = new();
    // 현재 진행 중인 프로젝트 목록
    public CurrentProjectSaveData activeProjectsData = new();

    public List<AchievementSaveInfo> _achievementStates = new();
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

    public bool isSpy;

    public EmployeeWorkStatus workStatus;
    public bool hasTalkedThisWeek;
    public List<string> completedProjectNames = new();

    public int    trainingRemainingWeeks;
    public int    trainingStartedWeek;
    public string trainingCourseName;
    public int    trainingCost;
    public int    trainingMinAbilityDelta;
    public int    trainingMaxAbilityDelta;
    public float  trainingFailureRate;
    public EmployeeMutableData trainingStartData;
}

[Serializable]
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
    public string      genre;
    public string      artStyle;
    public string      engine;

    public float retentionFactor;
    public int   users;
    public int   dailySales;
    public int   dailyGold;
    public int   dailyCost;
    public int   weeklySales;
    public int   weeklyGoldAccum;
    public int   prevWeekUsers;
    public int   prevWeekGold;

    public List<int> weeklyGoldHistoryList;
    public bool isServiceOver;
    public bool isUpdatePending;
    public int planUpdateId;
    public int artUpdateId;
    public int devUpdateId;
    public bool planUpdateCompleted;
    public bool artUpdateCompleted;
    public bool devUpdateCompleted;
    public int planUpdateLockWeeks;
    public int artUpdateLockWeeks;
    public int devUpdateLockWeeks;

}

[Serializable]
public sealed class ManagementStatusData
{
    public int totalIncome;
    public int gameSales;
    public int otherIncome;
    public int totalExpense;
    public int laborCost;
    public int devCost;
    public int operatingCost;
    public int marketingCost;
    public int otherExpense;
    public int operatingProfit;

    public void Recalculate()
    {
        totalIncome = gameSales + otherIncome;
        totalExpense = laborCost + devCost + operatingCost + marketingCost + otherExpense;
        operatingProfit = totalIncome - totalExpense;
    }

    public void Clear()
    {
        totalIncome = 0;
        gameSales = 0;
        otherIncome = 0;
        totalExpense = 0;
        laborCost = 0;
        devCost = 0;
        operatingCost = 0;
        marketingCost = 0;
        otherExpense = 0;
        operatingProfit = 0;
    }

    public ManagementStatusData Clone()
    {
        var clone = new ManagementStatusData
        {
            gameSales = gameSales,
            otherIncome = otherIncome,
            laborCost = laborCost,
            devCost = devCost,
            operatingCost = operatingCost,
            marketingCost = marketingCost,
            otherExpense = otherExpense
        };
        clone.Recalculate();
        return clone;
    }

    // 업적 세이브 데이터

    [Serializable]
    public class AchievementSaveInfo
    {
        public string id;
        public int currentValue;
        public bool isUnlocked;
    }
}
