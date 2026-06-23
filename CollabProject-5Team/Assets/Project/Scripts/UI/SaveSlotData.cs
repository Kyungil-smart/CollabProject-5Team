using System;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// 세이브 슬롯 표시 데이터. SaveView/LoadView 공용.
    /// </summary>
    public sealed class SaveSlotData
    {
        public int slotIndex;
        public bool isAutoSlot;
        public string title;
        public string companyName;
        public string dateTime;
        public string savedAt;
        public string gold;
        public int employeeCount;
        public string playTime;
        public string projectName;
        public string projectState;
        public TimeOfDay timeOfDay;

        public static SaveSlotData FromSaveData(int slotIndex, SaveData data, bool isAutoSlot)
        {
            if (data == null) return null;

            string projectName = GetProjectName(data);
            string title = isAutoSlot ? "자동 저장 데이터" : projectName;

            return new SaveSlotData
            {
                slotIndex = slotIndex,
                isAutoSlot = isAutoSlot,
                title = title,
                companyName = title,
                dateTime = $"{DateTimeManager.GetDateString(data.day)} ({GetTimeOfDayLabel(data.currentTime)})",
                savedAt = string.IsNullOrWhiteSpace(data.realSaveTime) ? "저장 시간 없음" : data.realSaveTime,
                gold = $"{data.company_Gold:N0}G",
                employeeCount = data.savedEmployees?.Count ?? 0,
                playTime = FormatPlayTime(data.playTime),
                projectName = projectName,
                projectState = GetProjectState(data),
                timeOfDay = data.currentTime,
            };
        }

        private static string GetProjectName(SaveData data)
        {
            CurrentProjectSaveData projectData = data.activeProjectsData;
            if (projectData != null &&
                projectData.hasActiveProject &&
                !string.IsNullOrWhiteSpace(projectData.project_userNamed))
            {
                return projectData.project_userNamed;
            }

            return "진행 중인 프로젝트 없음";
        }

        private static string GetProjectState(SaveData data)
        {
            CurrentProjectSaveData projectData = data.activeProjectsData;
            if (projectData == null || !projectData.hasActiveProject)
                return "프로젝트 없음";

            return $"{GetProjectSizeLabel(projectData.project_Scale)} 개발 중";
        }

        private static string GetProjectSizeLabel(ProjectSize size) => size switch
        {
            ProjectSize.Small => "소형",
            ProjectSize.Medium => "중형",
            ProjectSize.Large => "대형",
            _ => size.ToString(),
        };

        private static string GetTimeOfDayLabel(TimeOfDay timeOfDay) => timeOfDay switch
        {
            TimeOfDay.Day => "낮",
            TimeOfDay.Night => "밤",
            _ => timeOfDay.ToString(),
        };

        private static string FormatPlayTime(float playTime)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0f, playTime));
            return timeSpan.TotalHours >= 1
                ? timeSpan.ToString(@"hh\:mm\:ss")
                : timeSpan.ToString(@"mm\:ss");
        }
    }
}
