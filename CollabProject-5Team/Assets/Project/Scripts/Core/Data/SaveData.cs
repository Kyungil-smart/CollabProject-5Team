using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // DateTimeManager 저장

    public int currentWeek;
    public DayOfWeek currentDay;
    public TimeOfDay currentTime;
    public int day;
    public bool isWorkCompleted;
    public List<string> talkedNpcsToday;
    public List<int> talkedEmployeeIdsThisWeek;
}