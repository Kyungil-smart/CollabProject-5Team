using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class ReportSimulatorWindow : EditorWindow
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const string ReportRoot = "Assets/Project/DB/Report";

        private readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        private readonly List<AssetEntry<ReportSO>> _reports = new();
        private readonly List<ReportCandidatePreview> _candidates = new();

        private Vector2 _scrollPosition;
        private Role _roleFilter = Role.PLANNER;
        private int _selectedEmployeeIndex;
        private int _startRepo = 1;
        private bool _overrideDesire;
        private int _desireOverride = 50;
        private string _searchText = string.Empty;
        private bool _showContent = true;
        private bool _usePlayModeEmployees;

        [MenuItem("Tools/Simulation/Report Simulator")]
        public static void Open()
        {
            ReportSimulatorWindow window = GetWindow<ReportSimulatorWindow>("Report Simulator");
            window.minSize = new Vector2(620f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssets();
            SimulateSelectedEmployee();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawInputPanel();
            DrawSummary();
            DrawCandidates();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                {
                    RefreshAssets();
                    SimulateSelectedEmployee();
                }

                if (GUILayout.Button("선택 직원 시뮬레이션", EditorStyles.toolbarButton, GUILayout.Width(140f)))
                    SimulateSelectedEmployee();

                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                EditorGUI.BeginChangeCheck();
                _usePlayModeEmployees = GUILayout.Toggle(_usePlayModeEmployees, "Play Mode 직원", EditorStyles.toolbarButton, GUILayout.Width(110f));
                if (EditorGUI.EndChangeCheck())
                    SimulateSelectedEmployee();
                EditorGUI.EndDisabledGroup();
                _showContent = GUILayout.Toggle(_showContent, "본문 표시", EditorStyles.toolbarButton, GUILayout.Width(80f));
            }
        }

        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("보고서 생성 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "직원을 선택하면 현재 코드 기준으로 보고서 점수, 등급, 후보 보고서, 주차 스탯 반영값을 미리 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                _roleFilter = (Role)EditorGUILayout.EnumPopup("직군 필터", _roleFilter);

                List<EmployeeSnapshot> filteredEmployees = GetFilteredEmployees();
                if (filteredEmployees.Count == 0)
                {
                    string emptyMessage = _usePlayModeEmployees
                        ? "현재 Play Mode에서 선택한 직군의 고용 직원이 없습니다."
                        : "선택한 직군의 직원 데이터가 없습니다.";
                    EditorGUILayout.HelpBox(emptyMessage, MessageType.Warning);
                    EditorGUI.EndChangeCheck();
                    return;
                }

                _selectedEmployeeIndex = Mathf.Clamp(_selectedEmployeeIndex, 0, filteredEmployees.Count - 1);
                string[] employeeOptions = filteredEmployees
                    .Select(e => $"{e.So.id} / {e.So.Name} / 능력 {e.StatAbility} / 의욕 {e.Desire}")
                    .ToArray();
                _selectedEmployeeIndex = EditorGUILayout.Popup("직원", _selectedEmployeeIndex, employeeOptions);

                _startRepo = EditorGUILayout.Popup("보고서 구분", _startRepo == 1 ? 0 : 1, new[] { "1주차 보고서(startRepo=1)", "랜덤 보고서(startRepo=0)" }) == 0 ? 1 : 0;

                _overrideDesire = EditorGUILayout.Toggle("의욕 임시 변경", _overrideDesire);
                using (new EditorGUI.DisabledScope(!_overrideDesire))
                    _desireOverride = EditorGUILayout.IntSlider("시뮬레이션 의욕", _desireOverride, 0, 100);

                if (_usePlayModeEmployees)
                {
                    EditorGUILayout.HelpBox(
                        "Play Mode 직원 사용 중입니다. 의욕/피로도/충성도/성장 능력치는 현재 플레이 상태에서 읽습니다.",
                        MessageType.Info);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("검색", GUILayout.Width(42f));
                    _searchText = EditorGUILayout.TextField(_searchText);
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_searchText)))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(54f)))
                            _searchText = string.Empty;
                    }
                }

                if (EditorGUI.EndChangeCheck())
                    SimulateSelectedEmployee();
            }
        }

        private void DrawSummary()
        {
            EmployeeSnapshot employee = GetSelectedEmployee();
            if (!employee.IsValid)
                return;

            int desire = GetSimulationDesire(employee);
            float baseScore = PerkPolicy.CalcBaseProperty(employee.ReportScoreAbility);
            float motivationBonus = CalcMotivationBonus(desire);
            float reportScore = CalcReportScore(employee.ReportScoreAbility, desire);
            int grade = ReportPolicy.CalcGrade(reportScore);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("선택 직원 요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"직원: {employee.So.Name} / {employee.So.role} / {employee.So.style}");
                EditorGUILayout.LabelField($"능력 {employee.StatAbility} / 의욕 {desire} / 피로도 {employee.Fatigue} / 충성도 {employee.Loyalty}");
                EditorGUILayout.LabelField($"특성: 대표 {GetTraitLabel(employee.So.mainTrait)} / 보조 {GetTraitLabel(employee.So.subTrait)} / 리스크 {GetTraitLabel(employee.So.riskTrait)}");
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField($"보고서 점수: {baseScore:0.#} + 의욕 보정 {motivationBonus:+0.#;-0.#;0} = {reportScore:0.#}");
                EditorGUILayout.LabelField($"보고서 등급: {grade}등급 ({GetGradeName(grade)})");

                DrawStatPreview(employee, grade);
            }
        }

        private void DrawCandidates()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"후보 보고서 {_candidates.Count}개", EditorStyles.boldLabel);

            foreach (ReportCandidatePreview candidate in _candidates.Where(MatchesSearch))
                DrawCandidate(candidate);

            if (_candidates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "현재 직원 특성/startRepo/grade 조합으로 조회 가능한 보고서가 없습니다. 보고서 테이블의 trait, startRepo, grade를 확인하세요.",
                    MessageType.Warning);
            }
        }

        private void DrawCandidate(ReportCandidatePreview candidate)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{candidate.Report.id} / {candidate.Report.title}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Select", GUILayout.Width(64f)))
                    {
                        Selection.activeObject = candidate.Report;
                        EditorGUIUtility.PingObject(candidate.Report);
                    }
                }

                EditorGUILayout.LabelField($"직군: {candidate.Report.role} / 특성: {GetTraitLabel(candidate.Report.trait)} / grade={candidate.Report.grade} / startRepo={candidate.Report.startRepo}");
                EditorGUILayout.LabelField($"매칭 이유: 직원의 {candidate.MatchSource} 특성과 보고서 특성이 일치");

                if (_showContent && !string.IsNullOrWhiteSpace(candidate.Report.content))
                    EditorGUILayout.LabelField(candidate.Report.content, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawStatPreview(EmployeeSnapshot employee, int grade)
        {
            TraitStat[] stats = ReportPolicy.GetRoleStats(employee.So.role);
            if (stats.Length == 0)
                return;

            int ability = ReportPolicy.CalcLoyaltyAdjustedAbility(employee.StatAbility, employee.Loyalty);
            var scores = new Dictionary<TraitStat, float>
            {
                [stats[0]] = ability,
                [stats[1]] = ability,
                [stats[2]] = ability,
            };

            int mainDelta = grade == 1 ? 12 : grade == 2 ? 8 : 4;
            int subDelta = grade == 1 ? 6 : grade == 2 ? 4 : 2;
            int riskDelta = grade == 1 ? -6 : grade == 2 ? -4 : -2;

            ApplyPreviewDelta(scores, employee.So.mainTrait, mainDelta);
            ApplyPreviewDelta(scores, employee.So.subTrait, subDelta);
            ApplyPreviewDelta(scores, employee.So.riskTrait, riskDelta);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("현재 코드 기준 주차 결과값 미리보기", EditorStyles.boldLabel);

            foreach (TraitStat stat in stats)
            {
                float value = Mathf.Clamp(scores[stat], 0f, 100f);
                EditorGUILayout.LabelField($"{GetStatLabel(stat)}: {value:0.#}");
            }
        }

        private static void ApplyPreviewDelta(Dictionary<TraitStat, float> scores, Trait trait, int delta)
        {
            if (!QATraitUtility.TryGetTraitData(trait, out TraitData data))
                return;

            foreach (TraitStat stat in data.affectedStats)
            {
                if (scores.ContainsKey(stat))
                    scores[stat] += delta;
            }
        }

        private void RefreshAssets()
        {
            _employees.Clear();
            _employees.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot)
                .OrderBy(e => e.Asset.role)
                .ThenBy(e => e.Asset.id));

            _reports.Clear();
            _reports.AddRange(QAAssetUtility.FindAssetEntriesByType<ReportSO>(ReportRoot)
                .OrderBy(r => r.Asset.role)
                .ThenBy(r => r.Asset.trait)
                .ThenBy(r => r.Asset.startRepo)
                .ThenBy(r => r.Asset.grade)
                .ThenBy(r => r.Asset.id));
        }

        private void SimulateSelectedEmployee()
        {
            _candidates.Clear();

            EmployeeSnapshot employee = GetSelectedEmployee();
            if (!employee.IsValid)
                return;

            int desire = GetSimulationDesire(employee);
            int grade = ReportPolicy.CalcGrade(CalcReportScore(employee.ReportScoreAbility, desire));

            AddCandidates(employee, employee.So.mainTrait, "대표", grade);
            AddCandidates(employee, employee.So.riskTrait, "리스크", grade);
            AddCandidates(employee, employee.So.subTrait, "보조", grade);
        }

        private void AddCandidates(EmployeeSnapshot employee, Trait trait, string matchSource, int grade)
        {
            if (trait == Trait.None)
                return;

            foreach (AssetEntry<ReportSO> entry in _reports)
            {
                ReportSO report = entry.Asset;
                if (report.role != employee.So.role)
                    continue;

                if (report.trait != trait)
                    continue;

                if (report.grade != grade)
                    continue;

                if (report.startRepo != _startRepo)
                    continue;

                _candidates.Add(new ReportCandidatePreview(report, matchSource));
            }
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
                .Select(e => EmployeeSnapshot.FromAsset(e.Asset))
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

        private int GetSimulationDesire(EmployeeSnapshot employee)
        {
            return _overrideDesire ? _desireOverride : employee.Desire;
        }

        private bool MatchesSearch(ReportCandidatePreview candidate)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            string keyword = _searchText.Trim();
            string title = candidate.Report.title ?? string.Empty;
            string content = candidate.Report.content ?? string.Empty;

            return title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                || content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                || GetTraitLabel(candidate.Report.trait).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float CalcMotivationBonus(int desire)
        {
            if (desire >= 80)
                return 5f;

            if (desire >= 40)
                return 0f;

            return -10f;
        }

        private static float CalcReportScore(int ability, int desire)
        {
            return PerkPolicy.CalcBaseProperty(ability) + CalcMotivationBonus(desire);
        }

        private static string GetGradeName(int grade)
        {
            return grade switch
            {
                1 => "INNOVATION",
                2 => "STANDARD",
                3 => "SLOPPY",
                _ => "UNKNOWN"
            };
        }

        private static string GetTraitLabel(Trait trait)
        {
            if (trait == Trait.None)
                return "없음";

            return QATraitUtility.TryGetTraitData(trait, out TraitData data)
                ? data.displayName
                : trait.ToString();
        }

        private static string GetStatLabel(TraitStat stat)
        {
            return stat switch
            {
                TraitStat.Fun => "재미",
                TraitStat.Creativity => "창의성",
                TraitStat.Precision => "정교함",
                TraitStat.TechPower => "기술력",
                TraitStat.Optimize => "최적화",
                TraitStat.BugControl => "버그 제어",
                TraitStat.Visual => "비주얼",
                TraitStat.Direction => "연출력",
                TraitStat.Composition => "구도",
                _ => stat.ToString()
            };
        }

        private readonly struct ReportCandidatePreview
        {
            public ReportSO Report { get; }
            public string MatchSource { get; }

            public ReportCandidatePreview(ReportSO report, string matchSource)
            {
                Report = report;
                MatchSource = matchSource;
            }
        }

        private readonly struct EmployeeSnapshot
        {
            public EmployeeImmutableData So { get; }
            public int ReportScoreAbility { get; }
            public int StatAbility { get; }
            public int Desire { get; }
            public int Fatigue { get; }
            public int Loyalty { get; }
            public bool IsValid => So != null;

            private EmployeeSnapshot(
                EmployeeImmutableData so,
                int reportScoreAbility,
                int statAbility,
                int desire,
                int fatigue,
                int loyalty)
            {
                So = so;
                ReportScoreAbility = reportScoreAbility;
                StatAbility = statAbility;
                Desire = desire;
                Fatigue = fatigue;
                Loyalty = loyalty;
            }

            public static EmployeeSnapshot FromAsset(EmployeeImmutableData so)
            {
                return new EmployeeSnapshot(
                    so,
                    so.ability,
                    so.ability,
                    so.desire,
                    so.fatigue,
                    so.loyalty);
            }

            public static EmployeeSnapshot FromRuntime(Employee employee)
            {
                return new EmployeeSnapshot(
                    employee.so,
                    employee.so.ability,
                    employee.MutableData.ability,
                    employee.MutableData.desire,
                    employee.MutableData.fatigue,
                    employee.MutableData.loyalty);
            }
        }
    }
}
