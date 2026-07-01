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
        private bool _useEmployeeSourceValues = true;
        private bool _hasManualStartValues;
        private int _manualAbility;
        private int _manualDesire;
        private int _manualFatigue;
        private int _manualLoyalty;
        private bool _usePlayModeEmployees;
        private bool _inProject = true;
        private bool _talkEveryWeek = true;
        private bool _acceptReportEveryWeek = true;
        private int _acceptedReportGrade = 2;
        private TrainingPlan _trainingPlan = TrainingPlan.None;
        private bool _applyCompletionReward;
        private ProjectSize _completionProjectSize = ProjectSize.Small;
        private ProjectGradeOption _completionGrade = ProjectGradeOption.B;
        private bool _showTimelineDetails;
        private bool _hasBaseline;
        private WeekSnapshot _baselineLast;

        [MenuItem("Tools/Balance/6. Employee Lifecycle", false, 206)]
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
            DrawSummary();
            DrawEmployeeGuide();
            DrawInputPanel();
            DrawTimelineDetailsPanel();
            EditorGUILayout.EndScrollView();
        }


        private static void DrawEmployeeGuide()
        {
            BalanceGuideUI.Draw(
                "직원 상태 밸런싱 가이드",
                "대화 여부\n보고서 채택 여부/등급\n교육 계획\n프로젝트 완료 보상",
                "능력/의욕/피로/충성 변화\n퇴사 후보 여부\n몇 주 뒤 위험해지는지",
                "대화 1회 차이로 변화가 너무 큼\n교육 효율이 너무 높거나 낮음\n피로가 쉽게 80 이상 고정됨");
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
            EditorGUILayout.LabelField("직원 상태 수치 조절", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "대화, 보고서 채택, 교육, 프로젝트 완료 보상이 몇 주 뒤 능력/의욕/피로/충성도에 어떤 영향을 주는지 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);
            BalanceGuideUI.DrawDataFlow(
                "기준 직원은 Employee SO 또는 Play Mode 상태에서 가져옵니다. 대화/보고서/교육/보상 조건은 이 창에서 가정합니다.",
                "행동 조건은 시뮬레이션에만 반영되며 실제 직원 데이터는 수정하지 않습니다.",
                "주간 행동 조건 -> 능력/의욕/피로/충성 변화 -> 위험 주차/퇴사 후보 여부");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("1. 기준 직원", EditorStyles.boldLabel);
                _roleFilter = (Role)EditorGUILayout.EnumPopup("직군", _roleFilter);

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
                int previousEmployeeIndex = _selectedEmployeeIndex;
                _selectedEmployeeIndex = EditorGUILayout.Popup("직원", _selectedEmployeeIndex, employeeOptions);
                EmployeeSnapshot selectedEmployee = filteredEmployees[_selectedEmployeeIndex];
                if (previousEmployeeIndex != _selectedEmployeeIndex)
                    _hasManualStartValues = false;

                _weeks = EditorGUILayout.IntSlider("관찰 기간(주)", _weeks, 1, 24);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("1-1. 시작 상태 직접 조정", EditorStyles.boldLabel);
                _useEmployeeSourceValues = EditorGUILayout.Toggle("직원 원본값 사용", _useEmployeeSourceValues);
                if (_useEmployeeSourceValues)
                {
                    _hasManualStartValues = false;
                    EditorGUILayout.LabelField(
                        $"시작값: 능력 {selectedEmployee.Ability} / 의욕 {selectedEmployee.Desire} / 피로 {selectedEmployee.Fatigue} / 충성 {selectedEmployee.Loyalty}",
                        EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    EnsureManualStartValues(selectedEmployee);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("이 값은 시뮬레이션 시작값만 바꾸며 Employee SO나 Play Mode 직원 데이터는 수정하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
                        _manualAbility = EditorGUILayout.IntSlider("시작 능력치", _manualAbility, 0, 100);
                        _manualDesire = EditorGUILayout.IntSlider("시작 의욕", _manualDesire, 0, 100);
                        _manualFatigue = EditorGUILayout.IntSlider("시작 피로도", _manualFatigue, 0, 100);
                        _manualLoyalty = EditorGUILayout.IntSlider("시작 충성도", _manualLoyalty, 0, 100);
                    }
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("2. 주간 행동 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _inProject = EditorGUILayout.Toggle("프로젝트 투입", _inProject);
                    _talkEveryWeek = EditorGUILayout.Toggle("매주 대화", _talkEveryWeek);
                    _acceptReportEveryWeek = EditorGUILayout.Toggle("보고서 채택", _acceptReportEveryWeek);
                }
                using (new EditorGUI.DisabledScope(!_acceptReportEveryWeek))
                    _acceptedReportGrade = EditorGUILayout.IntSlider("채택 보고서 등급", _acceptedReportGrade, 1, 3);
                EditorGUILayout.LabelField("고피로 상태에서 보고서를 계속 채택하면 의욕/충성 하락과 퇴사 후보 전환을 확인할 수 있습니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("3. 성장/보상 조건", EditorStyles.boldLabel);
                _trainingPlan = (TrainingPlan)EditorGUILayout.EnumPopup("교육 계획", _trainingPlan);
                _applyCompletionReward = EditorGUILayout.Toggle("마지막 주 프로젝트 완료 보상", _applyCompletionReward);
                using (new EditorGUI.DisabledScope(!_applyCompletionReward))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _completionProjectSize = (ProjectSize)EditorGUILayout.EnumPopup("완료 프로젝트 규모", _completionProjectSize);
                        _completionGrade = (ProjectGradeOption)EditorGUILayout.EnumPopup("완료 프로젝트 등급", _completionGrade);
                    }
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
                BalanceGuideUI.DrawFormulaNotice("직원 변화는 현재 직원 성장/피로/퇴사 후보 공식으로 계산됩니다.");
                DrawEmployeeBaselineControls(last);
                DrawEmployeeAutoChecks(last);
                DrawEmployeeInterpretation(first, last);

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


        private void DrawEmployeeBaselineControls(WeekSnapshot last)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("변경 전후 비교", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(24f)))
                    {
                        _baselineLast = last;
                        _hasBaseline = true;
                    }

                    using (new EditorGUI.DisabledScope(!_hasBaseline))
                    {
                        if (GUILayout.Button("기준값 지우기", GUILayout.Width(110f), GUILayout.Height(24f)))
                            _hasBaseline = false;
                    }
                }

                BalanceGuideUI.DrawBaselineHint(_hasBaseline);
                if (_hasBaseline)
                {
                    EditorGUILayout.LabelField(BuildDeltaText("능력", last.Ability, _baselineLast.Ability), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("의욕", last.Desire, _baselineLast.Desire), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("피로", last.Fatigue, _baselineLast.Fatigue), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("충성", last.Loyalty, _baselineLast.Loyalty), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawEmployeeAutoChecks(WeekSnapshot last)
        {
            WeekSnapshot firstRisk = _timeline.FirstOrDefault(t => t.IsLeavePending || t.Fatigue >= 80 || t.Desire < 40);
            bool hasRiskWeek = firstRisk.Week > 0 || firstRisk.IsLeavePending;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 감지", EditorStyles.boldLabel);
                BalanceGuideUI.DrawAutoCheck("퇴사 후보", last.IsLeavePending, last.IsLeavePending ? "마지막 주차 기준 퇴사 후보입니다." : "마지막 주차 기준 퇴사 후보가 아닙니다.");
                BalanceGuideUI.DrawAutoCheck("위험 주차", hasRiskWeek, hasRiskWeek ? $"{firstRisk.Week}주차에 피로/의욕/퇴사 위험 조건이 감지됩니다." : "관찰 기간 동안 위험 주차가 없습니다.");
            }
        }

        private static string BuildDeltaText(string label, int current, int baseline)
        {
            int delta = current - baseline;
            return $"{label}: 현재 {current} / 기준 {baseline} / 차이 {delta:+0;-0;0}";
        }

        private static void DrawEmployeeInterpretation(WeekSnapshot first, WeekSnapshot last)
        {
            if (last.IsLeavePending)
            {
                BalanceGuideUI.DrawInterpretation("퇴사 후보 상태입니다. 피로 누적, 의욕 하락, 충성도 하락 조건을 먼저 확인하세요.", MessageType.Warning);
                return;
            }

            if (last.Fatigue >= 80 || last.Desire < 40)
            {
                BalanceGuideUI.DrawInterpretation("아직 퇴사 후보는 아니지만 직원 상태가 위험권에 가깝습니다.", MessageType.Warning);
                return;
            }

            if (last.Ability > first.Ability && last.Loyalty >= first.Loyalty)
                BalanceGuideUI.DrawInterpretation("성장과 충성도가 함께 유지되는 안정적인 직원 운영 조건입니다.");
            else
                BalanceGuideUI.DrawInterpretation("큰 사고는 없지만 성장/충성 보상이 약할 수 있습니다. 교육과 대화 조건을 비교해보세요.");
        }

        private void DrawTimelineDetailsPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showTimelineDetails = EditorGUILayout.Foldout(_showTimelineDetails, "주차별 상세 표", true);
                if (_showTimelineDetails)
                    DrawTimeline();
                else
                    EditorGUILayout.LabelField("요약에서 퇴사 위험이나 큰 변화가 보일 때 펼쳐서 주차별 원인을 확인합니다.", EditorStyles.wordWrappedMiniLabel);
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

            EnsureManualStartValues(employee);
            int ability = _useEmployeeSourceValues ? employee.Ability : _manualAbility;
            int desire = _useEmployeeSourceValues ? employee.Desire : _manualDesire;
            int fatigue = _useEmployeeSourceValues ? employee.Fatigue : _manualFatigue;
            int loyalty = _useEmployeeSourceValues ? employee.Loyalty : _manualLoyalty;
            string startNote = _useEmployeeSourceValues ? "시작값" : "수동 시작값";

            _timeline.Add(new WeekSnapshot(0, ability, desire, fatigue, loyalty, IsLeavePending(desire, fatigue, loyalty), startNote));

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

        private void EnsureManualStartValues(EmployeeSnapshot employee)
        {
            if (_hasManualStartValues || !employee.IsValid)
                return;

            _manualAbility = employee.Ability;
            _manualDesire = employee.Desire;
            _manualFatigue = employee.Fatigue;
            _manualLoyalty = employee.Loyalty;
            _hasManualStartValues = true;
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
