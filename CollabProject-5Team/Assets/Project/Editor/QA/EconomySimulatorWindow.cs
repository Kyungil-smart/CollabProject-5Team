using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class EconomySimulatorWindow : EditorWindow
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const string ProjectRoot = "Assets/Project/DB/Project";

        private readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        private readonly List<AssetEntry<ProjectSO>> _projects = new();
        private readonly List<EconomyWeekSnapshot> _weeks = new();

        private Vector2 _scrollPosition;
        private int _initialGold = 10000;
        private int _simulationWeeks = 12;
        private int _employeeCount = 4;
        private int _manualWeeklySalary;
        private bool _useAverageSalary = true;
        private int _startSmallProjects = 1;
        private int _startMediumProjects;
        private int _startLargeProjects;
        private int _completedSmallProjects;
        private int _completedMediumProjects;
        private int _completedLargeProjects;
        private int _weeklyRecruitCount;
        private int _weeklyTrainingCount;
        private TrainingPlan _trainingPlan = TrainingPlan.None;
        private int _oneTimeOfficeUpgradeCost;
        private int _weeklyOfficeMaintainCost = 500;

        private int _targetSurviveWeeks = 12;
        private int _targetMinGold;
        private int _targetCumulativeNetMin;
        private float _targetCoverageMin = 1f;
        private float _targetCoverageMax = 5f;
        private float _targetHiringRatioMax = 0.35f;
        private float _targetTrainingRatioMax = 0.35f;
        private float _targetProjectCostRatioMax = 0.55f;

        private bool _hasBaseline;
        private EconomySummary _baselineSummary;

        [MenuItem("Tools/Balance/5. Economy Balance", false, 205)]
        public static void Open()
        {
            EconomySimulatorWindow window = GetWindow<EconomySimulatorWindow>("Economy Balance");
            window.minSize = new Vector2(720f, 620f);
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
            DrawTargetValidation();
            DrawBaselineCompare();
            DrawRiskPanel();
            DrawScenarioMatrix();
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

                using (new EditorGUI.DisabledScope(!Application.isPlaying || Company.Instance == null))
                {
                    if (GUILayout.Button("Play Mode 자금 불러오기", EditorStyles.toolbarButton, GUILayout.Width(150f)))
                    {
                        _initialGold = Company.Instance.gold.Value;
                        Simulate();
                    }
                }
            }
        }

        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("재화 밸런스 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "초기 자금, 급여, 프로젝트 개발비, 채용비, 교육비, 사무실 유지비, 서비스 매출을 주차 단위로 합산해 회사가 몇 주 버티는지 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);

            DrawPresetPanel();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                _initialGold = EditorGUILayout.IntField("초기 자금", _initialGold);
                _simulationWeeks = EditorGUILayout.IntSlider("시뮬레이션 주차", _simulationWeeks, 1, 52);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("인건비", EditorStyles.boldLabel);
                _employeeCount = EditorGUILayout.IntSlider("직원 수", _employeeCount, 0, 50);
                _useAverageSalary = EditorGUILayout.Toggle("직원 DB 평균 급여 사용", _useAverageSalary);
                using (new EditorGUI.DisabledScope(_useAverageSalary))
                    _manualWeeklySalary = EditorGUILayout.IntField("직원 1인 주급", _manualWeeklySalary);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("1주차 프로젝트 시작비", EditorStyles.boldLabel);
                _startSmallProjects = EditorGUILayout.IntSlider("소형 시작 수", _startSmallProjects, 0, 5);
                _startMediumProjects = EditorGUILayout.IntSlider("중형 시작 수", _startMediumProjects, 0, 5);
                _startLargeProjects = EditorGUILayout.IntSlider("대형 시작 수", _startLargeProjects, 0, 5);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("서비스 중인 완료 프로젝트 매출", EditorStyles.boldLabel);
                _completedSmallProjects = EditorGUILayout.IntSlider("소형 서비스 수", _completedSmallProjects, 0, 10);
                _completedMediumProjects = EditorGUILayout.IntSlider("중형 서비스 수", _completedMediumProjects, 0, 10);
                _completedLargeProjects = EditorGUILayout.IntSlider("대형 서비스 수", _completedLargeProjects, 0, 10);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("반복 비용", EditorStyles.boldLabel);
                _weeklyRecruitCount = EditorGUILayout.IntSlider("주간 모집 인원", _weeklyRecruitCount, 0, 20);
                _trainingPlan = (TrainingPlan)EditorGUILayout.EnumPopup("교육 종류", _trainingPlan);
                _weeklyTrainingCount = EditorGUILayout.IntSlider("주간 교육 인원", _weeklyTrainingCount, 0, 20);
                _weeklyOfficeMaintainCost = EditorGUILayout.IntField("주간 사무실 유지비", _weeklyOfficeMaintainCost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("1주차 일회성 비용", EditorStyles.boldLabel);
                _oneTimeOfficeUpgradeCost = EditorGUILayout.IntField("사무실 증축비", _oneTimeOfficeUpgradeCost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("목표 범위", EditorStyles.boldLabel);
                _targetSurviveWeeks = EditorGUILayout.IntSlider("최소 생존 주차", _targetSurviveWeeks, 1, 52);
                _targetMinGold = EditorGUILayout.IntField("최소 보유 자금", _targetMinGold);
                _targetCumulativeNetMin = EditorGUILayout.IntField("최소 누적 순이익", _targetCumulativeNetMin);
                _targetCoverageMin = EditorGUILayout.FloatField("수입/지출 최소 배율", _targetCoverageMin);
                _targetCoverageMax = EditorGUILayout.FloatField("수입/지출 최대 배율", _targetCoverageMax);
                _targetHiringRatioMax = EditorGUILayout.Slider("채용비 최대 비중", _targetHiringRatioMax, 0f, 1f);
                _targetTrainingRatioMax = EditorGUILayout.Slider("교육비 최대 비중", _targetTrainingRatioMax, 0f, 1f);
                _targetProjectCostRatioMax = EditorGUILayout.Slider("프로젝트 시작비 최대 비중", _targetProjectCostRatioMax, 0f, 1f);

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }
        }

        private void DrawPresetPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("대표 운영 시나리오", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("버튼을 눌러 초기 운영, 성장 전환, 대형 도전처럼 자주 확인할 케이스를 빠르게 세팅합니다.", EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawScenarioButton("소형 반복", 10000, 12, 4, 1, 0, 0, 1, 0, 0, 0, TrainingPlan.None, 0, 500, 0);
                    DrawScenarioButton("소형→중형", 18000, 16, 6, 1, 1, 0, 1, 1, 0, 1, TrainingPlan.Basic, 1, 1200, 3000);
                    DrawScenarioButton("중형 중심", 30000, 20, 8, 0, 2, 0, 1, 2, 0, 1, TrainingPlan.Professional, 2, 1800, 8000);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawScenarioButton("대형 도전", 55000, 24, 12, 0, 1, 1, 1, 2, 1, 1, TrainingPlan.Intensive, 2, 4000, 15000);
                    DrawScenarioButton("채용 공격", 25000, 16, 10, 1, 1, 0, 1, 1, 0, 3, TrainingPlan.Basic, 1, 1500, 5000);
                    DrawScenarioButton("교육 성장", 22000, 16, 6, 1, 0, 0, 1, 1, 0, 0, TrainingPlan.Intensive, 3, 1500, 3000);
                }
            }
        }

        private void DrawScenarioButton(
            string label,
            int initialGold,
            int weeks,
            int employeeCount,
            int startSmall,
            int startMedium,
            int startLarge,
            int completedSmall,
            int completedMedium,
            int completedLarge,
            int recruitCount,
            TrainingPlan trainingPlan,
            int trainingCount,
            int officeMaintainCost,
            int officeUpgradeCost)
        {
            if (!GUILayout.Button(label))
                return;

            _initialGold = initialGold;
            _simulationWeeks = weeks;
            _employeeCount = employeeCount;
            _startSmallProjects = startSmall;
            _startMediumProjects = startMedium;
            _startLargeProjects = startLarge;
            _completedSmallProjects = completedSmall;
            _completedMediumProjects = completedMedium;
            _completedLargeProjects = completedLarge;
            _weeklyRecruitCount = recruitCount;
            _trainingPlan = trainingPlan;
            _weeklyTrainingCount = trainingCount;
            _weeklyOfficeMaintainCost = officeMaintainCost;
            _oneTimeOfficeUpgradeCost = officeUpgradeCost;
            Simulate();
        }

        private void DrawSummary()
        {
            if (_weeks.Count == 0)
                return;

            EconomySummary summary = BuildSummary(_weeks);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"마지막 자금: {summary.FinalGold:N0}G");
                EditorGUILayout.LabelField($"최저 자금: {summary.MinGold:N0}G");
                EditorGUILayout.LabelField($"누적 수입: {summary.TotalIncome:N0}G");
                EditorGUILayout.LabelField($"누적 지출: {summary.TotalExpense:N0}G");
                EditorGUILayout.LabelField($"누적 순이익: {summary.TotalNetGold:N0}G");
                EditorGUILayout.LabelField($"수입/지출 배율: {summary.Coverage:0.##}배");
                EditorGUILayout.LabelField($"주간 평균 순이익: {summary.AverageNetGold:N0}G");
                EditorGUILayout.LabelField($"직원 1인 주급 기준: {GetWeeklySalaryPerEmployee():N0}G");

                if (summary.FirstDeficitWeek > 0)
                    EditorGUILayout.HelpBox($"{summary.FirstDeficitWeek}주차에 자금이 음수가 됩니다.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("현재 조건에서는 시뮬레이션 기간 동안 자금이 음수가 되지 않습니다.", MessageType.Info);
            }
        }

        private void DrawTargetValidation()
        {
            if (_weeks.Count == 0)
                return;

            EconomySummary summary = BuildSummary(_weeks);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("목표 범위 체크", EditorStyles.boldLabel);
                DrawTargetLine("생존 주차", summary.SurvivedWeeks, _targetSurviveWeeks, int.MaxValue, "주");
                DrawTargetLine("최저 자금", summary.MinGold, _targetMinGold, int.MaxValue, "G");
                DrawTargetLine("누적 순이익", summary.TotalNetGold, _targetCumulativeNetMin, int.MaxValue, "G");
                DrawTargetLine("수입/지출 배율", summary.Coverage, _targetCoverageMin, _targetCoverageMax, "배");
                DrawTargetLine("채용비 비중", summary.HiringExpenseRatio, 0f, _targetHiringRatioMax, "");
                DrawTargetLine("교육비 비중", summary.TrainingExpenseRatio, 0f, _targetTrainingRatioMax, "");
                DrawTargetLine("프로젝트 시작비 비중", summary.ProjectStartExpenseRatio, 0f, _targetProjectCostRatioMax, "");
            }
        }

        private void DrawTargetLine(string label, int value, int min, int max, string suffix)
        {
            bool passed = value >= min && value <= max;
            string maxLabel = max == int.MaxValue ? "∞" : max.ToString("N0");
            EditorGUILayout.LabelField(
                $"{(passed ? "OK" : "NG")} {label}: {value:N0}{suffix} / 목표 {min:N0}~{maxLabel}{suffix}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private void DrawTargetLine(string label, float value, float min, float max, string suffix)
        {
            bool passed = value >= min && value <= max;
            EditorGUILayout.LabelField(
                $"{(passed ? "OK" : "NG")} {label}: {value:0.##}{suffix} / 목표 {min:0.##}~{max:0.##}{suffix}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private void DrawBaselineCompare()
        {
            if (_weeks.Count == 0)
                return;

            EconomySummary current = BuildSummary(_weeks);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("기준값 비교", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Width(180f)))
                    {
                        _baselineSummary = current;
                        _hasBaseline = true;
                    }

                    using (new EditorGUI.DisabledScope(!_hasBaseline))
                    {
                        if (GUILayout.Button("기준값 지우기", GUILayout.Width(110f)))
                            _hasBaseline = false;
                    }
                }

                if (!_hasBaseline)
                {
                    EditorGUILayout.LabelField("기준값을 저장하면 수치 조정 전후 차이를 바로 비교할 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                EditorGUILayout.LabelField(BuildDeltaText("마지막 자금", current.FinalGold, _baselineSummary.FinalGold), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(BuildDeltaText("누적 순이익", current.TotalNetGold, _baselineSummary.TotalNetGold), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(BuildDeltaText("수입/지출 배율", current.Coverage, _baselineSummary.Coverage), EditorStyles.miniLabel);
                EditorGUILayout.LabelField(BuildDeltaText("첫 적자 주차", current.FirstDeficitWeek, _baselineSummary.FirstDeficitWeek), EditorStyles.miniLabel);
            }
        }

        private static string BuildDeltaText(string label, int current, int baseline)
        {
            int delta = current - baseline;
            return $"{label}: 현재 {current:N0} / 기준 {baseline:N0} / 차이 {delta:+#,0;-#,0;0}";
        }

        private static string BuildDeltaText(string label, float current, float baseline)
        {
            float delta = current - baseline;
            return $"{label}: 현재 {current:0.##} / 기준 {baseline:0.##} / 차이 {delta:+0.##;-0.##;0}";
        }

        private void DrawRiskPanel()
        {
            if (_weeks.Count == 0)
                return;

            EconomySummary summary = BuildSummary(_weeks);
            List<string> risks = BuildRiskMessages(summary);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("리스크 신호", EditorStyles.boldLabel);

                if (risks.Count == 0)
                {
                    EditorGUILayout.HelpBox("현재 입력값에서는 큰 재화 리스크가 감지되지 않았습니다.", MessageType.Info);
                    return;
                }

                foreach (string risk in risks)
                    EditorGUILayout.HelpBox(risk, MessageType.Warning);
            }
        }

        private List<string> BuildRiskMessages(EconomySummary summary)
        {
            List<string> risks = new();

            if (summary.FirstDeficitWeek > 0 && summary.FirstDeficitWeek <= Mathf.Min(4, _simulationWeeks))
                risks.Add("초반 4주 안에 자금이 음수가 됩니다. 시작 자금, 프로젝트 시작비, 급여 중 하나가 너무 빡빡할 수 있습니다.");

            if (summary.Coverage < 1f)
                risks.Add("누적 수입보다 누적 지출이 큽니다. 완료 프로젝트 매출이나 유지비/급여 밸런스를 확인하세요.");

            if (summary.Coverage > 8f && summary.TotalIncome > 0)
                risks.Add("수입/지출 배율이 매우 높습니다. 운영 압박이 거의 없어져 재화 선택의 의미가 약해질 수 있습니다.");

            if (summary.HiringExpenseRatio > _targetHiringRatioMax)
                risks.Add("채용비 비중이 높습니다. 공격 채용 전략이 지나치게 벌칙처럼 느껴질 수 있습니다.");

            if (summary.TrainingExpenseRatio > _targetTrainingRatioMax)
                risks.Add("교육비 비중이 높습니다. 성장 루트가 손해처럼 보일 수 있습니다.");

            if (summary.ProjectStartExpenseRatio > _targetProjectCostRatioMax)
                risks.Add("프로젝트 시작비 비중이 높습니다. 중형/대형 진입이 너무 늦어질 수 있습니다.");

            if (_completedSmallProjects > 0 && _completedMediumProjects == 0 && _completedLargeProjects == 0 && summary.Coverage > 4f)
                risks.Add("소형 프로젝트만으로도 운영이 매우 안정적입니다. 규모 확장의 동기가 약해질 수 있습니다.");

            if (_startLargeProjects > 0 && summary.FirstDeficitWeek > 0)
                risks.Add("대형 프로젝트 시작 후 적자가 발생합니다. 대형 도전 비용이나 완료 보상을 함께 확인하는 것이 좋습니다.");

            return risks;
        }

        private void DrawScenarioMatrix()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("대표 운영 케이스 매트릭스", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("주요 운영 패턴을 같은 기준으로 돌려서 어떤 전략이 과하게 쉽거나 막히는지 비교합니다.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("케이스 / 마지막 자금 / 첫 적자 / 수입지출 / 순이익 / 판정", EditorStyles.miniLabel);

                DrawScenarioMatrixRow(new EconomyScenario("소형 반복", 10000, 12, 4, 1, 0, 0, 1, 0, 0, 0, TrainingPlan.None, 0, 500, 0));
                DrawScenarioMatrixRow(new EconomyScenario("소형→중형", 18000, 16, 6, 1, 1, 0, 1, 1, 0, 1, TrainingPlan.Basic, 1, 1200, 3000));
                DrawScenarioMatrixRow(new EconomyScenario("중형 중심", 30000, 20, 8, 0, 2, 0, 1, 2, 0, 1, TrainingPlan.Professional, 2, 1800, 8000));
                DrawScenarioMatrixRow(new EconomyScenario("대형 도전", 55000, 24, 12, 0, 1, 1, 1, 2, 1, 1, TrainingPlan.Intensive, 2, 4000, 15000));
                DrawScenarioMatrixRow(new EconomyScenario("채용 공격", 25000, 16, 10, 1, 1, 0, 1, 1, 0, 3, TrainingPlan.Basic, 1, 1500, 5000));
                DrawScenarioMatrixRow(new EconomyScenario("교육 성장", 22000, 16, 6, 1, 0, 0, 1, 1, 0, 0, TrainingPlan.Intensive, 3, 1500, 3000));
            }
        }

        private void DrawScenarioMatrixRow(EconomyScenario scenario)
        {
            EconomySummary summary = BuildSummary(SimulateScenario(scenario));
            bool passed = summary.FirstDeficitWeek == 0 && summary.Coverage >= _targetCoverageMin && summary.Coverage <= _targetCoverageMax;
            string deficit = summary.FirstDeficitWeek == 0 ? "없음" : $"{summary.FirstDeficitWeek}주";

            EditorGUILayout.LabelField(
                $"{scenario.Name} / {summary.FinalGold:N0}G / {deficit} / {summary.Coverage:0.##}배 / {summary.TotalNetGold:N0}G / {(passed ? "OK" : "확인")}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private void DrawTimeline()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("주차별 자금 흐름", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Week / Start / Income / Expense / Net / End", EditorStyles.miniLabel);

                foreach (EconomyWeekSnapshot week in _weeks)
                {
                    EditorGUILayout.LabelField(
                        $"{week.Week,2}주차  시작 {week.StartGold,8:N0}G  수입 {week.Income,8:N0}G  지출 {week.Expense,8:N0}G  순 {week.NetGold,8:N0}G  종료 {week.EndGold,8:N0}G",
                        week.EndGold < 0 ? EditorStyles.boldLabel : EditorStyles.miniLabel);

                    EditorGUILayout.LabelField(
                        $"       지출 상세: 급여 {week.SalaryExpense:N0} / 서비스 {week.ServiceExpense:N0} / 시작비 {week.ProjectStartExpense:N0} / 채용 {week.RecruitExpense:N0} / 교육 {week.TrainingExpense:N0} / 사무실 {week.OfficeExpense + week.OneTimeExpense:N0}",
                        EditorStyles.miniLabel);
                }
            }
        }

        private void Simulate()
        {
            _weeks.Clear();
            _weeks.AddRange(SimulateScenario(new EconomyScenario(
                "현재 입력값",
                _initialGold,
                _simulationWeeks,
                _employeeCount,
                _startSmallProjects,
                _startMediumProjects,
                _startLargeProjects,
                _completedSmallProjects,
                _completedMediumProjects,
                _completedLargeProjects,
                _weeklyRecruitCount,
                _trainingPlan,
                _weeklyTrainingCount,
                _weeklyOfficeMaintainCost,
                _oneTimeOfficeUpgradeCost)));
        }

        private List<EconomyWeekSnapshot> SimulateScenario(EconomyScenario scenario)
        {
            List<EconomyWeekSnapshot> weeks = new();
            int gold = scenario.InitialGold;
            float smallRetention = 1f;
            float mediumRetention = 1f;
            float largeRetention = 1f;

            for (int week = 1; week <= scenario.Weeks; week++)
            {
                int startGold = gold;
                int income = CalcWeeklyCompletedProjectIncome(ProjectSize.Small, scenario.CompletedSmallProjects, smallRetention)
                           + CalcWeeklyCompletedProjectIncome(ProjectSize.Medium, scenario.CompletedMediumProjects, mediumRetention)
                           + CalcWeeklyCompletedProjectIncome(ProjectSize.Large, scenario.CompletedLargeProjects, largeRetention);

                int salaryExpense = scenario.EmployeeCount * GetWeeklySalaryPerEmployee();
                int serviceExpense = scenario.CompletedSmallProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Small)
                                   + scenario.CompletedMediumProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Medium)
                                   + scenario.CompletedLargeProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Large);
                int recruitExpense = scenario.WeeklyRecruitCount * 1000;
                int trainingExpense = scenario.WeeklyTrainingCount * GetTrainingCost(scenario.TrainingPlan);
                int officeExpense = Mathf.Max(0, scenario.WeeklyOfficeMaintainCost);
                int projectStartExpense = 0;
                int oneTimeExpense = 0;

                if (week == 1)
                {
                    projectStartExpense += scenario.StartSmallProjects * GetProjectRequiredCost(ProjectSize.Small);
                    projectStartExpense += scenario.StartMediumProjects * GetProjectRequiredCost(ProjectSize.Medium);
                    projectStartExpense += scenario.StartLargeProjects * GetProjectRequiredCost(ProjectSize.Large);
                    oneTimeExpense += Mathf.Max(0, scenario.OneTimeOfficeUpgradeCost);
                }

                int expense = salaryExpense + serviceExpense + recruitExpense + trainingExpense + officeExpense + projectStartExpense + oneTimeExpense;
                int netGold = income - expense;
                gold += netGold;

                weeks.Add(new EconomyWeekSnapshot(
                    week,
                    startGold,
                    income,
                    expense,
                    netGold,
                    gold,
                    salaryExpense,
                    serviceExpense,
                    projectStartExpense,
                    recruitExpense,
                    trainingExpense,
                    officeExpense,
                    oneTimeExpense));

                smallRetention = Mathf.Clamp01(smallRetention - PerkPolicy.RETENTION_DECAY * 5f);
                mediumRetention = Mathf.Clamp01(mediumRetention - PerkPolicy.RETENTION_DECAY * 5f);
                largeRetention = Mathf.Clamp01(largeRetention - PerkPolicy.RETENTION_DECAY * 5f);
            }

            return weeks;
        }

        private EconomySummary BuildSummary(IReadOnlyList<EconomyWeekSnapshot> weeks)
        {
            if (weeks == null || weeks.Count == 0)
                return EconomySummary.Empty;

            int totalIncome = weeks.Sum(w => w.Income);
            int totalExpense = weeks.Sum(w => w.Expense);
            int totalNetGold = weeks.Sum(w => w.NetGold);
            int firstDeficitWeek = weeks.FirstOrDefault(w => w.EndGold < 0).Week;
            int survivedWeeks = firstDeficitWeek > 0 ? Mathf.Max(0, firstDeficitWeek - 1) : weeks.Count;
            int minGold = weeks.Min(w => w.EndGold);
            int finalGold = weeks[weeks.Count - 1].EndGold;
            float averageNetGold = (float)weeks.Average(w => w.NetGold);
            float coverage = totalExpense > 0 ? totalIncome / (float)totalExpense : 0f;
            int hiringExpense = weeks.Sum(w => w.RecruitExpense);
            int trainingExpense = weeks.Sum(w => w.TrainingExpense);
            int projectStartExpense = weeks.Sum(w => w.ProjectStartExpense);
            float hiringRatio = totalExpense > 0 ? hiringExpense / (float)totalExpense : 0f;
            float trainingRatio = totalExpense > 0 ? trainingExpense / (float)totalExpense : 0f;
            float projectStartRatio = totalExpense > 0 ? projectStartExpense / (float)totalExpense : 0f;

            return new EconomySummary(
                finalGold,
                minGold,
                totalIncome,
                totalExpense,
                totalNetGold,
                averageNetGold,
                coverage,
                firstDeficitWeek,
                survivedWeeks,
                hiringRatio,
                trainingRatio,
                projectStartRatio);
        }

        private void RefreshAssets()
        {
            _employees.Clear();
            _employees.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot)
                .OrderBy(e => e.Asset.id));

            _projects.Clear();
            _projects.AddRange(QAAssetUtility.FindAssetEntriesByType<ProjectSO>(ProjectRoot)
                .OrderBy(e => e.Asset.scale));
        }

        private int GetWeeklySalaryPerEmployee()
        {
            if (!_useAverageSalary)
                return Mathf.Max(0, _manualWeeklySalary);

            if (_employees.Count == 0)
                return 0;

            return Mathf.RoundToInt((float)_employees.Average(e => e.Asset.weekSalary));
        }

        private static int CalcWeeklyCompletedProjectIncome(ProjectSize size, int count, float retention)
        {
            if (count <= 0)
                return 0;

            int weeklyGold = 0;
            float currentRetention = retention;

            for (int day = 0; day < 5; day++)
            {
                int dailySales = PerkPolicy.CalcDailySales(size, 70f, 70f, 70f, currentRetention, 0);
                weeklyGold += PerkPolicy.CalcDailyGold(size, dailySales);
                currentRetention = Mathf.Clamp01(currentRetention - PerkPolicy.RETENTION_DECAY);
            }

            return weeklyGold * count;
        }

        private int GetProjectRequiredCost(ProjectSize size)
        {
            ProjectSO project = _projects
                .Select(e => e.Asset)
                .FirstOrDefault(p => p != null && p.scale == size);

            if (project != null)
                return project.requiredCost;

            return size switch
            {
                ProjectSize.Medium => 5000,
                ProjectSize.Large => 12000,
                _ => 1000
            };
        }

        private static int GetTrainingCost(TrainingPlan plan)
        {
            return plan switch
            {
                TrainingPlan.Basic => 1000,
                TrainingPlan.Professional => 3000,
                TrainingPlan.Intensive => 5000,
                _ => 0
            };
        }

        private readonly struct EconomyScenario
        {
            public string Name { get; }
            public int InitialGold { get; }
            public int Weeks { get; }
            public int EmployeeCount { get; }
            public int StartSmallProjects { get; }
            public int StartMediumProjects { get; }
            public int StartLargeProjects { get; }
            public int CompletedSmallProjects { get; }
            public int CompletedMediumProjects { get; }
            public int CompletedLargeProjects { get; }
            public int WeeklyRecruitCount { get; }
            public TrainingPlan TrainingPlan { get; }
            public int WeeklyTrainingCount { get; }
            public int WeeklyOfficeMaintainCost { get; }
            public int OneTimeOfficeUpgradeCost { get; }

            public EconomyScenario(
                string name,
                int initialGold,
                int weeks,
                int employeeCount,
                int startSmallProjects,
                int startMediumProjects,
                int startLargeProjects,
                int completedSmallProjects,
                int completedMediumProjects,
                int completedLargeProjects,
                int weeklyRecruitCount,
                TrainingPlan trainingPlan,
                int weeklyTrainingCount,
                int weeklyOfficeMaintainCost,
                int oneTimeOfficeUpgradeCost)
            {
                Name = name;
                InitialGold = initialGold;
                Weeks = weeks;
                EmployeeCount = employeeCount;
                StartSmallProjects = startSmallProjects;
                StartMediumProjects = startMediumProjects;
                StartLargeProjects = startLargeProjects;
                CompletedSmallProjects = completedSmallProjects;
                CompletedMediumProjects = completedMediumProjects;
                CompletedLargeProjects = completedLargeProjects;
                WeeklyRecruitCount = weeklyRecruitCount;
                TrainingPlan = trainingPlan;
                WeeklyTrainingCount = weeklyTrainingCount;
                WeeklyOfficeMaintainCost = weeklyOfficeMaintainCost;
                OneTimeOfficeUpgradeCost = oneTimeOfficeUpgradeCost;
            }
        }

        private readonly struct EconomyWeekSnapshot
        {
            public int Week { get; }
            public int StartGold { get; }
            public int Income { get; }
            public int Expense { get; }
            public int NetGold { get; }
            public int EndGold { get; }
            public int SalaryExpense { get; }
            public int ServiceExpense { get; }
            public int ProjectStartExpense { get; }
            public int RecruitExpense { get; }
            public int TrainingExpense { get; }
            public int OfficeExpense { get; }
            public int OneTimeExpense { get; }

            public EconomyWeekSnapshot(
                int week,
                int startGold,
                int income,
                int expense,
                int netGold,
                int endGold,
                int salaryExpense,
                int serviceExpense,
                int projectStartExpense,
                int recruitExpense,
                int trainingExpense,
                int officeExpense,
                int oneTimeExpense)
            {
                Week = week;
                StartGold = startGold;
                Income = income;
                Expense = expense;
                NetGold = netGold;
                EndGold = endGold;
                SalaryExpense = salaryExpense;
                ServiceExpense = serviceExpense;
                ProjectStartExpense = projectStartExpense;
                RecruitExpense = recruitExpense;
                TrainingExpense = trainingExpense;
                OfficeExpense = officeExpense;
                OneTimeExpense = oneTimeExpense;
            }
        }

        private readonly struct EconomySummary
        {
            public static EconomySummary Empty => new(0, 0, 0, 0, 0, 0f, 0f, 0, 0, 0f, 0f, 0f);

            public int FinalGold { get; }
            public int MinGold { get; }
            public int TotalIncome { get; }
            public int TotalExpense { get; }
            public int TotalNetGold { get; }
            public float AverageNetGold { get; }
            public float Coverage { get; }
            public int FirstDeficitWeek { get; }
            public int SurvivedWeeks { get; }
            public float HiringExpenseRatio { get; }
            public float TrainingExpenseRatio { get; }
            public float ProjectStartExpenseRatio { get; }

            public EconomySummary(
                int finalGold,
                int minGold,
                int totalIncome,
                int totalExpense,
                int totalNetGold,
                float averageNetGold,
                float coverage,
                int firstDeficitWeek,
                int survivedWeeks,
                float hiringExpenseRatio,
                float trainingExpenseRatio,
                float projectStartExpenseRatio)
            {
                FinalGold = finalGold;
                MinGold = minGold;
                TotalIncome = totalIncome;
                TotalExpense = totalExpense;
                TotalNetGold = totalNetGold;
                AverageNetGold = averageNetGold;
                Coverage = coverage;
                FirstDeficitWeek = firstDeficitWeek;
                SurvivedWeeks = survivedWeeks;
                HiringExpenseRatio = hiringExpenseRatio;
                TrainingExpenseRatio = trainingExpenseRatio;
                ProjectStartExpenseRatio = projectStartExpenseRatio;
            }
        }

        private enum TrainingPlan
        {
            None,
            Basic,
            Professional,
            Intensive
        }
    }
}
