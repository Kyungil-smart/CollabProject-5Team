using R3;
using System.Collections.Generic;
using UnityEngine;

public class Company : MonoBehaviour
{
    public static Company Instance;

    public string Name;
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

    public List<Project> projects = new(); // 현재 진행중인 프로젝트들
    public Project curProject; // 메인 프로젝트 (UI에 집중적으로 표시)

    #region 싱글톤 설정
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    #endregion
    }
    private void Start()
    {
        InitProjects();
    }

    // 자식 오브젝트의 Project 컴포넌트를 수집해 projects 리스트에 세팅
    public void InitProjects()
    {
        projects.Clear();
        projects.AddRange(GetComponentsInChildren<Project>());
        curProject = projects[0];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 테스트용: 첫 번째 직원 고용
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[0].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);
            curProject.HireEmployee(hiredEmployee);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[1].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);
            curProject.HireEmployee(hiredEmployee);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            int firstEmployeeId = _EmployeeManager.Instance.allEmployeeObj[2].GetComponent<Employee>().so.id;
            Employee hiredEmployee = _EmployeeManager.Instance.HireEmployee(firstEmployeeId);
            curProject.HireEmployee(hiredEmployee);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) // 테스트용: 첫 번째 직원 해고
        {
            Employee target = curProject?.plannings[0];
            curProject.FireEmployee(target);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Employee target = curProject?.programmer[0];
            curProject.FireEmployee(target);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Employee target = curProject?.arts[0];
            curProject.FireEmployee(target);
        }
    }

    #region 프로젝트 관리
    public void StartNewProject(Project project)
    {
        if (!CanStartNewProject(project)) return;
        gold -= project.RequiredCost;

        projects.Add(project);
    }

    public bool CanStartNewProject(Project project)
    {
        if (projects.Count >= ProjectSlots)
        {
            Debug.Log("프로젝트 슬롯이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        if (gold < project.RequiredCost)
        {
            Debug.Log("보유 자금이 부족합니다"); // 추후 UI로 변경
            return false;
        }
        return true;
    }
    #endregion
}
