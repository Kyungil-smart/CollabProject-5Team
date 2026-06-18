using System;
using UnityEngine;

[Serializable]
public class EmployeeTrainingCourse
{
    public string courseName;
    public int cost;
    public int minAbilityDelta;
    public int maxAbilityDelta;
    [Range(0f, 1f)] public float failureRate;

    public EmployeeTrainingCourse(string courseName, int cost, int minAbilityDelta, int maxAbilityDelta, float failureRate)
    {
        this.courseName = courseName;
        this.cost = cost;
        this.minAbilityDelta = minAbilityDelta;
        this.maxAbilityDelta = maxAbilityDelta;
        this.failureRate = failureRate;
    }
}

public class EmployeeTrainingProgress
{
    public Employee employee;
    public EmployeeTrainingCourse course;
    public EmployeeMutableData startData;
    public int remainingWeeks;
    public int startedWeek;

    public EmployeeTrainingProgress(Employee employee, EmployeeTrainingCourse course, int startedWeek)
    {
        this.employee = employee;
        this.course = new EmployeeTrainingCourse(course.courseName, course.cost, course.minAbilityDelta, course.maxAbilityDelta, course.failureRate);
        this.startData = employee.MutableData;
        this.remainingWeeks = _EmployeeManager.TrainingDurationWeeks;
        this.startedWeek = startedWeek;
    }

    public EmployeeTrainingProgress(Employee employee, EmployeeTrainingCourse course, EmployeeMutableData startData, int remainingWeeks, int startedWeek)
    {
        this.employee = employee;
        this.course = course;
        this.startData = startData;
        this.remainingWeeks = remainingWeeks;
        this.startedWeek = startedWeek;
    }
}
