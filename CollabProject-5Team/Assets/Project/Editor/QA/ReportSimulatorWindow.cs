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
        private bool _showCandidates = true;
        private bool _hasBaseline;
        private float _baselineReportScore;
        private int _baselineGrade;
        private int _baselineCandidateCount;

        [MenuItem("Tools/Balance/3. Report Generation", false, 203)]
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
            DrawSummary();
            DrawReportGuide();
            DrawInputPanel();
            DrawCandidatesPanel();
            EditorGUILayout.EndScrollView();
        }


        private static void DrawReportGuide()
        {
            BalanceGuideUI.Draw(
                "보고서 생성 가이드",
                "직군 필터\n직원 선택\n보고서 구분\n의욕 임시 변경",
                "보고서 점수/등급\n후보 보고서 개수\n대표/보조/리스크 특성 매칭\n스탯 반영 미리보기",
                "후보 보고서 0개\n특정 특성만 후보가 과하게 많음\n의욕 변화에도 등급 변화가 없음");
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
            EditorGUILayout.LabelField("보고서 생성 조건", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "직원 데이터와 보고서 테이블 조건을 맞춰 보고서 후보가 어떻게 생성되는지 확인합니다. 수식 자체가 아니라 생성 조건을 검증하는 탭입니다.",
                EditorStyles.wordWrappedMiniLabel);
            BalanceGuideUI.DrawDataFlow(
                "작성자 직군과 보고서 시점은 이 창에서 선택합니다. 직원 능력치/특성은 Employee SO, 보고서 후보는 Report SO에서 조회합니다.",
                "의욕 임시 변경은 계산에만 적용되고 원본 직원 데이터는 바뀌지 않습니다.",
                "작성자 능력/의욕/특성 -> 보고서 점수/등급 -> 조건에 맞는 보고서 후보 조회 -> 주차 결과값 미리보기");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("1. 작성자 조건", EditorStyles.boldLabel);
                _roleFilter = (Role)EditorGUILayout.EnumPopup("작성자 직군", _roleFilter);

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
                _selectedEmployeeIndex = EditorGUILayout.Popup("작성자", _selectedEmployeeIndex, employeeOptions);
                EditorGUILayout.LabelField("작성자의 직군, 특성, 능력, 의욕으로 조회 가능한 보고서 후보가 결정됩니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("2. 보고서 테이블 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _startRepo = EditorGUILayout.Popup("보고서 시점", _startRepo == 1 ? 0 : 1, new[] { "프로젝트 1주차", "진행 중 랜덤" }) == 0 ? 1 : 0;
                    _overrideDesire = EditorGUILayout.Toggle("의욕 임시 변경", _overrideDesire, GUILayout.Width(150f));
                }
                using (new EditorGUI.DisabledScope(!_overrideDesire))
                    _desireOverride = EditorGUILayout.IntSlider("임시 의욕", _desireOverride, 0, 100);
                EditorGUILayout.LabelField("의욕은 보고서 점수/등급 보정에만 임시 적용됩니다. 원본 직원 데이터는 수정하지 않습니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("3. 후보 필터", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("제목/본문 검색", GUILayout.Width(90f));
                    _searchText = EditorGUILayout.TextField(_searchText);
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_searchText)))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(54f)))
                            _searchText = string.Empty;
                    }
                }

                if (_usePlayModeEmployees)
                {
                    EditorGUILayout.HelpBox(
                        "Play Mode 직원 사용 중입니다. 의욕/피로도/충성도/성장 능력치는 현재 플레이 상태에서 읽습니다.",
                        MessageType.Info);
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
                BalanceGuideUI.DrawFormulaNotice("보고서 점수와 등급은 작성자 능력/의욕과 현재 코드 공식으로 계산됩니다.");
                DrawReportBaselineControls(reportScore, grade, _candidates.Count);
                DrawReportAutoChecks(grade, _candidates.Count);
                DrawReportInterpretation(reportScore, grade, _candidates.Count);

                DrawStatPreview(employee, grade);
            }
        }


        private void DrawReportBaselineControls(float reportScore, int grade, int candidateCount)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("변경 전후 비교", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(24f)))
                    {
                        _baselineReportScore = reportScore;
                        _baselineGrade = grade;
                        _baselineCandidateCount = candidateCount;
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
                    EditorGUILayout.LabelField(BuildDeltaText("보고서 점수", reportScore, _baselineReportScore), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"보고서 등급: 현재 {grade}등급 / 기준 {_baselineGrade}등급 / 차이 {_baselineGrade - grade:+0;-0;0}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"후보 개수: 현재 {candidateCount}개 / 기준 {_baselineCandidateCount}개 / 차이 {candidateCount - _baselineCandidateCount:+0;-0;0}", EditorStyles.miniLabel);
                }
            }
        }

        private static void DrawReportAutoChecks(int grade, int candidateCount)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 감지", EditorStyles.boldLabel);
                BalanceGuideUI.DrawAutoCheck("후보 보고서", candidateCount == 0, candidateCount == 0 ? "현재 조건으로 조회되는 보고서가 없습니다." : $"후보 {candidateCount}개가 조회됩니다.");
                BalanceGuideUI.DrawAutoCheck("보고서 등급", grade >= 3, grade >= 3 ? "낮은 등급입니다. 능력/의욕/특성 조건을 확인하세요." : $"{grade}등급 보고서입니다.");
            }
        }

        private static string BuildDeltaText(string label, float current, float baseline)
        {
            float delta = current - baseline;
            return $"{label}: 현재 {current:0.#} / 기준 {baseline:0.#} / 차이 {delta:+0.#;-0.#;0}";
        }

        private static void DrawReportInterpretation(float reportScore, int grade, int candidateCount)
        {
            if (candidateCount == 0)
            {
                BalanceGuideUI.DrawInterpretation("현재 조건으로 조회되는 보고서 후보가 없습니다. 보고서 테이블의 직군/특성/시점/등급 조건을 확인하세요.", MessageType.Warning);
                return;
            }

            if (grade == 1)
                BalanceGuideUI.DrawInterpretation("상위 보고서가 생성되는 조건입니다. 후보 수가 과하게 적거나 특정 특성에 몰리는지만 확인하세요.");
            else if (grade == 2)
                BalanceGuideUI.DrawInterpretation("표준 보고서 조건입니다. 의욕 보정이나 능력치 변화에 따른 등급 변화가 자연스러운지 확인하세요.");
            else
                BalanceGuideUI.DrawInterpretation($"보고서 점수 {reportScore:0.#}로 낮은 등급입니다. 낮은 의욕/능력치 구간의 리스크 표현을 확인하세요.", MessageType.Warning);
        }

        private void DrawCandidatesPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showCandidates = EditorGUILayout.Foldout(_showCandidates, $"후보 보고서 {_candidates.Count}개", true);
                if (_showCandidates)
                    DrawCandidates();
                else
                    EditorGUILayout.LabelField("후보 내용은 필요할 때 펼쳐서 확인합니다.", EditorStyles.wordWrappedMiniLabel);
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
