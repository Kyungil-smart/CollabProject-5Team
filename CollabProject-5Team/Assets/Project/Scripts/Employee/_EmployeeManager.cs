using R3;
using System;
using System.Collections.Generic;
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

    public EmployeeList employeeList;
    public HaveEmployees haveEmployees;
    public List<EmployeeTrainingProgress> activeTrainings = new();
    public const int TrainingDurationWeeks = 4;

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
        Employee employee = employeeList.leftEmployees[id].GetComponent<Employee>();
        return HireEmployee(employee);
    }
    public Employee HireEmployee(Employee employee) // Employee로 고용하는 경우 지원
    {
        employee.Init();
        haveEmployees.AddEmployee(employee);
        employeeList.DeleteEmployee(employee.so.id);
        return employee;
    }

    public void FireEmployee(Employee employee)
    {
        if (Company.Instance.curProject != null && Company.Instance.curProject.GetAllEmployees().Contains(employee))
            Company.Instance.curProject.RemoveEmployee(employee);

        RemoveTraining(employee);
        haveEmployees.RemoveEmployee(employee);
        employeeList.RestoreEmployee(employee.so.id);
        //Destroy(employee.gameObject);
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

                workStatus = emp.WorkStatus
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
    }

    public void ImportEmployeeData(SaveData data)
    {
        activeTrainings.Clear();
        haveEmployees.Clear();
        employeeList = new EmployeeList(allEmployeeObj);

        foreach (EmployeeSaveData empSave in data.savedEmployees)
        {
            Employee emp = HireEmployee(empSave.employeeId);
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

            RestoreEmployeeStatus(emp, empSave);
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
