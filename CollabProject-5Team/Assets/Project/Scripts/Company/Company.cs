using R3;
using System;
using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    public string Name;
    public ReactiveProperty<int> day = new(0); // 현재 날짜
    public int gold;            // 보유 자금
    public int level;           // 회사 레벨

    public int ProjectSlots  // 값은 임의로 작성
    {
        get
        {
            if (level <= 1) return 1;
            else if (level <= 3) return 2;
            else return 3;
        }
    }

    public Project[] projects;  // 현재 진행중인 프로젝트들

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }

    #region 날짜 진행
    public void ProgressDay()
    {
        if (projects.Length == 0 || Array.TrueForAll(projects, p => p.isFinished.Value))
        {
            Debug.Log("[Company] 모든 프로젝트가 종료되어 날짜를 진행할 수 없습니다.");
            return;
        }

        day.Value++;
        foreach (var project in projects)
            project.ProgressDay();

        // 금요일 밤:
        if (day.Value % 5 == 0) ProgressNight();
    }
    public void ProgressNight()
    {
        foreach (var project in projects)
            project.ProgressNight();

        // TODO: 모든 밤 정산이 끝나고 보고서 이벤트 연결
    }

    static readonly string[] WeekDayNames = { "월요일", "화요일", "수요일", "목요일", "금요일" };
    public static string GetWeekDayName(int day) => WeekDayNames[day % 5];
    #endregion

    #region 프로젝트 관리
    public void StartNewProject(Project project)
    {
        if (!CanStartNewProject(project)) return;
        gold -= project.RequiredCost;

        projects[projects.Length] = project;
    }

    public bool CanStartNewProject(Project project)
    {
        if (projects.Length < ProjectSlots)
        {
            Debug.Log("프로젝트 슬롯이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        if (gold >= project.RequiredCost)
        {
            Debug.Log("보유 자금이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        return true;
    }
    #endregion
}
