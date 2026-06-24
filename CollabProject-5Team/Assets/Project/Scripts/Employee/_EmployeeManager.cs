using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class _EmployeeManager : MonoBehaviour
{
    public static _EmployeeManager Instance { get; private set; }

    [Header("전체 직원 프리팹")]
    public GameObject[] allEmployeeObj;

    [Header("기본 고용 직원")]
    public Employee[] defaultEmployees;

    [Header("교육 과정")]
    public List<EmployeeTrainingCourse> trainingCourses = new();

    [Header("이번 주 지원자 리스트")]
    public List<Employee> currentApplicants = new();    // 현재 모집 요청 및 지원자

    public List<RecruitRequest> activeRecruiRequests = new();

    public EmployeeList employeeList;
    public HaveEmployees haveEmployees;
    public List<EmployeeTrainingProgress> activeTrainings = new();
    public List<Employee> leavePendingEmployees = new();
    public const int TrainingDurationWeeks = 4;
    const float DailyLeaveChance = 0.25f;
    public static event Action<string> OnEmployeeLeft;

    #region DontDestroyOnLoad 없는 Instance
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;
    private void Awake()
    {
        Instance = this;
    #endregion

        haveEmployees = new HaveEmployees();
        employeeList = new EmployeeList(allEmployeeObj);
        InitTrainingCourses();

        Dialogue.DialogueEvents.OnStatChangeRequested
            .Subscribe(delta =>
            {
                Employee emp = haveEmployees.haveEmployeeList.Find(e => e.so.id == delta.employeeId);
                var data = emp.MutableData;
                data.desire += delta.desireDelta;
                data.fatigue += delta.fatigueDelta;
                data.loyalty += delta.loyaltyDelta;
                emp.MutableData = data;
            })
            .AddTo(this);

        foreach (var emp in defaultEmployees)
            HireEmployee(emp);
    }

    #region 고용/퇴사
    public Employee HireEmployee(int id)
    {
        if (!employeeList.leftEmployees.TryGetValue(id, out GameObject prefab))
        {
            Debug.LogWarning($"[EmployeeManager] {id}번 직원 프리팹을 찾을 수 없습니다.");
            return null;
        }

        Employee employee = Instantiate(prefab, transform).GetComponent<Employee>();
        employee.gameObject.SetActive(false);
        return HireEmployee(employee);
    }
    public Employee HireEmployee(Employee employee) // Employee로 고용하는 경우 지원
    {
        if (!employee.gameObject.scene.IsValid())
            return HireEmployee(employee.so.id);

        employee.Init();
        haveEmployees.AddEmployee(employee);
        employeeList.DeleteEmployee(employee.so.id);
        return employee;
    }

    public void FireEmployee(Employee employee)
    {
        if (Company.Instance.activeProjectCount.Value > 0 && Company.Instance.curProject.GetAllEmployees().Contains(employee))
            Company.Instance.curProject.RemoveEmployee(employee);

        RemoveTraining(employee);
        haveEmployees.RemoveEmployee(employee);
        employeeList.RestoreEmployee(employee.so.id);
        //Destroy(employee.gameObject);
    }

    // 직원 채용 요청 등록
    public void RegisterRecruitRequests(List<RecruitRequest> requests)
    {
        activeRecruiRequests.Clear();
        activeRecruiRequests.AddRange(requests);
    }

    // 현재 지원자 리스트를 반환
    public List<Employee> GetCurrentApplicantEmployees()
    {
        return currentApplicants;
            //.SelectMany(request => request.Applicants)
            //.ToList();
    }

    // 금요일 밤에 채용 요청 수만큼 지원자를 확정한다.
    public void GenerateWeeklyApplicants()
    {
        currentApplicants.Clear();

        foreach (RecruitRequest request in activeRecruiRequests)
        {
            request.Applicants.Clear();

            var applicants = employeeList.leftEmployees.Values
                .Select(go => go.GetComponent<Employee>())
                .Where(e => e.so.role == request.TargetRole)
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(request.Count)
                .ToList();

            currentApplicants.AddRange(applicants);
        }
    }

    public void ClearAllRecruitData()
    {
        currentApplicants.Clear();
        activeRecruiRequests.Clear();
    }

    public void ClearCurrentApplicants()
    {
        currentApplicants.Clear();
    }

    #endregion

    #region 상태 관리
    public void MarkProjectEmployee(Employee employee)
    {
        haveEmployees.SetStatus(employee, EmployeeWorkStatus.InProject);
    }

    public void ReleaseProjectEmployees(IEnumerable<Employee> employees)
    {
        foreach (var employee in employees)
            haveEmployees.SetStatus(employee, EmployeeWorkStatus.Standby);
    }

    public void RegisterLeavePendingEmployees()
    {
        foreach (Employee employee in haveEmployees.haveEmployeeList)
        {
            if (leavePendingEmployees.Contains(employee))
                continue;

            EmployeeMutableData data = employee.MutableData;
            if (data.fatigue >= 100 || data.desire <= 0 || data.loyalty <= 0)
                leavePendingEmployees.Add(employee);
        }
    }

    public void TryProcessDailyLeave()
    {
        for (int i = 0; i < leavePendingEmployees.Count; i++)
        {
            Employee employee = leavePendingEmployees[i];
            if (UnityEngine.Random.value >= DailyLeaveChance)
                continue;

            string employeeName = employee.so.Name;
            leavePendingEmployees.RemoveAt(i);
            FireEmployee(employee);
            GameManager.Instance.RemoveNpcFromScene(employee);

            OnEmployeeLeft?.Invoke(employeeName);
            return;
        }
    }
    #endregion

    #region 교육시스템
    public void StartTraining(Employee employee, int courseIndex)
    {
        StartTraining(employee, trainingCourses[courseIndex]);
    }

    public void StartTraining(Employee employee, EmployeeTrainingCourse course)
    {
        if (employee.WorkStatus != EmployeeWorkStatus.Standby)
            throw new InvalidOperationException($"{employee.so.Name} 직원은 대기 중일 때만 교육을 시작할 수 있습니다.");

        if (Company.Instance.gold.Value < course.cost)
            throw new InvalidOperationException($"{course.courseName} 교육 비용이 부족합니다. 필요 비용: {course.cost}G");

        Company.Instance.gold.Value -= course.cost;
        Company.Instance.curManagementStatus.otherExpense += course.cost;
        Company.Instance.cumulativeManagementStatus.otherExpense += course.cost;
        Company.Instance.curManagementStatus.Recalculate();
        Company.Instance.cumulativeManagementStatus.Recalculate();

        haveEmployees.SetStatus(employee, EmployeeWorkStatus.InTraining);
        activeTrainings.Add(new EmployeeTrainingProgress(employee, course, DateTimeManager.Instance.currentWeek.Value));

        Debug.Log($"[교육] {employee.so.Name} 직원이 {course.courseName}을 시작했습니다. 비용 {course.cost}G, 기간 {TrainingDurationWeeks}주");
    }

    void InitTrainingCourses()
    {
        trainingCourses.Clear();
        trainingCourses.Add(new EmployeeTrainingCourse("기본 교육", 1000, 1, 3, 0.1f));
        trainingCourses.Add(new EmployeeTrainingCourse("전문 교육", 3000, 3, 6, 0.2f));
        trainingCourses.Add(new EmployeeTrainingCourse("집중 교육", 5000, 5, 10, 0.3f));
    }

    public void TickWeeklyTraining()
    {
        for (int i = activeTrainings.Count - 1; i >= 0; i--)
        {
            EmployeeTrainingProgress training = activeTrainings[i];
            training.remainingWeeks--;

            if (training.remainingWeeks > 0)
            {
                Debug.Log($"[교육] {training.employee.so.Name} 직원의 {training.course.courseName} 남은 기간: {training.remainingWeeks}주");
                continue;
            }

            activeTrainings.RemoveAt(i);
            CompleteTraining(training);
        }
    }

    public EmployeeTrainingProgress GetTraining(Employee employee)
    {
        return activeTrainings.Find(t => t.employee == employee);
    }

    void CompleteTraining(EmployeeTrainingProgress training)
    {
        Employee employee = training.employee;
        EmployeeTrainingCourse course = training.course;

        // 실패 시 교육 시작 전 능력치로 복귀한다.
        employee.MutableData = training.startData;

        if (UnityEngine.Random.value < course.failureRate)
        {
            Debug.Log($"[EM] {employee.so.Name} 직원의 {course.courseName} 실패. 능력치는 변동되지 않았습니다.");
        }
        else
        {
            int delta = UnityEngine.Random.Range(course.minAbilityDelta, course.maxAbilityDelta + 1);
            employee.MutableData.ability += delta; // 성장패널티 없음
#if UNITY_EDITOR
            Debug.Log($"[EM] {employee.so.Name} 직원의 {course.courseName} 성공. 증가량:{delta} 능력치:{employee.MutableData.ability}");
#endif
        }
        haveEmployees.SetStatus(employee, EmployeeWorkStatus.Standby);
    }

    void RemoveTraining(Employee employee)
    {
        activeTrainings.Remove(GetTraining(employee));
    }
    #endregion

    #region 세이브/로드
    public void ExportEmployeeData(SaveData data)
    {
        data.savedEmployees.Clear();
        if (data.leavePendingEmployeeIds == null)
            data.leavePendingEmployeeIds = new List<int>();
        else
            data.leavePendingEmployeeIds.Clear();

        foreach (Employee emp in haveEmployees.haveEmployeeList)
        {
            EmployeeTrainingProgress training = GetTraining(emp);
            var empSave = new EmployeeSaveData
            {
                employeeId = emp.so.id,

                ability = emp.MutableData.ability,
                property1 = emp.MutableData.property1,
                property2 = emp.MutableData.property2,
                property3 = emp.MutableData.property3,

                desire = emp.MutableData.desire,
                loyalty = emp.MutableData.loyalty,
                fatigue = emp.MutableData.fatigue,

                preDesire = emp.MutableData.preDesire,
                preLoyalty = emp.MutableData.preLoyalty,
                preFatigue = emp.MutableData.preFatigue,

                workStatus = emp.WorkStatus,
                hasTalkedThisWeek = emp.hasTalkedThisWeek,
                completedProjectNames = emp.completedProjectNames != null
                    ? new List<string>(emp.completedProjectNames)
                    : new List<string>()
            };

            if (emp.WorkStatus == EmployeeWorkStatus.InTraining)
            {
                empSave.trainingRemainingWeeks = training.remainingWeeks;
                empSave.trainingStartedWeek = training.startedWeek;
                empSave.trainingCourseName = training.course.courseName;
                empSave.trainingCost = training.course.cost;
                empSave.trainingMinAbilityDelta = training.course.minAbilityDelta;
                empSave.trainingMaxAbilityDelta = training.course.maxAbilityDelta;
                empSave.trainingFailureRate = training.course.failureRate;
                empSave.trainingStartData = training.startData;
            }

            data.savedEmployees.Add(empSave);
        }

        foreach (Employee employee in leavePendingEmployees)
        {
            if (employee != null && haveEmployees.haveEmployeeList.Contains(employee))
                data.leavePendingEmployeeIds.Add(employee.so.id);
        }
    }

    public void ImportEmployeeData(SaveData data)
    {
        activeTrainings.Clear();
        leavePendingEmployees.Clear();
        foreach (Employee emp in haveEmployees.haveEmployeeList)
        {
            if (emp != null)
                Destroy(emp.gameObject);
        }

        haveEmployees.Clear();
        employeeList = new EmployeeList(allEmployeeObj);

        foreach (EmployeeSaveData empSave in data.savedEmployees)
        {
            Employee emp = HireEmployee(empSave.employeeId);
            if (emp == null) continue;

            emp.MutableData = new EmployeeMutableData
            {
                ability = empSave.ability,
                property1 = empSave.property1,
                property2 = empSave.property2,
                property3 = empSave.property3,

                desire = empSave.desire,
                loyalty = empSave.loyalty,
                fatigue = empSave.fatigue,

                preDesire = empSave.preDesire,
                preLoyalty = empSave.preLoyalty,
                preFatigue = empSave.preFatigue
            };

            emp.hasTalkedThisWeek = empSave.hasTalkedThisWeek;
            emp.completedProjectNames = empSave.completedProjectNames != null
                ? new List<string>(empSave.completedProjectNames)
                : new List<string>();
            RestoreEmployeeStatus(emp, empSave);
        }

        if (data.leavePendingEmployeeIds != null)
        {
            foreach (int employeeId in data.leavePendingEmployeeIds)
            {
                Employee emp = haveEmployees.haveEmployeeList.Find(e => e.so.id == employeeId);
                if (emp != null) leavePendingEmployees.Add(emp);
            }
        }
    }

    void RestoreEmployeeStatus(Employee employee, EmployeeSaveData saveData)
    {
        if (saveData.workStatus == EmployeeWorkStatus.InTraining)
        {
            var course = new EmployeeTrainingCourse(
                saveData.trainingCourseName,
                saveData.trainingCost,
                saveData.trainingMinAbilityDelta,
                saveData.trainingMaxAbilityDelta,
                saveData.trainingFailureRate);

            activeTrainings.Add(new EmployeeTrainingProgress(
                employee,
                course,
                saveData.trainingStartData,
                saveData.trainingRemainingWeeks,
                saveData.trainingStartedWeek));
        }

        haveEmployees.SetStatus(employee, saveData.workStatus);
    }
    #endregion
}
