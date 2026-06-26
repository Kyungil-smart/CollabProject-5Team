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

        [MenuItem("Tools/Simulation/Economy Simulator")]
        public static void Open()
        {
            EconomySimulatorWindow window = GetWindow<EconomySimulatorWindow>("Economy Simulator");
            window.minSize = new Vector2(640f, 540f);
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

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }
        }

        private void DrawSummary()
        {
            if (_weeks.Count == 0)
                return;

            EconomyWeekSnapshot last = _weeks[_weeks.Count - 1];
            EconomyWeekSnapshot firstDeficit = _weeks.FirstOrDefault(w => w.EndGold < 0);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"마지막 자금: {last.EndGold:N0}G");
                EditorGUILayout.LabelField($"주간 평균 순이익: {_weeks.Average(w => w.NetGold):N0}G");
                EditorGUILayout.LabelField($"직원 1인 주급 기준: {GetWeeklySalaryPerEmployee():N0}G");

                if (firstDeficit.Week > 0)
                    EditorGUILayout.HelpBox($"{firstDeficit.Week}주차에 자금이 음수가 됩니다.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("현재 조건에서는 시뮬레이션 기간 동안 자금이 음수가 되지 않습니다.", MessageType.Info);
            }
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
                }
            }
        }

        private void Simulate()
        {
            _weeks.Clear();

            int gold = _initialGold;
            float smallRetention = 1f;
            float mediumRetention = 1f;
            float largeRetention = 1f;

            for (int week = 1; week <= _simulationWeeks; week++)
            {
                int startGold = gold;
                int income = CalcWeeklyCompletedProjectIncome(ProjectSize.Small, _completedSmallProjects, smallRetention)
                           + CalcWeeklyCompletedProjectIncome(ProjectSize.Medium, _completedMediumProjects, mediumRetention)
                           + CalcWeeklyCompletedProjectIncome(ProjectSize.Large, _completedLargeProjects, largeRetention);

                int expense = 0;
                expense += _employeeCount * GetWeeklySalaryPerEmployee();
                expense += _completedSmallProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Small);
                expense += _completedMediumProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Medium);
                expense += _completedLargeProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Large);
                expense += _weeklyRecruitCount * 1000;
                expense += _weeklyTrainingCount * GetTrainingCost(_trainingPlan);
                expense += Mathf.Max(0, _weeklyOfficeMaintainCost);

                if (week == 1)
                {
                    expense += _startSmallProjects * GetProjectRequiredCost(ProjectSize.Small);
                    expense += _startMediumProjects * GetProjectRequiredCost(ProjectSize.Medium);
                    expense += _startLargeProjects * GetProjectRequiredCost(ProjectSize.Large);
                    expense += Mathf.Max(0, _oneTimeOfficeUpgradeCost);
                }

                int netGold = income - expense;
                gold += netGold;

                _weeks.Add(new EconomyWeekSnapshot(week, startGold, income, expense, netGold, gold));

                smallRetention = Mathf.Clamp01(smallRetention - PerkPolicy.RETENTION_DECAY);
                mediumRetention = Mathf.Clamp01(mediumRetention - PerkPolicy.RETENTION_DECAY);
                largeRetention = Mathf.Clamp01(largeRetention - PerkPolicy.RETENTION_DECAY);
            }
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

            int dailySales = PerkPolicy.CalcDailySales(size, 70f, 70f, 70f, retention, 0);
            int dailyGold = PerkPolicy.CalcDailyGold(size, dailySales);
            return dailyGold * 5 * count;
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

        private readonly struct EconomyWeekSnapshot
        {
            public int Week { get; }
            public int StartGold { get; }
            public int Income { get; }
            public int Expense { get; }
            public int NetGold { get; }
            public int EndGold { get; }

            public EconomyWeekSnapshot(int week, int startGold, int income, int expense, int netGold, int endGold)
            {
                Week = week;
                StartGold = startGold;
                Income = income;
                Expense = expense;
                NetGold = netGold;
                EndGold = endGold;
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
