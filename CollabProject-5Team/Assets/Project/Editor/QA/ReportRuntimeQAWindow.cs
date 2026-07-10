using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class ReportRuntimeQAWindow : EditorWindow
    {
        private const int MaxRecords = 50;
        private const float ScoreTolerance = 0.05f;

        private readonly List<VerificationRecord> _records = new();
        private ApprovalSnapshot _armedSnapshot;
        private Vector2 _scrollPosition;
        private bool _autoCapture = true;
        private bool _showPassed = true;
        private bool _showFailed = true;
        private double _nextRepaintTime;

        public static void Open()
        {
            GameplayRuntimeQAWindow.OpenReportTab();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
                _armedSnapshot = null;

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
                return;

            Project project = GetActiveProject();
            if (project == null)
            {
                _armedSnapshot = null;
                RepaintPeriodically();
                return;
            }

            if (_armedSnapshot != null &&
                _armedSnapshot.Project == project &&
                project.nightCount > _armedSnapshot.NightCount)
            {
                VerifyApproval(_armedSnapshot, project);
                _armedSnapshot = null;
            }

            if (_autoCapture && project.selectedReports != null && project.selectedReports.Count > 0)
                _armedSnapshot = CaptureSnapshot(project, "자동");

            RepaintPeriodically();
        }

        private void RepaintPeriodically()
        {
            if (EditorApplication.timeSinceStartup < _nextRepaintTime)
                return;

            _nextRepaintTime = EditorApplication.timeSinceStartup + 0.2d;
            Repaint();
        }

        private static Project GetActiveProject()
        {
            if (Company.Instance == null || Company.Instance.activeProjectCount.Value < 1)
                return null;

            return Company.Instance.curProject;
        }

        private static ApprovalSnapshot CaptureSnapshot(Project project, string captureMode)
        {
            var selected = new Dictionary<Role, Report>(project.selectedReports);
            var pending = new List<Report>(project.pendingReports);
            var employees = new Dictionary<int, EmployeeBeforeState>();

            foreach (Report report in pending)
            {
                if (report?.owner == null || report.owner.so == null)
                    continue;

                int id = report.owner.so.id;
                if (employees.ContainsKey(id))
                    continue;

                employees[id] = EmployeeBeforeState.From(report.owner);
            }

            return new ApprovalSnapshot
            {
                Project = project,
                CaptureMode = captureMode,
                CapturedAt = DateTime.Now,
                NightCount = project.nightCount,
                Quality = project.qualityScore,
                Stability = project.stabilityScore,
                Charm = project.charmScore,
                CurScore = project.CurScore,
                SelectedReports = selected,
                PendingReports = pending,
                Employees = employees,
                PlannerQuestBonus = GetQuestBonus(Role.PLANNER),
                ProgrammerQuestBonus = GetQuestBonus(Role.PROGRAMMER),
                ArtistQuestBonus = GetQuestBonus(Role.ARTIST)
            };
        }

        private static float GetQuestBonus(Role role)
        {
            return QuestManager.Instance != null ? QuestManager.Instance.GetWeeklyBonus(role) : 0f;
        }

        private void VerifyApproval(ApprovalSnapshot before, Project actualProject)
        {
            ExpectedApproval expected = BuildExpectedApproval(before);
            var employeeResults = new List<EmployeeVerification>();

            foreach (ExpectedEmployeeState expectedEmployee in expected.Employees.Values)
            {
                Employee employee = expectedEmployee.Employee;
                EmployeeMutableData actual = employee != null ? employee.MutableData : default;
                bool exists = employee != null;
                bool desireMatch = exists && actual.desire == expectedEmployee.Desire;
                bool loyaltyMatch = exists && actual.loyalty == expectedEmployee.Loyalty;
                bool fatigueMatch = exists &&
                                    actual.fatigue >= expectedEmployee.FatigueMin &&
                                    actual.fatigue <= expectedEmployee.FatigueMax;

                employeeResults.Add(new EmployeeVerification
                {
                    EmployeeName = expectedEmployee.Name,
                    EmployeeId = expectedEmployee.Id,
                    ExpectedDesire = expectedEmployee.Desire,
                    ExpectedLoyalty = expectedEmployee.Loyalty,
                    ExpectedFatigueMin = expectedEmployee.FatigueMin,
                    ExpectedFatigueMax = expectedEmployee.FatigueMax,
                    ActualDesire = exists ? actual.desire : -1,
                    ActualLoyalty = exists ? actual.loyalty : -1,
                    ActualFatigue = exists ? actual.fatigue : -1,
                    Passed = desireMatch && loyaltyMatch && fatigueMatch,
                    Note = expectedEmployee.Note
                });
            }

            bool scoreMatch = Approximately(actualProject.qualityScore, expected.Quality) &&
                              Approximately(actualProject.stabilityScore, expected.Stability) &&
                              Approximately(actualProject.charmScore, expected.Charm) &&
                              Approximately(actualProject.CurScore, expected.CurScore);
            bool nightCountMatch = actualProject.nightCount == before.NightCount + 1;
            bool questReset = QuestManager.Instance == null ||
                              (Approximately(GetQuestBonus(Role.PLANNER), 0f) &&
                               Approximately(GetQuestBonus(Role.PROGRAMMER), 0f) &&
                               Approximately(GetQuestBonus(Role.ARTIST), 0f));
            bool contextValid = HasAllRequiredRoles(before.SelectedReports);

            _records.Insert(0, new VerificationRecord
            {
                Timestamp = DateTime.Now,
                ProjectName = actualProject.userNamed.Value,
                CaptureMode = before.CaptureMode,
                BeforeNightCount = before.NightCount,
                ActualNightCount = actualProject.nightCount,
                SelectedReports = expected.ReportResults,
                BeforeQuality = before.Quality,
                BeforeStability = before.Stability,
                BeforeCharm = before.Charm,
                ExpectedQuality = expected.Quality,
                ExpectedStability = expected.Stability,
                ExpectedCharm = expected.Charm,
                ExpectedCurScore = expected.CurScore,
                ActualQuality = actualProject.qualityScore,
                ActualStability = actualProject.stabilityScore,
                ActualCharm = actualProject.charmScore,
                ActualCurScore = actualProject.CurScore,
                EmployeeResults = employeeResults,
                ContextValid = contextValid,
                ScoreMatch = scoreMatch,
                NightCountMatch = nightCountMatch,
                QuestReset = questReset
            });

            if (_records.Count > MaxRecords)
                _records.RemoveRange(MaxRecords, _records.Count - MaxRecords);

            Repaint();
        }

        private static ExpectedApproval BuildExpectedApproval(ApprovalSnapshot before)
        {
            int nextNightCount = before.NightCount + 1;
            float qualityThisWeek = before.PlannerQuestBonus;
            float stabilityThisWeek = before.ProgrammerQuestBonus;
            float charmThisWeek = before.ArtistQuestBonus;
            var reportResults = new List<ReportVerification>();

            foreach (KeyValuePair<Role, Report> pair in before.SelectedReports)
            {
                Report report = pair.Value;
                if (report?.owner == null || report.so == null)
                    continue;

                float[] scores = ReportPolicy.CalcWeeklyStatScores(report);
                float roleAverage = scores.Length == 3 ? (scores[0] + scores[1] + scores[2]) / 3f : 0f;

                switch (report.role)
                {
                    case Role.PLANNER: qualityThisWeek += roleAverage; break;
                    case Role.PROGRAMMER: stabilityThisWeek += roleAverage; break;
                    case Role.ARTIST: charmThisWeek += roleAverage; break;
                }

                reportResults.Add(new ReportVerification
                {
                    Role = report.role,
                    Title = report.so.title,
                    EmployeeName = report.owner.so.Name,
                    Grade = report.grade,
                    Stat1 = scores.Length > 0 ? scores[0] : 0f,
                    Stat2 = scores.Length > 1 ? scores[1] : 0f,
                    Stat3 = scores.Length > 2 ? scores[2] : 0f,
                    RoleAverage = roleAverage,
                    QuestBonus = GetSnapshotQuestBonus(before, report.role)
                });
            }

            float expectedQuality = (before.Quality * before.NightCount + qualityThisWeek) / nextNightCount;
            float expectedStability = (before.Stability * before.NightCount + stabilityThisWeek) / nextNightCount;
            float expectedCharm = (before.Charm * before.NightCount + charmThisWeek) / nextNightCount;
            float expectedCurScore = Mathf.Clamp((expectedQuality + expectedStability + expectedCharm) / 3f, 0f, 100f);

            return new ExpectedApproval
            {
                Quality = expectedQuality,
                Stability = expectedStability,
                Charm = expectedCharm,
                CurScore = expectedCurScore,
                Employees = BuildExpectedEmployeeStates(before),
                ReportResults = reportResults
            };
        }

        private static Dictionary<int, ExpectedEmployeeState> BuildExpectedEmployeeStates(ApprovalSnapshot before)
        {
            var result = new Dictionary<int, ExpectedEmployeeState>();
            foreach (EmployeeBeforeState employee in before.Employees.Values)
            {
                result[employee.Id] = new ExpectedEmployeeState
                {
                    Employee = employee.Employee,
                    Id = employee.Id,
                    Name = employee.Name,
                    Desire = employee.Desire,
                    Loyalty = employee.Loyalty,
                    FatigueMin = employee.Fatigue,
                    FatigueMax = employee.Fatigue,
                    Note = "변화 없음"
                };
            }

            var acceptedReports = new HashSet<Report>(before.SelectedReports.Values);
            foreach (Report report in acceptedReports)
            {
                if (report?.owner == null || report.owner.so == null ||
                    !result.TryGetValue(report.owner.so.id, out ExpectedEmployeeState employee))
                    continue;

                var notes = new List<string>();
                if (employee.FatigueMin >= 80)
                {
                    employee.Desire = Mathf.Clamp(employee.Desire - 20, 0, 100);
                    employee.Loyalty = Mathf.Clamp(employee.Loyalty - 20, 0, 100);
                    notes.Add("고피로 의욕/충성 -20");
                }

                GetAcceptedFatigueRange(report.grade, out int minDelta, out int maxDelta);
                employee.FatigueMin = Mathf.Clamp(employee.FatigueMin + minDelta, 0, 100);
                employee.FatigueMax = Mathf.Clamp(employee.FatigueMax + maxDelta, 0, 100);
                notes.Add(minDelta == maxDelta
                    ? $"채택 피로 +{minDelta}"
                    : $"채택 피로 +{minDelta} 또는 +{maxDelta}");
                employee.Note = string.Join(", ", notes);
                result[employee.Id] = employee;
            }

            foreach (Report report in before.PendingReports)
            {
                if (report?.owner == null || report.owner.so == null || acceptedReports.Contains(report) ||
                    !result.TryGetValue(report.owner.so.id, out ExpectedEmployeeState employee))
                    continue;

                employee.FatigueMin = Mathf.Clamp(employee.FatigueMin - 5, 0, 100);
                employee.FatigueMax = Mathf.Clamp(employee.FatigueMax - 5, 0, 100);
                employee.Note = employee.Note == "변화 없음" ? "미채택 피로 -5" : employee.Note + ", 미채택 피로 -5";
                result[employee.Id] = employee;
            }

            return result;
        }

        private static void GetAcceptedFatigueRange(int grade, out int minDelta, out int maxDelta)
        {
            if (grade == 1) { minDelta = 5; maxDelta = 5; return; }
            if (grade == 2) { minDelta = 5; maxDelta = 10; return; }
            minDelta = 10;
            maxDelta = 10;
        }

        private static float GetSnapshotQuestBonus(ApprovalSnapshot snapshot, Role role)
        {
            return role switch
            {
                Role.PLANNER => snapshot.PlannerQuestBonus,
                Role.PROGRAMMER => snapshot.ProgrammerQuestBonus,
                Role.ARTIST => snapshot.ArtistQuestBonus,
                _ => 0f
            };
        }

        private static bool HasAllRequiredRoles(Dictionary<Role, Report> selected)
        {
            return selected.ContainsKey(Role.PLANNER) &&
                   selected.ContainsKey(Role.PROGRAMMER) &&
                   selected.ContainsKey(Role.ARTIST);
        }

        private static bool Approximately(float left, float right) => Mathf.Abs(left - right) <= ScoreTolerance;

        private void OnGUI()
        {
            DrawEmbeddedGUI();
        }

        internal void DrawEmbeddedGUI()
        {
            DrawToolbar();
            DrawGuide();
            DrawCurrentState();
            DrawSummary();
            DrawRecords();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _autoCapture = GUILayout.Toggle(_autoCapture, "자동 캡처", EditorStyles.toolbarButton, GUILayout.Width(90f));
                GUILayout.FlexibleSpace();
                _showPassed = GUILayout.Toggle(_showPassed, "PASS", EditorStyles.toolbarButton, GUILayout.Width(60f));
                _showFailed = GUILayout.Toggle(_showFailed, "FAIL", EditorStyles.toolbarButton, GUILayout.Width(60f));
                if (GUILayout.Button("결과 지우기", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    _records.Clear();
            }
        }

        private static void DrawGuide()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("보고서 승인 - 실제 플레이 검증", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play Mode에서 보고서를 선택하세요. 승인 직전 선택 상태를 기억한 뒤 프로젝트 점수, 퀘스트 보정, 직원 상태의 실제 반영 결과를 비교합니다.",
                MessageType.Info);
        }

        private void DrawCurrentState()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.LabelField("상태: Play Mode 대기 중");
                    return;
                }

                Project project = GetActiveProject();
                if (project == null)
                {
                    EditorGUILayout.LabelField("상태: 진행 중인 프로젝트 없음");
                    return;
                }

                EditorGUILayout.LabelField($"프로젝트: {project.userNamed.Value} / 보고서 선택 {project.selectedReports.Count}/3", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"현재 {project.nightCount}주 승인 / 완성도 {project.qualityScore:F1} / 안정성 {project.stabilityScore:F1} / 매력도 {project.charmScore:F1}");

                foreach (KeyValuePair<Role, Report> pair in project.selectedReports.OrderBy(pair => pair.Key))
                {
                    Report report = pair.Value;
                    EditorGUILayout.LabelField(
                        $"- {pair.Key}: {report?.so?.title ?? "보고서 없음"} / {report?.owner?.so?.Name ?? "직원 없음"}",
                        EditorStyles.miniLabel);
                }

                if (GUILayout.Button("현재 선택 상태 수동 캡처", GUILayout.Height(26f)))
                    _armedSnapshot = CaptureSnapshot(project, "수동");

                if (_armedSnapshot != null)
                    EditorGUILayout.LabelField(
                        $"캡처됨: {_armedSnapshot.CapturedAt:HH:mm:ss} / {_armedSnapshot.CaptureMode} / 선택 {_armedSnapshot.SelectedReports.Count}개",
                        EditorStyles.miniLabel);
            }
        }

        private void DrawSummary()
        {
            int passed = _records.Count(record => record.Passed);
            int failed = _records.Count - passed;
            EditorGUILayout.LabelField($"검증 결과: PASS {passed} / FAIL {failed} / 전체 {_records.Count}", EditorStyles.boldLabel);
        }

        private void DrawRecords()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (_records.Count == 0)
                EditorGUILayout.HelpBox("아직 승인 검증 결과가 없습니다.", MessageType.None);

            foreach (VerificationRecord record in _records)
            {
                if ((record.Passed && !_showPassed) || (!record.Passed && !_showFailed))
                    continue;

                DrawRecord(record);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawRecord(VerificationRecord record)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    $"[{(record.Passed ? "PASS" : "FAIL")}] {record.Timestamp:HH:mm:ss} {record.ProjectName} " +
                    $"({record.BeforeNightCount}주 → {record.ActualNightCount}주, {record.CaptureMode})",
                    record.Passed ? MessageType.Info : MessageType.Error);

                foreach (ReportVerification report in record.SelectedReports)
                {
                    EditorGUILayout.LabelField(
                        $"{report.Role} | {report.EmployeeName} | {report.Title} | {report.Grade}등급",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"세부 {report.Stat1:F1} / {report.Stat2:F1} / {report.Stat3:F1} → 평균 {report.RoleAverage:F1} + 퀘스트 {report.QuestBonus:F1}",
                        EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField(
                    "완성도",
                    $"이전 {record.BeforeQuality:F1} / 예상 {record.ExpectedQuality:F1} / 실제 {record.ActualQuality:F1}");
                EditorGUILayout.LabelField(
                    "안정성",
                    $"이전 {record.BeforeStability:F1} / 예상 {record.ExpectedStability:F1} / 실제 {record.ActualStability:F1}");
                EditorGUILayout.LabelField(
                    "매력도",
                    $"이전 {record.BeforeCharm:F1} / 예상 {record.ExpectedCharm:F1} / 실제 {record.ActualCharm:F1}");
                EditorGUILayout.LabelField("종합", $"예상 {record.ExpectedCurScore:F1} / 실제 {record.ActualCurScore:F1}");

                foreach (EmployeeVerification employee in record.EmployeeResults)
                {
                    string expectedFatigue = employee.ExpectedFatigueMin == employee.ExpectedFatigueMax
                        ? employee.ExpectedFatigueMin.ToString()
                        : $"{employee.ExpectedFatigueMin}~{employee.ExpectedFatigueMax}";
                    EditorGUILayout.LabelField(
                        $"{(employee.Passed ? "PASS" : "FAIL")} {employee.EmployeeName}({employee.EmployeeId})",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"예상 의욕 {employee.ExpectedDesire} / 피로 {expectedFatigue} / 충성 {employee.ExpectedLoyalty} | " +
                        $"실제 의욕 {employee.ActualDesire} / 피로 {employee.ActualFatigue} / 충성 {employee.ActualLoyalty}",
                        EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(employee.Note, EditorStyles.miniLabel);
                }

                if (!record.ContextValid)
                    EditorGUILayout.HelpBox("기획/아트/개발 보고서가 모두 선택되지 않은 상태에서 승인됐습니다.", MessageType.Error);
                if (!record.ScoreMatch)
                    EditorGUILayout.HelpBox("예상 프로젝트 점수와 실제 누적 점수가 다릅니다.", MessageType.Error);
                if (!record.NightCountMatch)
                    EditorGUILayout.HelpBox("보고서 승인 후 nightCount가 정확히 1 증가하지 않았습니다.", MessageType.Error);
                if (!record.QuestReset)
                    EditorGUILayout.HelpBox("보고서 승인 후 주간 퀘스트 보정값이 초기화되지 않았습니다.", MessageType.Error);
            }
        }

        private sealed class ApprovalSnapshot
        {
            public Project Project;
            public string CaptureMode;
            public DateTime CapturedAt;
            public int NightCount;
            public float Quality;
            public float Stability;
            public float Charm;
            public float CurScore;
            public Dictionary<Role, Report> SelectedReports;
            public List<Report> PendingReports;
            public Dictionary<int, EmployeeBeforeState> Employees;
            public float PlannerQuestBonus;
            public float ProgrammerQuestBonus;
            public float ArtistQuestBonus;
        }

        private sealed class ExpectedApproval
        {
            public float Quality;
            public float Stability;
            public float Charm;
            public float CurScore;
            public Dictionary<int, ExpectedEmployeeState> Employees;
            public List<ReportVerification> ReportResults;
        }

        private sealed class VerificationRecord
        {
            public DateTime Timestamp;
            public string ProjectName;
            public string CaptureMode;
            public int BeforeNightCount;
            public int ActualNightCount;
            public List<ReportVerification> SelectedReports;
            public float BeforeQuality;
            public float BeforeStability;
            public float BeforeCharm;
            public float ExpectedQuality;
            public float ExpectedStability;
            public float ExpectedCharm;
            public float ExpectedCurScore;
            public float ActualQuality;
            public float ActualStability;
            public float ActualCharm;
            public float ActualCurScore;
            public List<EmployeeVerification> EmployeeResults;
            public bool ContextValid;
            public bool ScoreMatch;
            public bool NightCountMatch;
            public bool QuestReset;
            public bool Passed => ContextValid && ScoreMatch && NightCountMatch && QuestReset && EmployeeResults.All(result => result.Passed);
        }

        private sealed class ReportVerification
        {
            public Role Role;
            public string Title;
            public string EmployeeName;
            public int Grade;
            public float Stat1;
            public float Stat2;
            public float Stat3;
            public float RoleAverage;
            public float QuestBonus;
        }

        private sealed class EmployeeVerification
        {
            public string EmployeeName;
            public int EmployeeId;
            public int ExpectedDesire;
            public int ExpectedLoyalty;
            public int ExpectedFatigueMin;
            public int ExpectedFatigueMax;
            public int ActualDesire;
            public int ActualLoyalty;
            public int ActualFatigue;
            public bool Passed;
            public string Note;
        }

        private sealed class EmployeeBeforeState
        {
            public Employee Employee;
            public int Id;
            public string Name;
            public int Desire;
            public int Loyalty;
            public int Fatigue;

            public static EmployeeBeforeState From(Employee employee)
            {
                return new EmployeeBeforeState
                {
                    Employee = employee,
                    Id = employee.so.id,
                    Name = employee.so.Name,
                    Desire = employee.MutableData.desire,
                    Loyalty = employee.MutableData.loyalty,
                    Fatigue = employee.MutableData.fatigue
                };
            }
        }

        private sealed class ExpectedEmployeeState
        {
            public Employee Employee;
            public int Id;
            public string Name;
            public int Desire;
            public int Loyalty;
            public int FatigueMin;
            public int FatigueMax;
            public string Note;
        }
    }
}
