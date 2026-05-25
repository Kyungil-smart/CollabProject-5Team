using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    public int day;             // 현재 날짜
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
        day++;
        foreach (var project in projects)
            project.ProgressDay();

        // 금요일 밤:
        if (day % 5 == 0)
            ProgressNight();
    }
    public void ProgressNight()
    {
        foreach (var project in projects)
            project.ProgressNight();
    }
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
