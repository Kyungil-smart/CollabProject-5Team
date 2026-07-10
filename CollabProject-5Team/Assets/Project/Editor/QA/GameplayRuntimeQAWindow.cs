using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class GameplayRuntimeQAWindow : EditorWindow
    {
        private enum RuntimeArea
        {
            Gameplay,
            Dialogue,
            Report
        }

        private enum RuntimeCategory
        {
            All,
            QuestAndDate,
            ProjectAndEconomy,
            HumanResources,
            SaveAndLoad
        }

        private enum CheckLevel
        {
            Pass,
            Warning,
            Fail,
            Info
        }

        private const int MaxRecords = 300;
        private readonly List<CheckRecord> _records = new();

        private RuntimeSnapshot _previous;
        private RuntimeSnapshot _saveBaseline;
        private RuntimeCategory _category = RuntimeCategory.All;
        private Vector2 _scrollPosition;
        private bool _autoObserve = true;
        private bool _showPass = true;
        private bool _showWarning = true;
        private bool _showFail = true;
        private bool _showInfo;
        private double _nextSampleTime;
        private RuntimeArea _runtimeArea;

        [NonSerialized] private DialogueRewardRuntimeQAWindow _dialoguePanel;
        [NonSerialized] private ReportRuntimeQAWindow _reportPanel;

        [MenuItem("Tools/QA/5. Runtime QA", false, 105)]
        public static void Open()
        {
            OpenTab(RuntimeArea.Gameplay);
        }

        internal static void OpenDialogueTab()
        {
            OpenTab(RuntimeArea.Dialogue);
        }

        internal static void OpenReportTab()
        {
            OpenTab(RuntimeArea.Report);
        }

        private static void OpenTab(RuntimeArea area)
        {
            GameplayRuntimeQAWindow window = GetWindow<GameplayRuntimeQAWindow>("Runtime QA");
            window.minSize = new Vector2(760f, 520f);
            window._runtimeArea = area;
            window.EnsureEmbeddedPanels();
            window.Show();
            window.Repaint();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EnsureEmbeddedPanels();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DestroyEmbeddedPanels();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void EnsureEmbeddedPanels()
        {
            if (_dialoguePanel == null)
            {
                _dialoguePanel = CreateInstance<DialogueRewardRuntimeQAWindow>();
                _dialoguePanel.hideFlags = HideFlags.DontSave;
            }

            if (_reportPanel == null)
            {
                _reportPanel = CreateInstance<ReportRuntimeQAWindow>();
                _reportPanel.hideFlags = HideFlags.DontSave;
            }
        }

        private void DestroyEmbeddedPanels()
        {
            if (_dialoguePanel != null)
                DestroyImmediate(_dialoguePanel);
            if (_reportPanel != null)
                DestroyImmediate(_reportPanel);

            _dialoguePanel = null;
            _reportPanel = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.EnteredPlayMode or PlayModeStateChange.ExitingPlayMode)
            {
                _previous = null;
                _saveBaseline = null;
            }

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_autoObserve || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _nextSampleTime)
                return;

            _nextSampleTime = EditorApplication.timeSinceStartup + 0.1d;
            RuntimeSnapshot current = CaptureRuntimeSnapshot();
            if (!current.IsReady)
            {
                _previous = null;
                Repaint();
                return;
            }

            if (_previous != null)
                ValidateTransitions(_previous, current);

            _previous = current;
            Repaint();
        }

        private static RuntimeSnapshot CaptureRuntimeSnapshot()
        {
            var snapshot = new RuntimeSnapshot();
            DateTimeManager date = DateTimeManager.Instance;
            Company company = Company.Instance;
            _EmployeeManager employeeManager = _EmployeeManager.Instance;
            if (date == null || company == null || employeeManager == null || employeeManager.haveEmployees == null)
                return snapshot;

            snapshot.IsReady = true;
            snapshot.Week = date.currentWeek.Value;
            snapshot.DayOfWeek = date.currentDay;
            snapshot.TimeOfDay = date.currentTime;
            snapshot.BusinessDay = date.day.Value;
            snapshot.WorkCompleted = date.isWorkCompleted;

            snapshot.Gold = company.gold.Value;
            snapshot.Level = company.level;
            snapshot.Popularity = company.popularity;
            snapshot.Reputation = company.reputation;
            snapshot.CompletedProjectCount = company.completedProjects.Count;
            snapshot.CompletedProjects = company.completedProjects
                .Select(project => new CompletedProjectState
                {
                    Name = project.projectName,
                    DailyGold = project.dailyGold,
                    DailyCost = project.dailyCost,
                    Retention = project.RetentionFactor,
                    ServiceOver = project.isServiceOver
                })
                .ToList();

            OfficeUpgradeData officeData = company._upgradeData != null ? company._upgradeData.GetData(company.level) : null;
            snapshot.OfficeMaintainCost = officeData != null ? officeData.maintainCost : 0;

            snapshot.ActiveProject = company.activeProjectCount.Value > 0 && company.curProject != null;
            if (snapshot.ActiveProject)
            {
                Project project = company.curProject;
                snapshot.Project = project;
                snapshot.ProjectName = project.userNamed.Value;
                snapshot.ProjectRequiredCost = project.RequiredCost;
                snapshot.ProjectDay = project.day;
                snapshot.ProjectDuration = project.DurationDays;
                snapshot.ProjectNightCount = project.nightCount;
                snapshot.ProjectQuality = project.qualityScore;
                snapshot.ProjectStability = project.stabilityScore;
                snapshot.ProjectCharm = project.charmScore;
                snapshot.ProjectCurScore = project.CurScore;
                snapshot.ProjectEmployeeIds = project.GetAllEmployees()
                    .Where(employee => employee != null && employee.so != null)
                    .Select(employee => employee.so.id)
                    .OrderBy(id => id)
                    .ToArray();
            }

            foreach (Employee employee in employeeManager.haveEmployees.haveEmployeeList)
            {
                if (employee == null || employee.so == null)
                    continue;

                snapshot.Employees[employee.so.id] = new EmployeeState
                {
                    Id = employee.so.id,
                    Name = employee.so.Name,
                    Role = employee.so.role,
                    WorkStatus = employee.WorkStatus,
                    Ability = employee.MutableData.ability,
                    Desire = employee.MutableData.desire,
                    Fatigue = employee.MutableData.fatigue,
                    Loyalty = employee.MutableData.loyalty,
                    HiringCost = employee.so.hiringCost,
                    WeekSalary = employee.so.weekSalary,
                    Employee = employee
                };
            }

            snapshot.LeftEmployeeIds = employeeManager.employeeList?.leftEmployees.Keys.OrderBy(id => id).ToArray() ?? Array.Empty<int>();
            snapshot.ApplicantIds = employeeManager.currentApplicants
                .Where(employee => employee != null && employee.so != null)
                .Select(employee => employee.so.id)
                .OrderBy(id => id)
                .ToArray();
            snapshot.RecruitRequests = employeeManager.activeRecruiRequests
                .Select(request => new RecruitRequestState(request.TargetRole, request.Count))
                .ToArray();
            snapshot.TrainingEmployeeIds = employeeManager.haveEmployees.trainingEmployees
                .Where(employee => employee != null && employee.so != null)
                .Select(employee => employee.so.id)
                .OrderBy(id => id)
                .ToArray();

            QuestManager quest = QuestManager.Instance;
            if (quest != null)
            {
                snapshot.HasQuestManager = true;
                snapshot.QuestState = quest.dailyQuestState.Value;
                snapshot.QuestProgress = quest.dailyQuestProgress.Value;
                if (quest.curDailyQuest?.so != null)
                {
                    snapshot.QuestId = quest.curDailyQuest.so.id;
                    snapshot.QuestRole = quest.curDailyQuest.so.role;
                    snapshot.QuestSuccessEffect = quest.curDailyQuest.so.successEffect;
                    snapshot.QuestTarget = quest.EffectiveTargetCount;
                }

                foreach (KeyValuePair<Role, int> pair in quest._weeklyBonusPoints)
                    snapshot.WeeklyBonusRaw[pair.Key] = pair.Value;
            }

            return snapshot;
        }

        private void ValidateTransitions(RuntimeSnapshot before, RuntimeSnapshot after)
        {
            ValidateDateTransition(before, after);
            ValidateQuestTransition(before, after);
            ValidateProjectTransition(before, after);
            ValidateHumanResourcesTransition(before, after);
            ValidateEconomyTransition(before, after);
        }

        private void ValidateDateTransition(RuntimeSnapshot before, RuntimeSnapshot after)
        {
            bool changed = before.Week != after.Week || before.DayOfWeek != after.DayOfWeek ||
                           before.TimeOfDay != after.TimeOfDay || before.BusinessDay != after.BusinessDay;
            if (!changed)
                return;

            bool valid;
            string expected;
            if (before.DayOfWeek == DayOfWeek.Friday && before.TimeOfDay == TimeOfDay.Day)
            {
                valid = after.DayOfWeek == DayOfWeek.Friday && after.TimeOfDay == TimeOfDay.Night &&
                        after.Week == before.Week && after.BusinessDay == before.BusinessDay + 1;
                expected = "금요일 낮 → 같은 주 금요일 밤, 영업일 +1";
            }
            else if (before.DayOfWeek == DayOfWeek.Friday && before.TimeOfDay == TimeOfDay.Night)
            {
                valid = after.DayOfWeek == DayOfWeek.Monday && after.TimeOfDay == TimeOfDay.Day &&
                        after.Week == before.Week + 1 && after.BusinessDay == before.BusinessDay &&
                        !after.WorkCompleted;
                expected = "금요일 밤 → 다음 주 월요일 낮, 업무 상태 초기화";
            }
            else
            {
                valid = after.DayOfWeek == (DayOfWeek)((int)before.DayOfWeek + 1) && after.TimeOfDay == TimeOfDay.Day &&
                        after.Week == before.Week && after.BusinessDay == before.BusinessDay + 1 &&
                        !after.WorkCompleted;
                expected = "월~목 낮 → 다음 요일 낮, 영업일 +1, 업무 상태 초기화";
            }

            AddRecord(RuntimeCategory.QuestAndDate, valid ? CheckLevel.Pass : CheckLevel.Fail,
                "날짜/낮밤 전환",
                $"이전 {FormatDate(before)} → 실제 {FormatDate(after)} | 기대: {expected}");
        }

        private void ValidateQuestTransition(RuntimeSnapshot before, RuntimeSnapshot after)
        {
            if (!before.HasQuestManager || !after.HasQuestManager || before.QuestState == after.QuestState)
                return;

            if (after.QuestState == QuestState.Playing)
            {
                bool valid = after.QuestId > 0 && after.QuestTarget > 0 && after.QuestProgress >= 0;
                AddRecord(RuntimeCategory.QuestAndDate, valid ? CheckLevel.Pass : CheckLevel.Fail,
                    "일일 퀘스트 시작",
                    $"Quest {after.QuestId} / {after.QuestRole} / 진행 {after.QuestProgress}/{after.QuestTarget}");
            }
            else if (after.QuestState == QuestState.End)
            {
                int beforeBonus = GetRawBonus(before, after.QuestRole);
                int afterBonus = GetRawBonus(after, after.QuestRole);
                int actualBonus = afterBonus - beforeBonus;
                bool progressValid = after.QuestProgress >= after.QuestTarget;
                bool rewardValid = actualBonus == after.QuestSuccessEffect;
                bool valid = progressValid && rewardValid && after.WorkCompleted;
                AddRecord(RuntimeCategory.QuestAndDate, valid ? CheckLevel.Pass : CheckLevel.Fail,
                    "일일 퀘스트 완료",
                    $"진행 {after.QuestProgress}/{after.QuestTarget}, 업무완료 {after.WorkCompleted}, " +
                    $"주간 보정 원본 기대 +{after.QuestSuccessEffect} / 실제 {FormatDelta(actualBonus)}");
            }
            else if (after.QuestState == QuestState.Ready)
            {
                AddRecord(RuntimeCategory.QuestAndDate, CheckLevel.Info,
                    "일일 퀘스트 초기화", $"{FormatDate(after)} / 업무완료={after.WorkCompleted}");
            }
        }

        private void ValidateProjectTransition(RuntimeSnapshot before, RuntimeSnapshot after)
        {
            if (!before.ActiveProject && after.ActiveProject)
            {
                int actualCost = before.Gold - after.Gold;
                bool costValid = actualCost == after.ProjectRequiredCost;
                bool employeesValid = after.ProjectEmployeeIds.Length > 0 && after.ProjectEmployeeIds.All(id =>
                    after.Employees.TryGetValue(id, out EmployeeState employee) && employee.WorkStatus == EmployeeWorkStatus.InProject);
                AddRecord(RuntimeCategory.ProjectAndEconomy,
                    costValid && employeesValid ? CheckLevel.Pass : CheckLevel.Fail,
                    "프로젝트 시작",
                    $"{after.ProjectName} / 비용 기대 {after.ProjectRequiredCost}G, 실제 {actualCost}G / " +
                    $"배치 직원 {after.ProjectEmployeeIds.Length}명 상태={(employeesValid ? "정상" : "불일치")}");
            }

            if (before.ActiveProject && after.ActiveProject && before.Project == after.Project && before.ProjectDay != after.ProjectDay)
            {
                bool valid = after.ProjectDay == before.ProjectDay + 1 && after.ProjectDay <= after.ProjectDuration;
                AddRecord(RuntimeCategory.ProjectAndEconomy, valid ? CheckLevel.Pass : CheckLevel.Fail,
                    "프로젝트 일일 진행",
                    $"{after.ProjectName}: {before.ProjectDay}일 → {after.ProjectDay}일 / 기간 {after.ProjectDuration}일");
            }

            if (before.ActiveProject && !after.ActiveProject)
            {
                bool recordAdded = after.CompletedProjectCount == before.CompletedProjectCount + 1;
                bool employeesReleased = before.ProjectEmployeeIds.All(id =>
                    !after.Employees.TryGetValue(id, out EmployeeState employee) || employee.WorkStatus == EmployeeWorkStatus.Standby);
                AddRecord(RuntimeCategory.ProjectAndEconomy,
                    recordAdded && employeesReleased ? CheckLevel.Pass : CheckLevel.Fail,
                    "프로젝트 완료/출시",
                    $"완료 기록 +{after.CompletedProjectCount - before.CompletedProjectCount}, 직원 대기 복귀={(employeesReleased ? "정상" : "불일치")}");
            }
        }

        private void ValidateHumanResourcesTransition(RuntimeSnapshot before, RuntimeSnapshot after)
        {
            int[] hiredIds = after.Employees.Keys.Except(before.Employees.Keys).ToArray();
            if (hiredIds.Length > 0)
            {
                bool listValid = hiredIds.All(id => !after.LeftEmployeeIds.Contains(id) &&
                    after.Employees[id].WorkStatus == EmployeeWorkStatus.Standby);
                int expectedCost = hiredIds.Sum(id => after.Employees[id].HiringCost);
                int actualCost = before.Gold - after.Gold;
                bool costValid = hiredIds.Length > 1 || actualCost == expectedCost;
                AddRecord(RuntimeCategory.HumanResources,
                    listValid && costValid ? CheckLevel.Pass : CheckLevel.Fail,
                    "직원 채용",
                    $"직원 {FormatIds(hiredIds)} / 계약금 기대 {expectedCost}G, 실제 {actualCost}G / 목록={(listValid ? "정상" : "불일치")}");
            }

            int[] removedIds = before.Employees.Keys.Except(after.Employees.Keys).ToArray();
            if (removedIds.Length > 0)
            {
                bool restored = removedIds.All(id => after.LeftEmployeeIds.Contains(id));
                int severance = removedIds.Sum(id => before.Employees[id].HiringCost);
                int actualCost = before.Gold - after.Gold;
                bool costValid = actualCost == severance || actualCost == 0;
                AddRecord(RuntimeCategory.HumanResources,
                    restored && costValid ? CheckLevel.Pass : CheckLevel.Fail,
                    "직원 해고/자발적 퇴사",
                    $"직원 {FormatIds(removedIds)} / 미고용 목록 복원={(restored ? "정상" : "불일치")} / 자금 변화 {-actualCost}G");
            }

            int beforeRequestCount = before.RecruitRequests.Sum(request => request.Count);
            int afterRequestCount = after.RecruitRequests.Sum(request => request.Count);
            if (beforeRequestCount == 0 && afterRequestCount > 0)
            {
                int expectedCost = afterRequestCount * 10000;
                int actualCost = before.Gold - after.Gold;
                AddRecord(RuntimeCategory.HumanResources,
                    actualCost == expectedCost ? CheckLevel.Pass : CheckLevel.Fail,
                    "직원 모집 요청",
                    $"모집 {afterRequestCount}명 / 비용 기대 {expectedCost}G, 실제 {actualCost}G");
            }

            if (!before.ApplicantIds.SequenceEqual(after.ApplicantIds) && after.ApplicantIds.Length > 0)
            {
                bool unique = after.ApplicantIds.Distinct().Count() == after.ApplicantIds.Length;
                bool notHired = after.ApplicantIds.All(id => !after.Employees.ContainsKey(id));
                AddRecord(RuntimeCategory.HumanResources,
                    unique && notHired ? CheckLevel.Pass : CheckLevel.Fail,
                    "주간 지원자 생성",
                    $"지원자 {FormatIds(after.ApplicantIds)} / 중복={(unique ? "없음" : "있음")} / 기존 직원 중복={(!notHired ? "있음" : "없음")}");
            }

            if (!before.TrainingEmployeeIds.SequenceEqual(after.TrainingEmployeeIds))
                RunEmployeeListInvariant(after, "교육 상태 변경");
        }

        private void ValidateEconomyTransition(RuntimeSnapshot before, RuntimeSnapshot after)
        {
            if (before.Gold == after.Gold)
                return;

            int delta = after.Gold - before.Gold;
            AddRecord(RuntimeCategory.ProjectAndEconomy, CheckLevel.Info,
                "자금 변동 감지",
                $"{before.Gold:N0}G → {after.Gold:N0}G ({FormatDelta(delta)}G), 날짜 {FormatDate(after)}");

            if (after.BusinessDay == before.BusinessDay + 1)
            {
                int dailyRevenue = 0;
                foreach (CompletedProjectState beforeProject in before.CompletedProjects.Where(project => !project.ServiceOver))
                {
                    CompletedProjectState current = after.CompletedProjects.FirstOrDefault(project => project.Name == beforeProject.Name);
                    if (current != null && !current.ServiceOver)
                        dailyRevenue += current.DailyGold;
                }

                int expectedDelta = dailyRevenue;
                string detail = $"일일 서비스 매출 +{dailyRevenue:N0}G";
                if (before.DayOfWeek == DayOfWeek.Friday && before.TimeOfDay == TimeOfDay.Day)
                {
                    int salaries = before.Employees.Values.Sum(employee => employee.WeekSalary);
                    int serviceCost = before.CompletedProjects.Where(project => !project.ServiceOver).Sum(project => project.DailyCost);
                    expectedDelta -= salaries + before.OfficeMaintainCost + serviceCost;
                    detail += $", 주급 -{salaries:N0}G, 사무실 -{before.OfficeMaintainCost:N0}G, 서비스 유지비 -{serviceCost:N0}G";
                }

                CheckLevel level = delta == expectedDelta ? CheckLevel.Pass : CheckLevel.Warning;
                AddRecord(RuntimeCategory.ProjectAndEconomy, level,
                    "일일/주간 정산 자금",
                    $"기대 {FormatDelta(expectedDelta)}G / 실제 {FormatDelta(delta)}G | {detail}" +
                    (level == CheckLevel.Warning ? " | 같은 프레임의 다른 거래가 포함됐는지 확인" : string.Empty));
            }
        }

        private void RunCurrentInvariants()
        {
            RuntimeSnapshot snapshot = CaptureRuntimeSnapshot();
            if (!snapshot.IsReady)
            {
                AddRecord(RuntimeCategory.All, CheckLevel.Fail, "현재 상태 검사", "필수 런타임 매니저를 찾지 못했습니다.");
                return;
            }

            RunEmployeeListInvariant(snapshot, "현재 직원 목록");

            bool projectCountValid = snapshot.ActiveProject == (Company.Instance.activeProjectCount.Value == 1);
            AddRecord(RuntimeCategory.ProjectAndEconomy,
                projectCountValid ? CheckLevel.Pass : CheckLevel.Fail,
                "프로젝트 슬롯 일관성",
                $"activeProjectCount={Company.Instance.activeProjectCount.Value}, curProject={(Company.Instance.curProject != null ? "있음" : "없음")}");

            bool statsValid = snapshot.Employees.Values.All(employee =>
                employee.Ability is >= 0 and <= 100 && employee.Desire is >= 0 and <= 100 &&
                employee.Fatigue is >= 0 and <= 100 && employee.Loyalty is >= 0 and <= 100);
            AddRecord(RuntimeCategory.HumanResources, statsValid ? CheckLevel.Pass : CheckLevel.Fail,
                "직원 상태 범위", statsValid ? "모든 직원 상태가 0~100 범위입니다." : "0~100 범위를 벗어난 직원 상태가 있습니다.");
        }

        private void RunEmployeeListInvariant(RuntimeSnapshot snapshot, string title)
        {
            _EmployeeManager manager = _EmployeeManager.Instance;
            bool valid = manager != null && snapshot.Employees.Values.All(employee =>
            {
                int listCount = 0;
                if (manager.haveEmployees.standbyEmployees.Contains(employee.Employee)) listCount++;
                if (manager.haveEmployees.projectEmployees.Contains(employee.Employee)) listCount++;
                if (manager.haveEmployees.trainingEmployees.Contains(employee.Employee)) listCount++;

                bool statusMatch = employee.WorkStatus switch
                {
                    EmployeeWorkStatus.InProject => manager.haveEmployees.projectEmployees.Contains(employee.Employee),
                    EmployeeWorkStatus.InTraining => manager.haveEmployees.trainingEmployees.Contains(employee.Employee),
                    _ => manager.haveEmployees.standbyEmployees.Contains(employee.Employee)
                };
                return listCount == 1 && statusMatch;
            });

            AddRecord(RuntimeCategory.HumanResources, valid ? CheckLevel.Pass : CheckLevel.Fail,
                title, valid ? "모든 직원이 정확히 하나의 상태 목록에 있습니다." : "직원 상태 목록 중복 또는 WorkStatus 불일치가 있습니다.");
        }

        private void CaptureSaveBaseline()
        {
            _saveBaseline = CaptureRuntimeSnapshot();
            AddRecord(RuntimeCategory.SaveAndLoad,
                _saveBaseline.IsReady ? CheckLevel.Info : CheckLevel.Fail,
                "저장 전 기준 캡처",
                _saveBaseline.IsReady ? $"{FormatDate(_saveBaseline)} / 직원 {_saveBaseline.Employees.Count}명 / 자금 {_saveBaseline.Gold:N0}G" : "런타임 상태를 읽을 수 없습니다.");
        }

        private void CompareSaveBaseline()
        {
            if (_saveBaseline == null || !_saveBaseline.IsReady)
            {
                AddRecord(RuntimeCategory.SaveAndLoad, CheckLevel.Fail, "저장 복원 비교", "먼저 저장 전 기준을 캡처하세요.");
                return;
            }

            RuntimeSnapshot current = CaptureRuntimeSnapshot();
            var differences = new List<string>();
            CompareCoreSaveFields(_saveBaseline, current, differences);
            AddRecord(RuntimeCategory.SaveAndLoad,
                differences.Count == 0 ? CheckLevel.Pass : CheckLevel.Fail,
                "저장/불러오기 복원",
                differences.Count == 0 ? "날짜, 회사, 프로젝트, 직원, 주간 퀘스트 보정이 기준과 동일합니다." : string.Join(" | ", differences));
        }

        private static void CompareCoreSaveFields(RuntimeSnapshot before, RuntimeSnapshot after, List<string> differences)
        {
            if (!after.IsReady) { differences.Add("런타임 매니저 없음"); return; }
            if (before.Week != after.Week || before.DayOfWeek != after.DayOfWeek || before.TimeOfDay != after.TimeOfDay || before.BusinessDay != after.BusinessDay)
                differences.Add("날짜 불일치");
            if (before.WorkCompleted != after.WorkCompleted) differences.Add("업무 완료 상태 불일치");
            if (before.Gold != after.Gold || before.Level != after.Level || before.Popularity != after.Popularity || before.Reputation != after.Reputation)
                differences.Add("회사 수치 불일치");
            if (before.CompletedProjectCount != after.CompletedProjectCount) differences.Add("완료 프로젝트 수 불일치");
            if (before.ActiveProject != after.ActiveProject) differences.Add("진행 프로젝트 존재 여부 불일치");
            if (before.ActiveProject && after.ActiveProject &&
                (before.ProjectName != after.ProjectName || before.ProjectDay != after.ProjectDay || before.ProjectNightCount != after.ProjectNightCount ||
                 !Approximately(before.ProjectQuality, after.ProjectQuality) || !Approximately(before.ProjectStability, after.ProjectStability) ||
                 !Approximately(before.ProjectCharm, after.ProjectCharm) || !before.ProjectEmployeeIds.SequenceEqual(after.ProjectEmployeeIds)))
                differences.Add("진행 프로젝트 데이터 불일치");
            if (!before.Employees.Keys.OrderBy(id => id).SequenceEqual(after.Employees.Keys.OrderBy(id => id)))
                differences.Add("직원 구성 불일치");
            else if (before.Employees.Keys.Any(id => !EmployeeSaveFieldsEqual(before.Employees[id], after.Employees[id])))
                differences.Add("직원 상태 수치 불일치");
            if (!DictionaryEqual(before.WeeklyBonusRaw, after.WeeklyBonusRaw)) differences.Add("주간 퀘스트 보정 불일치");
        }

        private static bool EmployeeSaveFieldsEqual(EmployeeState left, EmployeeState right)
        {
            return left.Ability == right.Ability && left.Desire == right.Desire && left.Fatigue == right.Fatigue &&
                   left.Loyalty == right.Loyalty && left.WorkStatus == right.WorkStatus;
        }

        private static bool DictionaryEqual(Dictionary<Role, int> left, Dictionary<Role, int> right)
        {
            foreach (Role role in new[] { Role.PLANNER, Role.ARTIST, Role.PROGRAMMER })
            {
                left.TryGetValue(role, out int leftValue);
                right.TryGetValue(role, out int rightValue);
                if (leftValue != rightValue) return false;
            }
            return true;
        }

        private void AddRecord(RuntimeCategory category, CheckLevel level, string title, string detail)
        {
            _records.Insert(0, new CheckRecord
            {
                Timestamp = DateTime.Now,
                Category = category,
                Level = level,
                Title = title,
                Detail = detail
            });
            if (_records.Count > MaxRecords)
                _records.RemoveRange(MaxRecords, _records.Count - MaxRecords);
        }

        private void OnGUI()
        {
            EnsureEmbeddedPanels();
            DrawRuntimeTabs();

            switch (_runtimeArea)
            {
                case RuntimeArea.Dialogue:
                    _dialoguePanel.DrawEmbeddedGUI();
                    return;
                case RuntimeArea.Report:
                    _reportPanel.DrawEmbeddedGUI();
                    return;
            }

            DrawToolbar();
            DrawCategoryTabs();
            DrawCurrentStatus();
            DrawActions();
            DrawSummary();
            DrawRecords();
        }

        private void DrawRuntimeTabs()
        {
            EditorGUILayout.Space(5f);
            _runtimeArea = (RuntimeArea)GUILayout.Toolbar(
                (int)_runtimeArea,
                new[] { "전체 흐름", "대화", "보고서" },
                GUILayout.Height(28f));
            EditorGUILayout.Space(4f);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _autoObserve = GUILayout.Toggle(_autoObserve, "자동 관찰", EditorStyles.toolbarButton, GUILayout.Width(85f));
                GUILayout.FlexibleSpace();
                _showPass = GUILayout.Toggle(_showPass, "PASS", EditorStyles.toolbarButton, GUILayout.Width(55f));
                _showWarning = GUILayout.Toggle(_showWarning, "WARN", EditorStyles.toolbarButton, GUILayout.Width(55f));
                _showFail = GUILayout.Toggle(_showFail, "FAIL", EditorStyles.toolbarButton, GUILayout.Width(55f));
                _showInfo = GUILayout.Toggle(_showInfo, "INFO", EditorStyles.toolbarButton, GUILayout.Width(55f));
                if (GUILayout.Button("지우기", EditorStyles.toolbarButton, GUILayout.Width(55f))) _records.Clear();
            }
        }

        private void DrawCategoryTabs()
        {
            EditorGUILayout.Space(5f);
            string[] labels = { "전체", "퀘스트/날짜", "프로젝트/재화", "인사", "저장/불러오기" };
            _category = (RuntimeCategory)GUILayout.Toolbar((int)_category, labels);
        }

        private static void DrawCurrentStatus()
        {
            RuntimeSnapshot snapshot = CaptureRuntimeSnapshot();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.LabelField("Play Mode에서 실제 플레이 흐름을 관찰합니다.", EditorStyles.boldLabel);
                    return;
                }
                if (!snapshot.IsReady)
                {
                    EditorGUILayout.HelpBox("필수 런타임 매니저가 준비되지 않았습니다.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.LabelField(FormatDate(snapshot), EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"자금 {snapshot.Gold:N0}G / 직원 {snapshot.Employees.Count}명 / 프로젝트 {(snapshot.ActiveProject ? snapshot.ProjectName : "없음")}");
                EditorGUILayout.LabelField($"일일 업무 완료 {snapshot.WorkCompleted} / 퀘스트 {snapshot.QuestState} {snapshot.QuestProgress}/{snapshot.QuestTarget}");
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("현재 상태 일관성 검사", GUILayout.Height(28f))) RunCurrentInvariants();
                if (GUILayout.Button("저장 전 기준 캡처", GUILayout.Height(28f))) CaptureSaveBaseline();
                if (GUILayout.Button("불러온 상태와 비교", GUILayout.Height(28f))) CompareSaveBaseline();
            }
            EditorGUILayout.HelpBox(
                "저장 검증: 기준 캡처 → 게임 UI에서 저장 → 상태를 변경 → 같은 슬롯 불러오기 → 불러온 상태와 비교 순서로 사용합니다.",
                MessageType.Info);
        }

        private void DrawSummary()
        {
            IEnumerable<CheckRecord> filtered = _records.Where(CategoryMatches);
            EditorGUILayout.LabelField(
                $"PASS {filtered.Count(record => record.Level == CheckLevel.Pass)} / " +
                $"WARN {filtered.Count(record => record.Level == CheckLevel.Warning)} / " +
                $"FAIL {filtered.Count(record => record.Level == CheckLevel.Fail)} / " +
                $"INFO {filtered.Count(record => record.Level == CheckLevel.Info)}",
                EditorStyles.boldLabel);
        }

        private void DrawRecords()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (CheckRecord record in _records.Where(CategoryMatches).Where(LevelMatches))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.HelpBox(
                        $"[{record.Level}] {record.Timestamp:HH:mm:ss} {record.Title}",
                        ToMessageType(record.Level));
                    EditorGUILayout.LabelField(record.Detail, EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private bool CategoryMatches(CheckRecord record) => _category == RuntimeCategory.All || record.Category == _category;
        private bool LevelMatches(CheckRecord record) => record.Level switch
        {
            CheckLevel.Pass => _showPass,
            CheckLevel.Warning => _showWarning,
            CheckLevel.Fail => _showFail,
            _ => _showInfo
        };

        private static MessageType ToMessageType(CheckLevel level) => level switch
        {
            CheckLevel.Fail => MessageType.Error,
            CheckLevel.Warning => MessageType.Warning,
            CheckLevel.Pass => MessageType.Info,
            _ => MessageType.None
        };

        private static int GetRawBonus(RuntimeSnapshot snapshot, Role role) =>
            snapshot.WeeklyBonusRaw.TryGetValue(role, out int value) ? value : 0;
        private static string FormatDate(RuntimeSnapshot snapshot) =>
            $"{snapshot.Week}주차 {snapshot.DayOfWeek} {snapshot.TimeOfDay} / 영업일 {snapshot.BusinessDay}";
        private static string FormatDelta(int value) => value > 0 ? $"+{value}" : value.ToString();
        private static string FormatIds(IEnumerable<int> ids) => string.Join(", ", ids);
        private static bool Approximately(float left, float right) => Mathf.Abs(left - right) <= 0.05f;

        private sealed class RuntimeSnapshot
        {
            public bool IsReady;
            public int Week;
            public DayOfWeek DayOfWeek;
            public TimeOfDay TimeOfDay;
            public int BusinessDay;
            public bool WorkCompleted;
            public int Gold;
            public int Level;
            public int Popularity;
            public int Reputation;
            public int CompletedProjectCount;
            public List<CompletedProjectState> CompletedProjects = new();
            public int OfficeMaintainCost;
            public bool ActiveProject;
            public Project Project;
            public string ProjectName;
            public int ProjectRequiredCost;
            public int ProjectDay;
            public int ProjectDuration;
            public int ProjectNightCount;
            public float ProjectQuality;
            public float ProjectStability;
            public float ProjectCharm;
            public float ProjectCurScore;
            public int[] ProjectEmployeeIds = Array.Empty<int>();
            public Dictionary<int, EmployeeState> Employees = new();
            public int[] LeftEmployeeIds = Array.Empty<int>();
            public int[] ApplicantIds = Array.Empty<int>();
            public RecruitRequestState[] RecruitRequests = Array.Empty<RecruitRequestState>();
            public int[] TrainingEmployeeIds = Array.Empty<int>();
            public bool HasQuestManager;
            public QuestState QuestState;
            public int QuestId;
            public Role QuestRole;
            public int QuestSuccessEffect;
            public int QuestProgress;
            public int QuestTarget;
            public Dictionary<Role, int> WeeklyBonusRaw = new();
        }

        private sealed class EmployeeState
        {
            public int Id;
            public string Name;
            public Role Role;
            public EmployeeWorkStatus WorkStatus;
            public int Ability;
            public int Desire;
            public int Fatigue;
            public int Loyalty;
            public int HiringCost;
            public int WeekSalary;
            public Employee Employee;
        }

        private sealed class CompletedProjectState
        {
            public string Name;
            public int DailyGold;
            public int DailyCost;
            public float Retention;
            public bool ServiceOver;
        }

        private readonly struct RecruitRequestState
        {
            public readonly Role Role;
            public readonly int Count;
            public RecruitRequestState(Role role, int count) { Role = role; Count = count; }
        }

        private sealed class CheckRecord
        {
            public DateTime Timestamp;
            public RuntimeCategory Category;
            public CheckLevel Level;
            public string Title;
            public string Detail;
        }
    }
}
