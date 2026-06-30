using System;
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
        private int _simulationWeeks = 52;
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

        private bool _useLevelBalanceOverrides = true;
        private bool _showLevelBalanceKnobs = true;
        private bool _useSequentialRoute = true;
        private int _routeSmallProjects = 3;
        private int _routeMediumProjects = 2;
        private int _routeLargeProjects = 2;
        private int _smallDurationWeeks = 4;
        private int _mediumDurationWeeks = 6;
        private int _largeDurationWeeks = 8;
        private int _smallProjectCost = 15000;
        private int _mediumProjectCost = 50000;
        private int _largeProjectCost = 200000;
        private int _smallUpdateCost = 10000;
        private int _mediumUpdateCost = 35000;
        private int _largeUpdateCost = 100000;
        private int _weeklySmallUpdateCount;
        private int _weeklyMediumUpdateCount;
        private int _weeklyLargeUpdateCount;
        private int _recruitCost = 10000;
        private int _basicTrainingCost = 3000;
        private int _professionalTrainingCost = 10000;
        private int _intensiveTrainingCost = 30000;
        private int _officeLevel = 1;
        private int _level1WeeklyRent = 2000;
        private int _level2WeeklyRent = 4000;
        private int _level3WeeklyRent = 6000;
        private int _level2ExpansionCost = 200000;
        private int _level3ExpansionCost = 600000;
        private int _smallUnitPrice = 150;
        private int _mediumUnitPrice = 200;
        private int _largeUnitPrice = 300;
        private int _smallBaseSales = 300;
        private int _mediumBaseSales = 500;
        private int _largeBaseSales = 800;
        private float _simulatedCompletionScore = 70f;
        private float _retentionDecayPerDay = 0.05f;
        private float _minimumPurchaseWeight = 0.1f;

        private int _targetSurviveWeeks = 12;
        private int _targetMinGold;
        private int _targetCumulativeNetMin;
        private float _targetCoverageMin = 1f;
        private float _targetCoverageMax = 5f;
        private float _targetHiringRatioMax = 0.35f;
        private float _targetTrainingRatioMax = 0.35f;
        private float _targetProjectCostRatioMax = 0.55f;

        private bool _hasBaseline;
        private bool _showGraph = true;
        private bool _showTuningChecklist = true;
        private bool _showPresetSettings;
        private bool _showDetailedSettings;
        private bool _showAdvancedAnalysis;
        private bool _showTimelineDetails;
        private bool _checkedStartMoney;
        private bool _checkedFixedCost;
        private bool _checkedProjectRoute;
        private bool _checkedRevenueRecovery;
        private bool _checkedGrowthCost;
        private EconomyGraphMode _graphMode = EconomyGraphMode.Gold;
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
            _useLevelBalanceOverrides = true;
            _useSequentialRoute = true;
            RefreshAssets();
            Simulate();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawSummary();
            DrawEconomyGraph();
            DrawTuningChecklist();
            DrawEconomyGuide();
            DrawInputPanel();
            DrawAdvancedAnalysisPanel();
            EditorGUILayout.EndScrollView();
        }


        private static void DrawEconomyGuide()
        {
            BalanceGuideUI.Draw(
                "재화 밸런싱 가이드",
                "초기 자금\n직원 수/급여\n프로젝트 개발비\n기본 판매량/단가\n유지력 감소",
                "최종 자금/최저 자금\n첫 적자 주차\n수입/지출 배율\n지출 항목별 그래프",
                "첫 프로젝트 비용이 초기 자금 초과\n4주 안에 적자\n급여/개발비가 지출 대부분 차지");
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
                _showTuningChecklist = GUILayout.Toggle(_showTuningChecklist, "체크리스트", EditorStyles.toolbarButton, GUILayout.Width(90f));
                _showGraph = GUILayout.Toggle(_showGraph, "그래프", EditorStyles.toolbarButton, GUILayout.Width(70f));

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
            EditorGUILayout.LabelField("재화 수치 조절", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "처음에는 이 영역의 핵심값만 바꾸고, 결과는 위의 그래프/체크리스트로 확인합니다. 더 세밀한 값은 접힌 상세 설정에서 조정합니다.",
                EditorStyles.wordWrappedMiniLabel);
            BalanceGuideUI.DrawDataFlow(
                "초기 자금, 직원 수, 프로젝트 루트, 개발비, 판매량, 단가는 운영 흐름을 보기 위한 시뮬레이션 값입니다. DB 평균 급여를 켜면 직원 SO 급여를 참조합니다.",
                "이 창의 조절값은 원본 데이터에 저장되지 않습니다. 운영 루트를 바꿔 자금 흐름만 비교합니다.",
                "초기 자금/고정비/개발비 -> 주차별 수입·지출 -> 첫 적자 시점/최저 자금/누적 순이익");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                using (new EditorGUILayout.HorizontalScope())
                {
                    _initialGold = EditorGUILayout.IntField("초기 자금", _initialGold);
                    _simulationWeeks = EditorGUILayout.IntSlider("관찰 기간(주)", _simulationWeeks, 1, 80);
                    _employeeCount = EditorGUILayout.IntSlider("직원 수", _employeeCount, 0, 50);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _useAverageSalary = EditorGUILayout.Toggle("DB 평균 급여", _useAverageSalary, GUILayout.Width(150f));
                    using (new EditorGUI.DisabledScope(_useAverageSalary))
                        _manualWeeklySalary = EditorGUILayout.IntField("1인 주급", _manualWeeklySalary);
                }

                _useSequentialRoute = EditorGUILayout.Toggle("프로젝트 순차 진행", _useSequentialRoute);
                DrawLevelQuickKnobs();

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }

            DrawFoldoutSection(ref _showPresetSettings, "프리셋", DrawPresetPanel);
            DrawFoldoutSection(ref _showDetailedSettings, "상세 수치 설정", DrawDetailedSettingsPanel);
        }

        private void DrawLevelQuickKnobs()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("프로젝트 제작 루트", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _routeSmallProjects = EditorGUILayout.IntField("제작할 소형 수", Mathf.Max(0, _routeSmallProjects));
                _routeMediumProjects = EditorGUILayout.IntField("제작할 중형 수", Mathf.Max(0, _routeMediumProjects));
                _routeLargeProjects = EditorGUILayout.IntField("제작할 대형 수", Mathf.Max(0, _routeLargeProjects));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _smallDurationWeeks = EditorGUILayout.IntField("소형 기간(주)", Mathf.Max(1, _smallDurationWeeks));
                _mediumDurationWeeks = EditorGUILayout.IntField("중형 기간(주)", Mathf.Max(1, _mediumDurationWeeks));
                _largeDurationWeeks = EditorGUILayout.IntField("대형 기간(주)", Mathf.Max(1, _largeDurationWeeks));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("프로젝트 시작 비용", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _smallProjectCost = EditorGUILayout.IntField("소형 개발비", Mathf.Max(0, _smallProjectCost));
                _mediumProjectCost = EditorGUILayout.IntField("중형 개발비", Mathf.Max(0, _mediumProjectCost));
                _largeProjectCost = EditorGUILayout.IntField("대형 개발비", Mathf.Max(0, _largeProjectCost));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("출시 후 매출 가정", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _smallBaseSales = EditorGUILayout.IntField("소형 기본판매", Mathf.Max(0, _smallBaseSales));
                _mediumBaseSales = EditorGUILayout.IntField("중형 기본판매", Mathf.Max(0, _mediumBaseSales));
                _largeBaseSales = EditorGUILayout.IntField("대형 기본판매", Mathf.Max(0, _largeBaseSales));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _smallUnitPrice = EditorGUILayout.IntField("소형 단가", Mathf.Max(0, _smallUnitPrice));
                _mediumUnitPrice = EditorGUILayout.IntField("중형 단가", Mathf.Max(0, _mediumUnitPrice));
                _largeUnitPrice = EditorGUILayout.IntField("대형 단가", Mathf.Max(0, _largeUnitPrice));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _simulatedCompletionScore = EditorGUILayout.Slider("가정 완성도", _simulatedCompletionScore, 0f, 100f);
                _retentionDecayPerDay = EditorGUILayout.Slider("일일 유지력 감소", _retentionDecayPerDay, 0f, 0.2f);
            }
        }

        private void DrawDetailedSettingsPanel()
        {
            EditorGUI.BeginChangeCheck();
            DrawLevelBalanceTuningPanel();
            if (EditorGUI.EndChangeCheck())
                Simulate();
        }

        private static void DrawFoldoutSection(ref bool show, string title, Action drawContent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                show = EditorGUILayout.Foldout(show, title, true);
                if (show)
                    drawContent();
            }
        }

        private void DrawAdvancedAnalysisPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showAdvancedAnalysis = EditorGUILayout.Foldout(_showAdvancedAnalysis, "고급 분석", true);
                if (!_showAdvancedAnalysis)
                {
                    EditorGUILayout.LabelField("목표 범위, 기준값 비교, 리스크 신호, 시나리오 매트릭스는 필요할 때만 펼쳐서 봅니다.", EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                DrawTargetValidation();
                DrawBaselineCompare();
                DrawRiskPanel();
                DrawScenarioMatrix();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showTimelineDetails = EditorGUILayout.Foldout(_showTimelineDetails, "주차별 상세 표", true);
                if (_showTimelineDetails)
                    DrawTimeline();
                else
                    EditorGUILayout.LabelField("그래프에서 이상한 구간을 발견했을 때 펼쳐서 주차별 상세 수치를 확인합니다.", EditorStyles.wordWrappedMiniLabel);
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

        private void DrawLevelBalanceTuningPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _showLevelBalanceKnobs = EditorGUILayout.Foldout(_showLevelBalanceKnobs, "상세 조절값", true);
                    GUILayout.FlexibleSpace();
                }

                if (!_showLevelBalanceKnobs)
                    return;

                EditorGUILayout.LabelField("기획서의 재화 밸런싱 표에 있는 세부값을 조정하는 영역입니다. 자주 보는 핵심값은 위의 주요 조절값에도 노출되어 있습니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("프로젝트 루트", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _routeSmallProjects = EditorGUILayout.IntField("소형 개수", Mathf.Max(0, _routeSmallProjects));
                    _routeMediumProjects = EditorGUILayout.IntField("중형 개수", Mathf.Max(0, _routeMediumProjects));
                    _routeLargeProjects = EditorGUILayout.IntField("대형 개수", Mathf.Max(0, _routeLargeProjects));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _smallDurationWeeks = EditorGUILayout.IntField("소형 기간(주)", Mathf.Max(1, _smallDurationWeeks));
                    _mediumDurationWeeks = EditorGUILayout.IntField("중형 기간(주)", Mathf.Max(1, _mediumDurationWeeks));
                    _largeDurationWeeks = EditorGUILayout.IntField("대형 기간(주)", Mathf.Max(1, _largeDurationWeeks));
                }

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("프로젝트 비용", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _smallProjectCost = EditorGUILayout.IntField("소형 개발비", Mathf.Max(0, _smallProjectCost));
                    _mediumProjectCost = EditorGUILayout.IntField("중형 개발비", Mathf.Max(0, _mediumProjectCost));
                    _largeProjectCost = EditorGUILayout.IntField("대형 개발비", Mathf.Max(0, _largeProjectCost));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _smallUpdateCost = EditorGUILayout.IntField("소형 업데이트비", Mathf.Max(0, _smallUpdateCost));
                    _mediumUpdateCost = EditorGUILayout.IntField("중형 업데이트비", Mathf.Max(0, _mediumUpdateCost));
                    _largeUpdateCost = EditorGUILayout.IntField("대형 업데이트비", Mathf.Max(0, _largeUpdateCost));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _weeklySmallUpdateCount = EditorGUILayout.IntField("주간 소형 업데이트", Mathf.Max(0, _weeklySmallUpdateCount));
                    _weeklyMediumUpdateCount = EditorGUILayout.IntField("주간 중형 업데이트", Mathf.Max(0, _weeklyMediumUpdateCount));
                    _weeklyLargeUpdateCount = EditorGUILayout.IntField("주간 대형 업데이트", Mathf.Max(0, _weeklyLargeUpdateCount));
                }

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("채용/교육/사무실", EditorStyles.boldLabel);
                _recruitCost = EditorGUILayout.IntField("모집 1회 비용", Mathf.Max(0, _recruitCost));
                using (new EditorGUILayout.HorizontalScope())
                {
                    _basicTrainingCost = EditorGUILayout.IntField("기초 교육", Mathf.Max(0, _basicTrainingCost));
                    _professionalTrainingCost = EditorGUILayout.IntField("전문 교육", Mathf.Max(0, _professionalTrainingCost));
                    _intensiveTrainingCost = EditorGUILayout.IntField("집중 교육", Mathf.Max(0, _intensiveTrainingCost));
                }
                _officeLevel = EditorGUILayout.IntSlider("사무실 레벨", _officeLevel, 1, 3);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _level1WeeklyRent = EditorGUILayout.IntField("Lv1 주세", Mathf.Max(0, _level1WeeklyRent));
                    _level2WeeklyRent = EditorGUILayout.IntField("Lv2 주세", Mathf.Max(0, _level2WeeklyRent));
                    _level3WeeklyRent = EditorGUILayout.IntField("Lv3 주세", Mathf.Max(0, _level3WeeklyRent));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _level2ExpansionCost = EditorGUILayout.IntField("Lv2 증축비", Mathf.Max(0, _level2ExpansionCost));
                    _level3ExpansionCost = EditorGUILayout.IntField("Lv3 증축비", Mathf.Max(0, _level3ExpansionCost));
                }

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("매출 가정", EditorStyles.boldLabel);
                _simulatedCompletionScore = EditorGUILayout.Slider("가정 완성도", _simulatedCompletionScore, 0f, 100f);
                _minimumPurchaseWeight = EditorGUILayout.Slider("최소 구매 가중치", _minimumPurchaseWeight, 0f, 1f);
                _retentionDecayPerDay = EditorGUILayout.Slider("일일 유지력 감소", _retentionDecayPerDay, 0f, 0.2f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _smallUnitPrice = EditorGUILayout.IntField("소형 단가", Mathf.Max(0, _smallUnitPrice));
                    _mediumUnitPrice = EditorGUILayout.IntField("중형 단가", Mathf.Max(0, _mediumUnitPrice));
                    _largeUnitPrice = EditorGUILayout.IntField("대형 단가", Mathf.Max(0, _largeUnitPrice));
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _smallBaseSales = EditorGUILayout.IntField("소형 기본판매", Mathf.Max(0, _smallBaseSales));
                    _mediumBaseSales = EditorGUILayout.IntField("중형 기본판매", Mathf.Max(0, _mediumBaseSales));
                    _largeBaseSales = EditorGUILayout.IntField("대형 기본판매", Mathf.Max(0, _largeBaseSales));
                }

                int routeWeeks = _routeSmallProjects * _smallDurationWeeks + _routeMediumProjects * _mediumDurationWeeks + _routeLargeProjects * _largeDurationWeeks;
                EditorGUILayout.HelpBox($"현재 루트 개발 기간 합계: {routeWeeks}주. 문서의 최소 41주 기준과 비교할 때 1주 차이가 나면 튜토리얼/준비 주차 포함 여부를 확인하세요.", MessageType.Info);
            }
        }

        private void ApplyLevelBalanceV02Defaults()
        {
            _initialGold = 10000;
            _simulationWeeks = 52;
            _useLevelBalanceOverrides = true;
            _useSequentialRoute = true;
            _routeSmallProjects = 3;
            _routeMediumProjects = 2;
            _routeLargeProjects = 2;
            _smallDurationWeeks = 4;
            _mediumDurationWeeks = 6;
            _largeDurationWeeks = 8;
            _smallProjectCost = 15000;
            _mediumProjectCost = 50000;
            _largeProjectCost = 200000;
            _smallUpdateCost = 10000;
            _mediumUpdateCost = 35000;
            _largeUpdateCost = 100000;
            _weeklySmallUpdateCount = 0;
            _weeklyMediumUpdateCount = 0;
            _weeklyLargeUpdateCount = 0;
            _recruitCost = 10000;
            _basicTrainingCost = 3000;
            _professionalTrainingCost = 10000;
            _intensiveTrainingCost = 30000;
            _officeLevel = 1;
            _level1WeeklyRent = 2000;
            _level2WeeklyRent = 4000;
            _level3WeeklyRent = 6000;
            _level2ExpansionCost = 200000;
            _level3ExpansionCost = 600000;
            _smallUnitPrice = 150;
            _mediumUnitPrice = 200;
            _largeUnitPrice = 300;
            _smallBaseSales = 300;
            _mediumBaseSales = 500;
            _largeBaseSales = 800;
            _simulatedCompletionScore = 70f;
            _minimumPurchaseWeight = 0.1f;
            _retentionDecayPerDay = 0.05f;
            _weeklyOfficeMaintainCost = GetOfficeWeeklyRent();
            Simulate();
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
                BalanceGuideUI.DrawFormulaNotice("자금 흐름은 현재 재화 시뮬레이션 공식과 입력된 운영 루트로 계산됩니다.");
                DrawSummaryBaselineControls(summary);
                DrawEconomyAutoChecks(summary);
                DrawEconomyInterpretation(summary);

                if (summary.FirstDeficitWeek > 0)
                    EditorGUILayout.HelpBox($"{summary.FirstDeficitWeek}주차에 자금이 음수가 됩니다.", MessageType.Warning);
                else
                    EditorGUILayout.HelpBox("현재 조건에서는 시뮬레이션 기간 동안 자금이 음수가 되지 않습니다.", MessageType.Info);
            }
        }

        private void DrawSummaryBaselineControls(EconomySummary summary)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("변경 전후 비교", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(24f)))
                    {
                        _baselineSummary = summary;
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
                    EditorGUILayout.LabelField(BuildDeltaText("마지막 자금", summary.FinalGold, _baselineSummary.FinalGold), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("누적 순이익", summary.TotalNetGold, _baselineSummary.TotalNetGold), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("수입/지출 배율", summary.Coverage, _baselineSummary.Coverage), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("첫 적자 주차", summary.FirstDeficitWeek, _baselineSummary.FirstDeficitWeek), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawEconomyAutoChecks(EconomySummary summary)
        {
            int firstProjectCost = _useSequentialRoute || _startSmallProjects > 0
                ? GetProjectRequiredCost(ProjectSize.Small)
                : _startMediumProjects > 0
                    ? GetProjectRequiredCost(ProjectSize.Medium)
                    : _startLargeProjects > 0
                        ? GetProjectRequiredCost(ProjectSize.Large)
                        : 0;
            int weeklyFixedCost = GetWeeklySalaryPerEmployee() * _employeeCount + (_useLevelBalanceOverrides ? GetOfficeWeeklyRent() : _weeklyOfficeMaintainCost);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 감지", EditorStyles.boldLabel);
                BalanceGuideUI.DrawAutoCheck("첫 프로젝트 시작 가능", _initialGold < firstProjectCost, _initialGold < firstProjectCost ? $"초기 자금 {_initialGold:N0}G < 첫 프로젝트 비용 {firstProjectCost:N0}G" : "초기 자금이 첫 프로젝트 비용 이상입니다.");
                BalanceGuideUI.DrawAutoCheck("초반 적자", summary.FirstDeficitWeek > 0 && summary.FirstDeficitWeek <= Mathf.Min(4, _simulationWeeks), summary.FirstDeficitWeek > 0 ? $"{summary.FirstDeficitWeek}주차에 적자입니다." : "관찰 기간 동안 적자가 없습니다.");
                BalanceGuideUI.DrawAutoCheck("주간 고정비", weeklyFixedCost > Mathf.Max(1, _initialGold / 2), $"주간 고정비 {weeklyFixedCost:N0}G / 초기 자금 {_initialGold:N0}G");
            }
        }

        private static void DrawEconomyInterpretation(EconomySummary summary)
        {
            if (summary.FirstDeficitWeek > 0)
            {
                BalanceGuideUI.DrawInterpretation("운영 중 자금이 음수로 내려갑니다. 초기 자금, 프로젝트 개발비, 주간 고정비를 먼저 확인하세요.", MessageType.Warning);
                return;
            }

            if (summary.Coverage > 8f && summary.TotalIncome > 0)
            {
                BalanceGuideUI.DrawInterpretation("수입/지출 배율이 매우 높습니다. 보상이 과하게 넉넉한지 확인하세요.", MessageType.Warning);
                return;
            }

            if (summary.TotalNetGold >= 0)
                BalanceGuideUI.DrawInterpretation("현재 운영 루트에서는 시뮬레이션 기간 동안 자금 흐름이 안정권입니다.");
            else
                BalanceGuideUI.DrawInterpretation("최종 순이익이 음수입니다. 수입보다 반복 비용이 큰 구조입니다.", MessageType.Warning);
        }

        private void DrawTuningChecklist()
        {
            if (!_showTuningChecklist || _weeks.Count == 0)
                return;

            EconomySummary summary = BuildSummary(_weeks);
            int firstProjectCost = _useSequentialRoute || _startSmallProjects > 0
                ? GetProjectRequiredCost(ProjectSize.Small)
                : _startMediumProjects > 0
                    ? GetProjectRequiredCost(ProjectSize.Medium)
                    : _startLargeProjects > 0
                        ? GetProjectRequiredCost(ProjectSize.Large)
                        : 0;
            int weeklyFixedCost = _employeeCount * GetWeeklySalaryPerEmployee() + (_useLevelBalanceOverrides ? GetOfficeWeeklyRent() : _weeklyOfficeMaintainCost);
            int routeWeeks = _routeSmallProjects * _smallDurationWeeks + _routeMediumProjects * _mediumDurationWeeks + _routeLargeProjects * _largeDurationWeeks;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("수동 밸런싱 체크리스트", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("왼쪽 체크는 사용자가 직접 확인 완료 표시를 하는 용도입니다. 통과 여부는 현재 시뮬레이션 값 기준으로 자동 판정됩니다.", EditorStyles.wordWrappedMiniLabel);

                DrawChecklistLine(ref _checkedStartMoney, "1. 첫 프로젝트 시작 가능", _initialGold >= firstProjectCost, $"초기 자금 {_initialGold:N0}G / 첫 개발비 {firstProjectCost:N0}G", "초기 자금, 첫 프로젝트 개발비, 튜토리얼 지원금");
                DrawChecklistLine(ref _checkedFixedCost, "2. 주간 고정비 감당 가능", summary.FirstDeficitWeek == 0 || summary.FirstDeficitWeek > 4, $"주간 급여+주세 {weeklyFixedCost:N0}G / 첫 적자 {FormatDeficitWeek(summary.FirstDeficitWeek)}", "직원 수, 주급, 사무실 주세, 초반 매출");
                DrawChecklistLine(ref _checkedProjectRoute, "3. 목표 루트 기간 확인", !_useSequentialRoute || routeWeeks <= _simulationWeeks, $"루트 {routeWeeks}주 / 시뮬레이션 {_simulationWeeks}주", "프로젝트 개수, 규모별 개발 기간, 시뮬레이션 주차");
                DrawChecklistLine(ref _checkedRevenueRecovery, "4. 매출 회수력 확인", summary.Coverage >= _targetCoverageMin && summary.Coverage <= _targetCoverageMax, $"수입/지출 {summary.Coverage:0.##}배 / 목표 {_targetCoverageMin:0.##}~{_targetCoverageMax:0.##}배", "기본 판매량, 단가, 완성도 가정, 유지력 감소");
                DrawChecklistLine(ref _checkedGrowthCost, "5. 성장 비용 압박 확인", summary.TrainingExpenseRatio <= _targetTrainingRatioMax && summary.HiringExpenseRatio <= _targetHiringRatioMax, $"채용 {summary.HiringExpenseRatio:P0}, 교육 {summary.TrainingExpenseRatio:P0}", "모집 비용, 교육 비용, 주간 모집/교육 횟수");
            }
        }

        private static void DrawChecklistLine(ref bool checkedByUser, string title, bool autoPassed, string current, string knobs)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                checkedByUser = EditorGUILayout.Toggle(checkedByUser, GUILayout.Width(18f));
                GUILayout.Label(autoPassed ? "OK" : "확인", autoPassed ? EditorStyles.miniLabel : EditorStyles.boldLabel, GUILayout.Width(36f));
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(current, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"조정 후보: {knobs}", EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private static string FormatDeficitWeek(int firstDeficitWeek)
        {
            return firstDeficitWeek > 0 ? $"{firstDeficitWeek}주차" : "없음";
        }

        private void DrawEconomyGraph()
        {
            if (!_showGraph || _weeks.Count == 0)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("재화 흐름 그래프", EditorStyles.boldLabel, GUILayout.Width(110f));
                    _graphMode = (EconomyGraphMode)EditorGUILayout.EnumPopup(_graphMode, GUILayout.Width(170f));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("선이 급격히 꺾이는 주차가 조정 우선 구간입니다.", EditorStyles.miniLabel, GUILayout.Width(280f));
                }

                EconomyGraphSeries[] series = GetEconomyGraphSeries(_graphMode);
                Rect rect = GUILayoutUtility.GetRect(10f, 260f, GUILayout.ExpandWidth(true));
                DrawEconomyLineGraph(rect, series);
                DrawEconomyGraphLegend(series);
            }
        }

        private EconomyGraphSeries[] GetEconomyGraphSeries(EconomyGraphMode mode)
        {
            return mode switch
            {
                EconomyGraphMode.ExpenseBreakdown => new[]
                {
                    new EconomyGraphSeries("급여", new Color(0.88f, 0.48f, 0.38f), w => w.SalaryExpense),
                    new EconomyGraphSeries("개발비", new Color(0.95f, 0.68f, 0.26f), w => w.ProjectStartExpense),
                    new EconomyGraphSeries("채용", new Color(0.72f, 0.55f, 0.92f), w => w.RecruitExpense),
                    new EconomyGraphSeries("교육", new Color(0.45f, 0.72f, 0.95f), w => w.TrainingExpense),
                    new EconomyGraphSeries("사무실", new Color(0.62f, 0.82f, 0.48f), w => w.OfficeExpense + w.OneTimeExpense)
                },
                EconomyGraphMode.Net => new[]
                {
                    new EconomyGraphSeries("순이익", new Color(0.4f, 0.78f, 1f), w => w.NetGold),
                    new EconomyGraphSeries("종료 자금", new Color(0.95f, 0.78f, 0.32f), w => w.EndGold)
                },
                _ => new[]
                {
                    new EconomyGraphSeries("종료 자금", new Color(0.95f, 0.78f, 0.32f), w => w.EndGold),
                    new EconomyGraphSeries("수입", new Color(0.42f, 0.82f, 0.48f), w => w.Income),
                    new EconomyGraphSeries("지출", new Color(0.92f, 0.42f, 0.36f), w => w.Expense)
                }
            };
        }

        private void DrawEconomyLineGraph(Rect rect, EconomyGraphSeries[] series)
        {
            Rect plotRect = new Rect(rect.x + 56f, rect.y + 18f, rect.width - 82f, rect.height - 48f);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            EditorGUI.DrawRect(plotRect, new Color(0.08f, 0.08f, 0.08f));

            float min = Mathf.Min(0f, series.SelectMany(s => _weeks.Select(w => s.ValueSelector(w))).DefaultIfEmpty(0f).Min());
            float max = Mathf.Max(1f, series.SelectMany(s => _weeks.Select(w => s.ValueSelector(w))).DefaultIfEmpty(1f).Max());
            float padding = Mathf.Max(1f, (max - min) * 0.08f);
            min -= padding;
            max += padding;

            Handles.BeginGUI();
            DrawEconomyGrid(plotRect);
            DrawEconomyZeroGuide(plotRect, min, max);
            DrawEconomyWeekMarkers(plotRect);
            foreach (EconomyGraphSeries item in series)
                DrawEconomySeriesLine(plotRect, item, min, max);
            Handles.EndGUI();

            GUI.Label(new Rect(rect.x + 8f, plotRect.y - 4f, 46f, 18f), max.ToString("N0"), EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + 8f, plotRect.yMax - 14f, 46f, 18f), min.ToString("N0"), EditorStyles.miniLabel);
            GUI.Label(new Rect(plotRect.x, plotRect.yMax + 4f, 80f, 18f), "1주", EditorStyles.miniLabel);
            GUI.Label(new Rect(plotRect.xMax - 70f, plotRect.yMax + 4f, 90f, 18f), $"{_weeks.Count}주", EditorStyles.miniLabel);
        }

        private static void DrawEconomyGrid(Rect plotRect)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.12f);
            for (int i = 0; i <= 4; i++)
            {
                float y = Mathf.Lerp(plotRect.yMax, plotRect.y, i / 4f);
                Handles.DrawLine(new Vector3(plotRect.x, y), new Vector3(plotRect.xMax, y));
            }

            Handles.color = new Color(1f, 1f, 1f, 0.18f);
            Handles.DrawAAPolyLine(1.5f,
                new Vector3(plotRect.x, plotRect.y),
                new Vector3(plotRect.xMax, plotRect.y),
                new Vector3(plotRect.xMax, plotRect.yMax),
                new Vector3(plotRect.x, plotRect.yMax),
                new Vector3(plotRect.x, plotRect.y));
        }

        private static void DrawEconomyZeroGuide(Rect plotRect, float min, float max)
        {
            if (0f < min || 0f > max)
                return;

            float normalized = Mathf.InverseLerp(min, max, 0f);
            float y = Mathf.Lerp(plotRect.yMax, plotRect.y, normalized);
            Handles.color = new Color(1f, 0.35f, 0.28f, 0.9f);
            Handles.DrawDottedLine(new Vector3(plotRect.x, y), new Vector3(plotRect.xMax, y), 5f);
            GUI.color = new Color(1f, 0.5f, 0.42f);
            GUI.Label(new Rect(plotRect.xMax - 56f, y - 16f, 54f, 18f), "0G", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        private void DrawEconomyWeekMarkers(Rect plotRect)
        {
            if (_weeks.Count < 2)
                return;

            for (int i = 0; i < _weeks.Count; i++)
            {
                EconomyWeekSnapshot row = _weeks[i];
                if (row.ProjectStartExpense <= 0 && row.EndGold >= 0)
                    continue;

                float x = Mathf.Lerp(plotRect.x, plotRect.xMax, i / (float)(_weeks.Count - 1));
                Handles.color = row.EndGold < 0 ? new Color(1f, 0.28f, 0.22f, 0.5f) : new Color(1f, 0.72f, 0.22f, 0.35f);
                Handles.DrawAAPolyLine(2f, new Vector3(x, plotRect.y), new Vector3(x, plotRect.yMax));
            }
        }

        private void DrawEconomySeriesLine(Rect plotRect, EconomyGraphSeries series, float min, float max)
        {
            if (_weeks.Count < 2)
                return;

            var points = new Vector3[_weeks.Count];
            for (int i = 0; i < _weeks.Count; i++)
            {
                float x = Mathf.Lerp(plotRect.x, plotRect.xMax, i / (float)(_weeks.Count - 1));
                float normalized = Mathf.InverseLerp(min, max, series.ValueSelector(_weeks[i]));
                float y = Mathf.Lerp(plotRect.yMax, plotRect.y, normalized);
                points[i] = new Vector3(x, y);
            }

            Handles.color = series.Color;
            Handles.DrawAAPolyLine(2.5f, points);
        }

        private static void DrawEconomyGraphLegend(EconomyGraphSeries[] series)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (EconomyGraphSeries item in series)
                {
                    Rect colorRect = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f), GUILayout.Height(14f));
                    EditorGUI.DrawRect(colorRect, item.Color);
                    GUILayout.Label(item.Label, EditorStyles.miniLabel, GUILayout.Width(70f));
                }
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

            if (_useLevelBalanceOverrides && _useSequentialRoute && _initialGold < _smallProjectCost)
                risks.Add("현재 입력값 기준으로 초기 자금이 소형 개발비보다 낮습니다. 첫 프로젝트 지원금, 개발비 분할, 초기 자금 상향 중 하나가 필요할 수 있습니다.");

            int routeWeeks = _routeSmallProjects * _smallDurationWeeks + _routeMediumProjects * _mediumDurationWeeks + _routeLargeProjects * _largeDurationWeeks;
            if (_useSequentialRoute && routeWeeks > _simulationWeeks)
                risks.Add($"순차 루트 개발 기간({routeWeeks}주)이 시뮬레이션 기간({_simulationWeeks}주)보다 깁니다. 전체 완주 흐름을 보려면 시뮬레이션 주차를 늘리세요.");

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
            if (_useSequentialRoute)
                return SimulateSequentialRoute(scenario);

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
                                   + scenario.CompletedLargeProjects * PerkPolicy.CalcWeeklyCost(ProjectSize.Large)
                                   + CalcWeeklyUpdateExpense();
                int recruitExpense = scenario.WeeklyRecruitCount * GetRecruitCost();
                int trainingExpense = scenario.WeeklyTrainingCount * GetTrainingCost(scenario.TrainingPlan);
                int officeExpense = _useLevelBalanceOverrides ? GetOfficeWeeklyRent() : Mathf.Max(0, scenario.WeeklyOfficeMaintainCost);
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

                float weeklyRetentionDecay = GetRetentionDecayPerDay() * 5f;
                smallRetention = Mathf.Clamp01(smallRetention - weeklyRetentionDecay);
                mediumRetention = Mathf.Clamp01(mediumRetention - weeklyRetentionDecay);
                largeRetention = Mathf.Clamp01(largeRetention - weeklyRetentionDecay);
            }

            return weeks;
        }

        private List<EconomyWeekSnapshot> SimulateSequentialRoute(EconomyScenario scenario)
        {
            List<EconomyWeekSnapshot> weeks = new();
            Queue<ProjectSize> plan = BuildSequentialProjectPlan();
            List<LiveProjectState> liveProjects = new();
            int gold = scenario.InitialGold;
            ProjectSize? activeProject = null;
            int remainingDevelopmentWeeks = 0;

            for (int week = 1; week <= scenario.Weeks; week++)
            {
                int startGold = gold;
                int income = 0;
                for (int i = 0; i < liveProjects.Count; i++)
                {
                    LiveProjectState live = liveProjects[i];
                    income += CalcWeeklyCompletedProjectIncome(live.Size, 1, live.Retention);
                    live.Retention = Mathf.Clamp01(live.Retention - GetRetentionDecayPerDay() * 5f);
                    liveProjects[i] = live;
                }

                int salaryExpense = scenario.EmployeeCount * GetWeeklySalaryPerEmployee();
                int serviceExpense = (_useLevelBalanceOverrides
                    ? 0
                    : liveProjects.Sum(p => PerkPolicy.CalcWeeklyCost(p.Size))) + CalcWeeklyUpdateExpense();
                int recruitExpense = scenario.WeeklyRecruitCount * GetRecruitCost();
                int trainingExpense = scenario.WeeklyTrainingCount * GetTrainingCost(scenario.TrainingPlan);
                int officeExpense = _useLevelBalanceOverrides ? GetOfficeWeeklyRent() : Mathf.Max(0, scenario.WeeklyOfficeMaintainCost);
                int projectStartExpense = 0;
                int oneTimeExpense = week == 1 ? Mathf.Max(0, scenario.OneTimeOfficeUpgradeCost) : 0;

                if (activeProject == null && plan.Count > 0)
                {
                    activeProject = plan.Dequeue();
                    remainingDevelopmentWeeks = GetLevelProjectDurationWeeks(activeProject.Value);
                    projectStartExpense = GetProjectRequiredCost(activeProject.Value);
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

                if (activeProject != null)
                {
                    remainingDevelopmentWeeks--;
                    if (remainingDevelopmentWeeks <= 0)
                    {
                        liveProjects.Add(new LiveProjectState(activeProject.Value, 1f));
                        activeProject = null;
                    }
                }
            }

            return weeks;
        }

        private Queue<ProjectSize> BuildSequentialProjectPlan()
        {
            var plan = new Queue<ProjectSize>();
            for (int i = 0; i < Mathf.Max(0, _routeSmallProjects); i++)
                plan.Enqueue(ProjectSize.Small);
            for (int i = 0; i < Mathf.Max(0, _routeMediumProjects); i++)
                plan.Enqueue(ProjectSize.Medium);
            for (int i = 0; i < Mathf.Max(0, _routeLargeProjects); i++)
                plan.Enqueue(ProjectSize.Large);
            return plan;
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

        private int CalcWeeklyCompletedProjectIncome(ProjectSize size, int count, float retention)
        {
            if (count <= 0)
                return 0;

            int weeklyGold = 0;
            float currentRetention = retention;

            for (int day = 0; day < 5; day++)
            {
                int dailyGold = _useLevelBalanceOverrides
                    ? CalcLevelBalanceDailyGold(size, currentRetention)
                    : PerkPolicy.CalcDailyGold(size, PerkPolicy.CalcDailySales(size, 70f, 70f, 70f, currentRetention, 0));
                weeklyGold += dailyGold;
                currentRetention = Mathf.Clamp01(currentRetention - GetRetentionDecayPerDay());
            }

            return weeklyGold * count;
        }

        private int GetProjectRequiredCost(ProjectSize size)
        {
            if (_useLevelBalanceOverrides)
                return GetLevelProjectCost(size);

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

        private int GetTrainingCost(TrainingPlan plan)
        {
            if (_useLevelBalanceOverrides)
            {
                return plan switch
                {
                    TrainingPlan.Basic => _basicTrainingCost,
                    TrainingPlan.Professional => _professionalTrainingCost,
                    TrainingPlan.Intensive => _intensiveTrainingCost,
                    _ => 0
                };
            }

            return plan switch
            {
                TrainingPlan.Basic => 1000,
                TrainingPlan.Professional => 3000,
                TrainingPlan.Intensive => 5000,
                _ => 0
            };
        }

        private int CalcWeeklyUpdateExpense()
        {
            if (!_useLevelBalanceOverrides)
                return 0;

            return Mathf.Max(0, _weeklySmallUpdateCount) * Mathf.Max(0, _smallUpdateCost)
                 + Mathf.Max(0, _weeklyMediumUpdateCount) * Mathf.Max(0, _mediumUpdateCost)
                 + Mathf.Max(0, _weeklyLargeUpdateCount) * Mathf.Max(0, _largeUpdateCost);
        }

        private int GetRecruitCost()
        {
            return _useLevelBalanceOverrides ? Mathf.Max(0, _recruitCost) : 1000;
        }

        private int GetOfficeWeeklyRent()
        {
            return _officeLevel switch
            {
                2 => Mathf.Max(0, _level2WeeklyRent),
                3 => Mathf.Max(0, _level3WeeklyRent),
                _ => Mathf.Max(0, _level1WeeklyRent)
            };
        }

        private int GetLevelProjectCost(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => Mathf.Max(0, _mediumProjectCost),
                ProjectSize.Large => Mathf.Max(0, _largeProjectCost),
                _ => Mathf.Max(0, _smallProjectCost)
            };
        }

        private int GetLevelProjectDurationWeeks(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => Mathf.Max(1, _mediumDurationWeeks),
                ProjectSize.Large => Mathf.Max(1, _largeDurationWeeks),
                _ => Mathf.Max(1, _smallDurationWeeks)
            };
        }

        private int GetUnitPrice(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => Mathf.Max(0, _mediumUnitPrice),
                ProjectSize.Large => Mathf.Max(0, _largeUnitPrice),
                _ => Mathf.Max(0, _smallUnitPrice)
            };
        }

        private int GetBaseSales(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => Mathf.Max(0, _mediumBaseSales),
                ProjectSize.Large => Mathf.Max(0, _largeBaseSales),
                _ => Mathf.Max(0, _smallBaseSales)
            };
        }

        private float GetRetentionDecayPerDay()
        {
            return _useLevelBalanceOverrides ? Mathf.Max(0f, _retentionDecayPerDay) : PerkPolicy.RETENTION_DECAY;
        }

        private int CalcLevelBalanceDailyGold(ProjectSize size, float retention)
        {
            float purchaseWeight = Mathf.Max(_minimumPurchaseWeight, (_simulatedCompletionScore - 40f) / 100f);
            float sales = GetBaseSales(size) * purchaseWeight * Mathf.Clamp01(retention);
            return Mathf.RoundToInt(sales * GetUnitPrice(size));
        }

        private readonly struct EconomyGraphSeries
        {
            public string Label { get; }
            public Color Color { get; }
            public Func<EconomyWeekSnapshot, float> ValueSelector { get; }

            public EconomyGraphSeries(string label, Color color, Func<EconomyWeekSnapshot, float> valueSelector)
            {
                Label = label;
                Color = color;
                ValueSelector = valueSelector;
            }
        }

        private struct LiveProjectState
        {
            public ProjectSize Size { get; }
            public float Retention { get; set; }

            public LiveProjectState(ProjectSize size, float retention)
            {
                Size = size;
                Retention = retention;
            }
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

        private enum EconomyGraphMode
        {
            Gold,
            Net,
            ExpenseBreakdown
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
