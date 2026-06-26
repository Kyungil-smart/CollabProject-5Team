using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class EmployeeLifecycleSimulatorWindow : EditorWindow
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const int TrainingDurationWeeks = 4;
        private const float DailyLeaveChance = 0.25f;

        private readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        private readonly List<WeekSnapshot> _timeline = new();

        private Vector2 _scrollPosition;
        private Role _roleFilter = Role.PLANNER;
        private int _selectedEmployeeIndex;
        private int _weeks = 8;
        private bool _usePlayModeEmployees;
        private bool _inProject = true;
        private bool _talkEveryWeek = true;
        private bool _acceptReportEveryWeek = true;
        private int _acceptedReportGrade = 2;
        private TrainingPlan _trainingPlan = TrainingPlan.None;
        private bool _applyCompletionReward;
        private ProjectSize _completionProjectSize = ProjectSize.Small;
        private ProjectGradeOption _completionGrade = ProjectGradeOption.B;

        [MenuItem("Tools/Simulation/Employee Lifecycle Simulator")]
        public static void Open()
        {
            EmployeeLifecycleSimulatorWindow window = GetWindow<EmployeeLifecycleSimulatorWindow>("Employee Lifecycle");
            window.minSize = new Vector2(680f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssets();
            Simulate();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawInputPanel();
            DrawSummary();
            DrawTimeline();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    RefreshAssets();
                    Simulate();
                }

                if (GUILayout.Button("시뮬레이션 실행", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    Simulate();

                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                EditorGUI.BeginChangeCheck();
                _usePlayModeEmployees = GUILayout.Toggle(_usePlayModeEmployees, "Play Mode 직원", EditorStyles.toolbarButton, GUILayout.Width(110f));
                if (EditorGUI.EndChangeCheck())
                    Simulate();
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("직원 생애주기 밸런스 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "프로젝트 투입, 대화 여부, 보고서 승인, 교육, 완료 보상이 몇 주 뒤 직원 상태와 퇴사 위험에 어떤 영향을 주는지 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                _roleFilter = (Role)EditorGUILayout.EnumPopup("직군 필터", _roleFilter);

                List<EmployeeSnapshot> filteredEmployees = GetFilteredEmployees();
                if (filteredEmployees.Count == 0)
                {
                    EditorGUILayout.HelpBox("선택한 직군의 직원 데이터가 없습니다.", MessageType.Warning);
                    EditorGUI.EndChangeCheck();
                    return;
                }

                _selectedEmployeeIndex = Mathf.Clamp(_selectedEmployeeIndex, 0, filteredEmployees.Count - 1);
                string[] employeeOptions = filteredEmployees
                    .Select(e => $"{e.So.id} / {e.So.Name} / 능력 {e.Ability} / 의욕 {e.Desire} / 피로 {e.Fatigue} / 충성 {e.Loyalty}")
                    .ToArray();
                _selectedEmployeeIndex = EditorGUILayout.Popup("직원", _selectedEmployeeIndex, employeeOptions);

                _weeks = EditorGUILayout.IntSlider("시뮬레이션 주차", _weeks, 1, 24);
                _inProject = EditorGUILayout.Toggle("프로젝트 투입 상태", _inProject);
                _talkEveryWeek = EditorGUILayout.Toggle("매주 대화함", _talkEveryWeek);
                _acceptReportEveryWeek = EditorGUILayout.Toggle("매주 보고서 채택", _acceptReportEveryWeek);

                using (new EditorGUI.DisabledScope(!_acceptReportEveryWeek))
                    _acceptedReportGrade = EditorGUILayout.IntSlider("채택 보고서 등급", _acceptedReportGrade, 1, 3);

                _trainingPlan = (TrainingPlan)EditorGUILayout.EnumPopup("교육 계획", _trainingPlan);

                _applyCompletionReward = EditorGUILayout.Toggle("마지막 주 프로젝트 완료 보상", _applyCompletionReward);
                using (new EditorGUI.DisabledScope(!_applyCompletionReward))
                {
                    _completionProjectSize = (ProjectSize)EditorGUILayout.EnumPopup("완료 프로젝트 규모", _completionProjectSize);
                    _completionGrade = (ProjectGradeOption)EditorGUILayout.EnumPopup("완료 프로젝트 등급", _completionGrade);
                }

                if (_usePlayModeEmployees)
                {
                    EditorGUILayout.HelpBox(
                        "Play Mode 직원 사용 중입니다. 현재 고용 직원의 성장 능력치, 의욕, 피로도, 충성도를 시작값으로 읽습니다.",
                        MessageType.Info);
                }

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }
        }

        private void DrawSummary()
        {
            if (_timeline.Count == 0)
                return;

            WeekSnapshot first = _timeline[0];
            WeekSnapshot last = _timeline[_timeline.Count - 1];

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"시작: 능력 {first.Ability} / 의욕 {first.Desire} / 피로 {first.Fatigue} / 충성 {first.Loyalty}");
                EditorGUILayout.LabelField($"결과: 능력 {last.Ability} / 의욕 {last.Desire} / 피로 {last.Fatigue} / 충성 {last.Loyalty}");
                EditorGUILayout.LabelField($"변화: 능력 {FormatDelta(last.Ability - first.Ability)} / 의욕 {FormatDelta(last.Desire - first.Desire)} / 피로 {FormatDelta(last.Fatigue - first.Fatigue)} / 충성 {FormatDelta(last.Loyalty - first.Loyalty)}");

                if (last.IsLeavePending)
                {
                    EditorGUILayout.HelpBox(
                        $"퇴사 후보 상태입니다. 실제 코드 기준 일일 퇴사 확률은 {DailyLeaveChance:P0}이며, 5일 동안 최소 1회 퇴사 처리될 확률은 {CalcLeaveChanceOverDays(5):P1}입니다.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("현재 조건에서는 마지막 주차 기준 퇴사 후보 상태가 아닙니다.", MessageType.Info);
                }
            }
        }

        private void DrawTimeline()
        {
            if (_timeline.Count == 0)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("주차별 변화", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Week / Ability / Desire / Fatigue / Loyalty / Risk / Note", EditorStyles.miniLabel);

                foreach (WeekSnapshot snapshot in _timeline)
                {
                    string risk = snapshot.IsLeavePending ? "퇴사 후보" : "-";
                    EditorGUILayout.LabelField(
                        $"{snapshot.Week,2}주차  능력 {snapshot.Ability,3}  의욕 {snapshot.Desire,3}  피로 {snapshot.Fatigue,3}  충성 {snapshot.Loyalty,3}  {risk}  {snapshot.Note}",
                        snapshot.IsLeavePending ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                }
            }
        }

        private void Simulate()
        {
            _timeline.Clear();

            EmployeeSnapshot employee = GetSelectedEmployee();
            if (!employee.IsValid)
                return;

            int ability = employee.Ability;
            int desire = employee.Desire;
            int fatigue = employee.Fatigue;
            int loyalty = employee.Loyalty;

            _timeline.Add(new WeekSnapshot(0, ability, desire, fatigue, loyalty, IsLeavePending(desire, fatigue, loyalty), "시작값"));

            for (int week = 1; week <= _weeks; week++)
            {
                List<string> notes = new();

                if (_inProject)
                {
                    int weeklyGrowth = ApplyAbilityGrowth(ability, PerkPolicy.CalcWeeklyAbilityDelta(loyalty));
                    ability = ClampStat(ability + weeklyGrowth);
                    notes.Add($"주간성장 {FormatDelta(weeklyGrowth)}");

                    if (!_talkEveryWeek)
                    {
                        loyalty = ClampStat(loyalty - 5);
                        desire = ClampStat(desire - 5);
                        fatigue = ClampStat(fatigue + 5);
                        notes.Add("대화없음");
                    }

                    if (_acceptReportEveryWeek)
                    {
                        if (fatigue >= 80)
                        {
                            desire = ClampStat(desire - 20);
                            loyalty = ClampStat(loyalty - 20);
                            notes.Add("고피로 채택 페널티");
                        }

                        int fatigueDelta = GetAcceptedReportFatigueDelta(_acceptedReportGrade);
                        fatigue = ClampStat(fatigue + fatigueDelta);
                        notes.Add($"보고서 피로 +{fatigueDelta}");
                    }
                }

                if (_trainingPlan != TrainingPlan.None && week % TrainingDurationWeeks == 0)
                {
                    TrainingCourseData course = GetTrainingCourse(_trainingPlan);
                    int expectedGain = Mathf.RoundToInt(((course.MinDelta + course.MaxDelta) * 0.5f) * (1f - course.FailureRate));
                    ability = ClampStat(ability + expectedGain);
                    notes.Add($"{course.Name} 기대성장 {FormatDelta(expectedGain)}");
                }

                if (_applyCompletionReward && week == _weeks)
                {
                    char grade = GetCompletionGradeChar(_completionGrade);
                    int abilityDelta = ApplyAbilityGrowth(ability, PerkPolicy.CalcCompletionAbilityDelta(_completionProjectSize, grade));
                    int loyaltyDelta = PerkPolicy.CalcCompletionLoyaltyDelta(_completionProjectSize, grade);
                    ability = ClampStat(ability + abilityDelta);
                    loyalty = ClampStat(loyalty + loyaltyDelta);
                    notes.Add($"완료보상 능력 {FormatDelta(abilityDelta)} 충성 {FormatDelta(loyaltyDelta)}");
                }

                bool leavePending = IsLeavePending(desire, fatigue, loyalty);
                _timeline.Add(new WeekSnapshot(
                    week,
                    ability,
                    desire,
                    fatigue,
                    loyalty,
                    leavePending,
                    notes.Count == 0 ? "-" : string.Join(", ", notes)));
            }
        }

        private void RefreshAssets()
        {
            _employees.Clear();
            _employees.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot)
                .OrderBy(e => e.Asset.role)
                .ThenBy(e => e.Asset.id));
        }

        private List<EmployeeSnapshot> GetFilteredEmployees()
        {
            return GetEmployeesForRole(_roleFilter);
        }

        private List<EmployeeSnapshot> GetEmployeesForRole(Role role)
        {
            if (_usePlayModeEmployees && Application.isPlaying && _EmployeeManager.Instance != null)
            {
                return _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                    .Where(e => e != null && e.so != null && e.so.role == role)
                    .OrderBy(e => e.so.id)
                    .Select(EmployeeSnapshot.FromRuntime)
                    .ToList();
            }

            return _employees
                .Where(e => e.Asset.role == role)
                .OrderBy(e => e.Asset.id)
                .Select(EmployeeSnapshot.FromAsset)
                .ToList();
        }

        private EmployeeSnapshot GetSelectedEmployee()
        {
            List<EmployeeSnapshot> filteredEmployees = GetFilteredEmployees();
            if (filteredEmployees.Count == 0)
                return default;

            _selectedEmployeeIndex = Mathf.Clamp(_selectedEmployeeIndex, 0, filteredEmployees.Count - 1);
            return filteredEmployees[_selectedEmployeeIndex];
        }

        private static int ApplyAbilityGrowth(int currentAbility, int delta)
        {
            if (delta <= 0)
                return delta;

            float growthRate = currentAbility <= 40 ? 1.0f :
                               currentAbility <= 60 ? 0.8f :
                               currentAbility <= 80 ? 0.6f : 0.4f;
            return Mathf.CeilToInt(delta * growthRate);
        }

        private static int GetAcceptedReportFatigueDelta(int grade)
        {
            return grade switch
            {
                1 => 5,
                2 => 10,
                _ => 15
            };
        }

        private static bool IsLeavePending(int desire, int fatigue, int loyalty)
        {
            return fatigue >= 100 || desire <= 0 || loyalty <= 0;
        }

        private static float CalcLeaveChanceOverDays(int days)
        {
            return 1f - Mathf.Pow(1f - DailyLeaveChance, days);
        }

        private static int ClampStat(int value)
        {
            return Mathf.Clamp(value, 0, 100);
        }

        private static string FormatDelta(int delta)
        {
            return delta > 0 ? $"+{delta}" : delta.ToString();
        }

        private static TrainingCourseData GetTrainingCourse(TrainingPlan plan)
        {
            return plan switch
            {
                TrainingPlan.Basic => new TrainingCourseData("기본 교육", 1, 3, 0.1f),
                TrainingPlan.Professional => new TrainingCourseData("전문 교육", 3, 6, 0.2f),
                TrainingPlan.Intensive => new TrainingCourseData("집중 교육", 5, 10, 0.3f),
                _ => default
            };
        }

        private static char GetCompletionGradeChar(ProjectGradeOption grade)
        {
            return grade switch
            {
                ProjectGradeOption.S => 'S',
                ProjectGradeOption.A => 'A',
                ProjectGradeOption.B => 'B',
                _ => 'C'
            };
        }

        private readonly struct EmployeeSnapshot
        {
            public EmployeeImmutableData So { get; }
            public int Ability { get; }
            public int Desire { get; }
            public int Fatigue { get; }
            public int Loyalty { get; }
            public bool IsValid => So != null;

            private EmployeeSnapshot(EmployeeImmutableData so, int ability, int desire, int fatigue, int loyalty)
            {
                So = so;
                Ability = ability;
                Desire = desire;
                Fatigue = fatigue;
                Loyalty = loyalty;
            }

            public static EmployeeSnapshot FromAsset(AssetEntry<EmployeeImmutableData> entry)
            {
                EmployeeImmutableData so = entry.Asset;
                return new EmployeeSnapshot(so, so.ability, so.desire, so.fatigue, so.loyalty);
            }

            public static EmployeeSnapshot FromRuntime(Employee employee)
            {
                EmployeeMutableData data = employee.MutableData;
                return new EmployeeSnapshot(employee.so, data.ability, data.desire, data.fatigue, data.loyalty);
            }
        }

        private readonly struct WeekSnapshot
        {
            public int Week { get; }
            public int Ability { get; }
            public int Desire { get; }
            public int Fatigue { get; }
            public int Loyalty { get; }
            public bool IsLeavePending { get; }
            public string Note { get; }

            public WeekSnapshot(int week, int ability, int desire, int fatigue, int loyalty, bool isLeavePending, string note)
            {
                Week = week;
                Ability = ability;
                Desire = desire;
                Fatigue = fatigue;
                Loyalty = loyalty;
                IsLeavePending = isLeavePending;
                Note = note;
            }
        }

        private readonly struct TrainingCourseData
        {
            public string Name { get; }
            public int MinDelta { get; }
            public int MaxDelta { get; }
            public float FailureRate { get; }

            public TrainingCourseData(string name, int minDelta, int maxDelta, float failureRate)
            {
                Name = name;
                MinDelta = minDelta;
                MaxDelta = maxDelta;
                FailureRate = failureRate;
            }
        }

        private enum TrainingPlan
        {
            None,
            Basic,
            Professional,
            Intensive
        }

        private enum ProjectGradeOption
        {
            S,
            A,
            B,
            C
        }
    }
}
