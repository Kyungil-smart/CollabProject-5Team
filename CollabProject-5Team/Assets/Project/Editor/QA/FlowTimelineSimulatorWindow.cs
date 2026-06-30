using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class FlowTimelineSimulatorWindow : EditorWindow
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const string ProjectRoot = "Assets/Project/DB/Project";
        private const string ReportRoot = "Assets/Project/DB/Report";

        private readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        private readonly List<AssetEntry<ProjectSO>> _projects = new();
        private readonly List<AssetEntry<ReportSO>> _reports = new();
        private readonly List<FlowDaySnapshot> _timeline = new();
        private readonly List<FlowDaySnapshot> _comparisonTimeline = new();

        private Vector2 _scrollPosition;
        private ProjectSize _projectSize = ProjectSize.Small;
        private PickMode _pickMode = PickMode.Average;
        private int _simulationWeeks = 8;
        private int _initialGold = 10000;
        private int _companyPopularity;
        private int _officeWeeklyCost = 500;
        private int _dailyQuestScore = 2;
        private int _dailyFatigueGain = 2;
        private int _dailyDesireDecay = 1;
        private int _fridayRestFatigueRecovery = 5;
        private int _fridayRestDesireRecovery = 2;
        private bool _showDailyRows = true;
        private bool _showDevelopment = true;
        private bool _showLaunch = true;
        private bool _showGraph = true;
        private bool _focusGraph;
        private bool _showFormulaGuide;
        private bool _showFormulaTrace;
        private bool _showPresetControls;
        private TraceMode _traceMode = TraceMode.Project;
        private GraphMode _graphMode = GraphMode.Project;
        private bool _showAdvancedAnalysis;
        private bool _showTimelineDetails;
        private string _comparisonLabel = "비교 기준 없음";

        [MenuItem("Tools/Balance/7. Flow Timeline", false, 207)]
        public static void Open()
        {
            FlowTimelineSimulatorWindow window = GetWindow<FlowTimelineSimulatorWindow>("Flow Timeline");
            window.minSize = new Vector2(900f, 620f);
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
            DrawSummaryPanel();
            DrawGraphPanel();
            DrawFlowGuide();
            DrawInputPanel();
            DrawAdvancedAnalysisPanel();
            EditorGUILayout.EndScrollView();
        }


        private void DrawAdvancedAnalysisPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showAdvancedAnalysis = EditorGUILayout.Foldout(_showAdvancedAnalysis, "고급 분석", true);
                if (!_showAdvancedAnalysis)
                {
                    EditorGUILayout.LabelField("공식 안내, 계산 추적, 기준값 비교는 필요할 때만 펼쳐서 봅니다.", EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    DrawFormulaGuidePanel();
                    DrawFormulaTracePanel();
                    DrawComparisonPanel();
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showTimelineDetails = EditorGUILayout.Foldout(_showTimelineDetails, "일자별 상세 표", true);
                if (_showTimelineDetails)
                {
                    if (!_focusGraph)
                        DrawTimeline();
                    else
                        EditorGUILayout.HelpBox("그래프 집중 모드입니다. 표를 다시 보려면 상단의 집중 토글을 끄세요.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField("그래프에서 이상한 구간을 발견했을 때 펼쳐서 일자별 흐름을 확인합니다.", EditorStyles.wordWrappedMiniLabel);
                }
            }
        }


        private static void DrawFlowGuide()
        {
            BalanceGuideUI.Draw(
                "전체 흐름 밸런싱 가이드",
                "프로젝트 규모\n대표 팀 구성\n시뮬레이션 주차\n일일 업무 보너스\n피로/의욕 변화",
                "진척도 흐름\n완성도/안정성/매력도 추세\n직원 의욕/피로 추세\n자금 흐름",
                "출시 전 적자\n진척도는 끝났는데 점수가 낮음\n피로 80 이상/의욕 40 미만 고착");
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

                if (GUILayout.Button("흐름 다시 계산", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    Simulate();

                GUILayout.FlexibleSpace();
                _showFormulaGuide = GUILayout.Toggle(_showFormulaGuide, "공식 안내", EditorStyles.toolbarButton, GUILayout.Width(80f));
                _showFormulaTrace = GUILayout.Toggle(_showFormulaTrace, "계산 추적", EditorStyles.toolbarButton, GUILayout.Width(80f));
                _showGraph = GUILayout.Toggle(_showGraph, "그래프", EditorStyles.toolbarButton, GUILayout.Width(70f));
                using (new EditorGUI.DisabledScope(!_showGraph))
                    _focusGraph = GUILayout.Toggle(_focusGraph, "집중", EditorStyles.toolbarButton, GUILayout.Width(60f));
                _showDailyRows = GUILayout.Toggle(_showDailyRows, "일자 행", EditorStyles.toolbarButton, GUILayout.Width(70f));
                _showDevelopment = GUILayout.Toggle(_showDevelopment, "개발", EditorStyles.toolbarButton, GUILayout.Width(60f));
                _showLaunch = GUILayout.Toggle(_showLaunch, "출시", EditorStyles.toolbarButton, GUILayout.Width(60f));
            }
        }


        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("전체 루프 수치 조절", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "월~금 낮 업무, 금요일 밤 보고서, 출시 후 매출까지 한 번에 이어 보며 프로젝트/직원/재화 흐름을 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);

            BalanceGuideUI.DrawSourceLegend();
            BalanceGuideUI.DrawImpactMap("프로젝트/팀 조건 -> 출시 시점과 프로젝트 점수\n자금/고정비 -> 적자 발생 시점\n낮 업무/금요일 회복값 -> 직원 피로, 의욕, 장기 지속성");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("1. 프로젝트/팀 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _projectSize = (ProjectSize)EditorGUILayout.EnumPopup(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "개발 규모"), _projectSize);
                    _pickMode = (PickMode)EditorGUILayout.EnumPopup(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "대표 팀 구성"), _pickMode);
                    _simulationWeeks = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "관찰 기간(주)"), _simulationWeeks, 1, 24);
                }
                EditorGUILayout.LabelField("규모와 팀 구성에 따라 주차별 진척도, 세부 점수, 출시 시점이 달라집니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("2. 재화/시장 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _initialGold = EditorGUILayout.IntField(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "초기 자금"), _initialGold);
                    _companyPopularity = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "회사 인기"), _companyPopularity, 0, 300);
                    _officeWeeklyCost = EditorGUILayout.IntField(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "주간 사무실 유지비"), _officeWeeklyCost);
                }
                EditorGUILayout.LabelField("자금 흐름, 출시 전 적자 여부, 출시 후 매출 회복 가능성을 확인하는 조건입니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("3. 낮 업무 변화값", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _dailyQuestScore = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "일일 업무 보너스"), _dailyQuestScore, 0, 10);
                    _dailyFatigueGain = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "피로 증가"), _dailyFatigueGain, 0, 10);
                    _dailyDesireDecay = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "의욕 감소"), _dailyDesireDecay, 0, 10);
                }
                EditorGUILayout.LabelField("낮 업무가 프로젝트 점수와 직원 상태에 주는 임시 밸런스값입니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("4. 금요일 밤 회복값", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _fridayRestFatigueRecovery = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "피로 회복"), _fridayRestFatigueRecovery, 0, 30);
                    _fridayRestDesireRecovery = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "의욕 회복"), _fridayRestDesireRecovery, 0, 30);
                }
                EditorGUILayout.LabelField("밤 경영 이후 다음 주로 넘어갈 때 직원 상태가 얼마나 회복되는지 보는 값입니다.", EditorStyles.wordWrappedMiniLabel);

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showPresetControls = EditorGUILayout.Foldout(_showPresetControls, "빠른 프리셋", true);
                if (_showPresetControls)
                    DrawPresetButtons();
                else
                    EditorGUILayout.LabelField("대표 케이스가 필요할 때만 펼쳐서 불러옵니다.", EditorStyles.wordWrappedMiniLabel);
            }
        }


        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("대표 흐름 프리셋", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("소형/상위팀/안정", GUILayout.Height(24f)))
                    ApplyPreset(ProjectSize.Small, PickMode.Strong, 8, 15000, 20, 500, 3, 1, 0, 10, 5);

                if (GUILayout.Button("소형/평균팀", GUILayout.Height(24f)))
                    ApplyPreset(ProjectSize.Small, PickMode.Average, 8, 10000, 0, 500, 2, 2, 1, 5, 2);

                if (GUILayout.Button("중형/상위팀/공격", GUILayout.Height(24f)))
                    ApplyPreset(ProjectSize.Medium, PickMode.Strong, 10, 25000, 40, 1500, 4, 3, 1, 4, 2);

                if (GUILayout.Button("대형/낮은팀/고위험", GUILayout.Height(24f)))
                    ApplyPreset(ProjectSize.Large, PickMode.Low, 16, 45000, 80, 4000, 5, 5, 2, 2, 1);
            }
            EditorGUILayout.LabelField("프리셋은 비교용 시작값입니다. 실제 밸런스 확정값이 아니라 빠른 검증 기준으로 사용합니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private void ApplyPreset(
            ProjectSize projectSize,
            PickMode pickMode,
            int weeks,
            int initialGold,
            int popularity,
            int officeCost,
            int questScore,
            int fatigueGain,
            int desireDecay,
            int restFatigue,
            int restDesire)
        {
            _projectSize = projectSize;
            _pickMode = pickMode;
            _simulationWeeks = weeks;
            _initialGold = initialGold;
            _companyPopularity = popularity;
            _officeWeeklyCost = officeCost;
            _dailyQuestScore = questScore;
            _dailyFatigueGain = fatigueGain;
            _dailyDesireDecay = desireDecay;
            _fridayRestFatigueRecovery = restFatigue;
            _fridayRestDesireRecovery = restDesire;
            Simulate();
            Repaint();
        }


        private void DrawFormulaGuidePanel()
        {
            if (!_showFormulaGuide)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("공식/수치 수정 안내", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "이 시뮬레이터는 현재 적용된 공식으로 결과를 확인하는 도구입니다. 화면에 [조절 가능]으로 표시된 값만 이 창에서 바로 수정됩니다.",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawGuideColumn("[조절 가능]", "이 창에서 바로 바꿔 검증", "초기 자금\n회사 인기\n주간 유지비\n일일 업무 보너스\n피로/의욕 변화값");
                    DrawGuideColumn("[데이터 수정]", "SO/테이블에서 수정", "프로젝트 기간\n프로젝트 시작 비용\n직원 능력치/급여\n보고서 데이터\n직원 특성 데이터");
                    DrawGuideColumn("[코드 수정]", "공식 자체 수정 필요", "일일 판매량 공식\n보고서 점수 공식\n진척도 증가 방식\n유지력 감소 공식\n등급 판정 공식");
                }

                EditorGUILayout.HelpBox("수식이 바뀌어야 하는데 입력칸이 없다면, 못 찾는 것이 아니라 현재는 코드 또는 데이터 쪽 수정 대상일 가능성이 큽니다.", MessageType.Info);
            }
        }

        private static void DrawGuideColumn(string title, string subtitle, string body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(180f)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(body, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawSummaryPanel()
        {
            if (_timeline.Count == 0)
            {
                EditorGUILayout.HelpBox("표시할 시뮬레이션 결과가 없습니다.", MessageType.Warning);
                return;
            }

            FlowDaySnapshot final = _timeline[_timeline.Count - 1];
            FlowDaySnapshot launch = _timeline.FirstOrDefault(t => t.Phase == FlowPhase.Launch && t.DayOfWeek == 1);
            int minGold = _timeline.Min(t => t.EndGold);
            int totalIncome = _timeline.Sum(t => t.Income);
            int totalExpense = _timeline.Sum(t => t.Expense);
            int totalNet = totalIncome - totalExpense;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"최종 자금: {final.EndGold:N0}G / 최저 자금: {minGold:N0}G / 누적 순이익: {totalNet:N0}G");
                EditorGUILayout.LabelField($"최종 프로젝트: 완성도 {final.Quality:0.#}, 안정성 {final.Stability:0.#}, 매력도 {final.Charm:0.#}, 진척도 {final.Progress:0.#}%");
                EditorGUILayout.LabelField($"최종 직원 평균: 의욕 {final.Desire:0.#}, 피로도 {final.Fatigue:0.#}, 충성도 {final.Loyalty:0.#}");

                if (launch.Week > 0)
                    EditorGUILayout.LabelField($"출시 시작: {launch.Week}주차 월요일 / 출시 첫날 매출 {launch.Income:N0}G");
                else
                    EditorGUILayout.LabelField("출시 시작: 시뮬레이션 기간 내 미도달");

                BalanceGuideUI.DrawFormulaNotice("전체 흐름은 프로젝트/보고서/직원/매출 공식을 연결한 통합 시뮬레이션입니다.");
                DrawSummaryBaselineControls();
                DrawFlowAutoChecks();
                DrawFlowInterpretation(final, minGold, totalNet);

                DrawRiskLabel("자금 적자", minGold < 0);
                DrawRiskLabel("직원 번아웃 위험", final.Fatigue >= 80f);
                DrawRiskLabel("의욕 저하", final.Desire < 40f);
                DrawRiskLabel("품질 저점", final.Quality < 45f || final.Stability < 45f || final.Charm < 45f);
            }
        }

        private void DrawSummaryBaselineControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("변경 전후 비교", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(24f)))
                    {
                        _comparisonTimeline.Clear();
                        _comparisonTimeline.AddRange(_timeline);
                        _comparisonLabel = $"{_projectSize}/{_pickMode}/{_simulationWeeks}주";
                    }

                    using (new EditorGUI.DisabledScope(_comparisonTimeline.Count == 0))
                    {
                        if (GUILayout.Button("기준값 지우기", GUILayout.Width(110f), GUILayout.Height(24f)))
                        {
                            _comparisonTimeline.Clear();
                            _comparisonLabel = "비교 기준 없음";
                        }
                    }
                }

                BalanceGuideUI.DrawBaselineHint(_comparisonTimeline.Count > 0);
                if (_comparisonTimeline.Count > 0)
                {
                    FlowSummary baseline = BuildSummary(_comparisonTimeline);
                    FlowSummary current = BuildSummary(_timeline);
                    EditorGUILayout.LabelField($"기준: {_comparisonLabel}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("최종 자금", current.FinalGold, baseline.FinalGold), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("최저 자금", current.MinGold, baseline.MinGold), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("누적 순이익", current.TotalNet, baseline.TotalNet), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("평균 피로", current.Fatigue, baseline.Fatigue), EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(BuildDeltaText("평균 의욕", current.Desire, baseline.Desire), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawFlowAutoChecks()
        {
            FlowDaySnapshot firstRisk = _timeline.FirstOrDefault(t => t.HasRisk);
            FlowDaySnapshot firstDeficit = _timeline.FirstOrDefault(t => t.EndGold < 0);
            bool hasRisk = firstRisk.Week > 0 || firstRisk.HasRisk;
            bool hasDeficit = firstDeficit.Week > 0 || firstDeficit.EndGold < 0;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 감지", EditorStyles.boldLabel);
                BalanceGuideUI.DrawAutoCheck("첫 위험 구간", hasRisk, hasRisk ? $"{firstRisk.Week}주차 {firstRisk.DayOfWeek}일차: {firstRisk.Memo}" : "관찰 기간 동안 위험 행이 없습니다.");
                BalanceGuideUI.DrawAutoCheck("첫 자금 적자", hasDeficit, hasDeficit ? $"{firstDeficit.Week}주차 {firstDeficit.DayOfWeek}일차에 자금이 {firstDeficit.EndGold:N0}G입니다." : "관찰 기간 동안 자금 적자가 없습니다.");
            }
        }

        private static string BuildDeltaText(string label, float current, float baseline)
        {
            float delta = current - baseline;
            return $"{label}: 현재 {current:0.#} / 기준 {baseline:0.#} / 차이 {delta:+0.#;-0.#;0}";
        }

        private static void DrawFlowInterpretation(FlowDaySnapshot final, int minGold, int totalNet)
        {
            if (minGold < 0)
            {
                BalanceGuideUI.DrawInterpretation("출시 전후 어느 시점에 자금이 적자입니다. 초기 자금, 개발 기간, 고정비를 먼저 확인하세요.", MessageType.Warning);
                return;
            }

            if (final.Fatigue >= 80f || final.Desire < 40f)
            {
                BalanceGuideUI.DrawInterpretation("자금은 버티지만 직원 상태가 위험합니다. 낮 업무 피로와 금요일 회복값을 확인하세요.", MessageType.Warning);
                return;
            }

            if (final.Quality < 45f || final.Stability < 45f || final.Charm < 45f)
            {
                BalanceGuideUI.DrawInterpretation("프로젝트 한 축이 낮습니다. 해당 직군 직원/보고서/일일 업무 보너스를 확인하세요.", MessageType.Warning);
                return;
            }

            if (totalNet >= 0)
                BalanceGuideUI.DrawInterpretation("현재 조건은 전체 루프 기준으로 자금, 직원 상태, 프로젝트 점수가 비교적 안정적입니다.");
            else
                BalanceGuideUI.DrawInterpretation("최종 누적 순이익이 음수입니다. 출시 후 매출이나 개발 전 지출을 확인하세요.", MessageType.Warning);
        }

        private static void DrawRiskLabel(string label, bool isRisk)
        {
            string text = isRisk ? $"위험: {label}" : $"정상: {label}";
            MessageType type = isRisk ? MessageType.Warning : MessageType.None;
            EditorGUILayout.HelpBox(text, type);
        }




        private void DrawFormulaTracePanel()
        {
            if (!_showFormulaTrace || _timeline.Count == 0)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("왜 이렇게 나왔나요?", EditorStyles.boldLabel, GUILayout.Width(140f));
                    _traceMode = (TraceMode)EditorGUILayout.EnumPopup(_traceMode, GUILayout.Width(160f));
                    if (GUILayout.Button("현재 그래프 기준으로 보기", GUILayout.Width(150f)))
                        _traceMode = (TraceMode)_graphMode;
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("공식 출처 / 원인 분해 / 조정 후보를 함께 표시합니다.", EditorStyles.miniLabel, GUILayout.Width(300f));
                }

                switch (_traceMode)
                {
                    case TraceMode.Money:
                        DrawMoneyTrace();
                        break;
                    case TraceMode.Staff:
                        DrawStaffTrace();
                        break;
                    default:
                        DrawProjectTrace();
                        break;
                }
            }
        }

        private void DrawMoneyTrace()
        {
            ProjectSO project = GetProject(_projectSize);
            int maxPerPart = Mathf.Max(1, project != null ? project.maxEmployeePerPart : GetFallbackMaxEmployeePerPart(_projectSize));
            int projectStartCost = Mathf.Max(0, project != null ? project.requiredCost : GetFallbackProjectCost(_projectSize));
            int totalIncome = _timeline.Sum(t => t.Income);
            int totalExpense = _timeline.Sum(t => t.Expense);
            int finalGold = _timeline[_timeline.Count - 1].EndGold;
            int minGold = _timeline.Min(t => t.EndGold);
            int weeklySalary = BuildTeam(maxPerPart).Sum(e => Mathf.Max(0, e.weekSalary));

            DrawTraceColumns(
                "공식 출처",
                "자금 흐름: FlowTimelineSimulatorWindow.Simulate()\n출시 매출: PerkPolicy.CalcDailySales(), PerkPolicy.CalcDailyGold()\n프로젝트 비용: ProjectSO.requiredCost\n직원 급여: EmployeeImmutableData.weekSalary",
                "원인 분해",
                $"초기 자금: {_initialGold:N0}G\n프로젝트 시작 비용: -{projectStartCost:N0}G\n출시 총수입: +{totalIncome:N0}G\n급여/유지비 총지출: -{totalExpense:N0}G\n주간 급여 기준: {weeklySalary:N0}G + 유지비 {_officeWeeklyCost:N0}G\n최종 자금: {finalGold:N0}G\n최저 자금: {minGold:N0}G",
                "조정 후보",
                "[조절 가능] 초기 자금, 회사 인기, 주간 사무실 유지비\n[데이터 수정] 프로젝트 시작 비용, 직원 급여, 프로젝트 규모\n[코드 수정] 일일 판매량 공식, 규모별 매출 공식, 유지력 감소 공식");

            if (minGold < 0)
                EditorGUILayout.HelpBox("자금이 적자로 내려갑니다. 프로젝트 시작 비용, 출시 전 개발 기간, 주간 고정비, 출시 매출 공식을 우선 확인하세요.", MessageType.Warning);
        }

        private void DrawProjectTrace()
        {
            ProjectSO project = GetProject(_projectSize);
            int durationDays = Mathf.Max(5, project != null ? project.durationDays : GetFallbackDurationDays(_projectSize));
            int dayWorkCount = _timeline.Count(t => t.EventType == FlowEventType.DayWork);
            int reportCount = _timeline.Count(t => t.EventType == FlowEventType.FridayReport);
            FlowDaySnapshot final = _timeline[_timeline.Count - 1];
            FlowDaySnapshot launch = _timeline.FirstOrDefault(t => t.Phase == FlowPhase.Launch && t.DayOfWeek == 1);
            string launchText = launch.Week > 0 ? $"{launch.Week}주차 월요일" : "기간 내 미출시";

            DrawTraceColumns(
                "공식 출처",
                "진척도: FlowTimelineSimulatorWindow.Simulate()\n프로젝트 기간: ProjectSO.durationDays\n보고서 후보: ReportSO + 직원 특성/의욕\n보고서 점수: ReportPolicy.CalcScore(), CalcRoleScore()\n일일 업무 보너스: 이 창의 [조절 가능] 값",
                "원인 분해",
                $"프로젝트 기간 기준: {durationDays}일\n낮 업무 진행 일수: {dayWorkCount}일\n금요일 밤 보고서 반영: {reportCount}회\n일일 업무 직군 보너스: +{_dailyQuestScore}\n최종 진척도: {final.Progress:0.#}%\n최종 완성도/안정성/매력도: {final.Quality:0.#}/{final.Stability:0.#}/{final.Charm:0.#}\n출시 시작: {launchText}",
                "조정 후보",
                "[조절 가능] 일일 업무 직군 보너스, 대표 팀 구성\n[데이터 수정] ProjectSO.durationDays, ProjectSO.maxEmployeePerPart, 직원 능력치/특성, ReportSO 데이터\n[코드 수정] 진척도 증가 방식, 보고서 등급/점수 공식, 직군별 결과값 계산식");

            if (final.Quality < 45f || final.Stability < 45f || final.Charm < 45f)
                EditorGUILayout.HelpBox("프로젝트 점수 중 낮은 축이 있습니다. 해당 직군의 직원 능력치/특성, 보고서 데이터, 일일 업무 보너스를 먼저 확인하세요.", MessageType.Warning);
        }

        private void DrawStaffTrace()
        {
            ProjectSO project = GetProject(_projectSize);
            int maxPerPart = Mathf.Max(1, project != null ? project.maxEmployeePerPart : GetFallbackMaxEmployeePerPart(_projectSize));
            List<EmployeeImmutableData> team = BuildTeam(maxPerPart);
            float initialDesire = team.Count > 0 ? (float)team.Average(e => e.desire) : 0f;
            float initialFatigue = team.Count > 0 ? (float)team.Average(e => e.fatigue) : 0f;
            float initialLoyalty = team.Count > 0 ? (float)team.Average(e => e.loyalty) : 0f;
            int dayWorkCount = _timeline.Count(t => t.EventType == FlowEventType.DayWork);
            int reportCount = _timeline.Count(t => t.EventType == FlowEventType.FridayReport);
            FlowDaySnapshot final = _timeline[_timeline.Count - 1];

            DrawTraceColumns(
                "공식 출처",
                "초기 직원 상태: EmployeeImmutableData desire/fatigue/loyalty\n낮 업무 변화: 이 창의 [조절 가능] 피로/의욕 변화값\n금요일 밤 변화: SimulateFridayReports()의 보고서 피로/의욕/충성도 결과\n직원 선택: 대표 팀 구성 + ProjectSO.maxEmployeePerPart",
                "원인 분해",
                $"팀 인원: {team.Count}명\n초기 의욕/피로/충성: {initialDesire:0.#}/{initialFatigue:0.#}/{initialLoyalty:0.#}\n낮 업무 횟수: {dayWorkCount}회\n낮 업무 누적 피로 증가 추정: +{dayWorkCount * _dailyFatigueGain}\n낮 업무 누적 의욕 감소 추정: -{dayWorkCount * _dailyDesireDecay}\n금요일 밤 보고서/휴식: {reportCount}회\n최종 의욕/피로/충성: {final.Desire:0.#}/{final.Fatigue:0.#}/{final.Loyalty:0.#}",
                "조정 후보",
                "[조절 가능] 낮 업무 피로 증가, 낮 업무 의욕 감소, 금요일 밤 피로/의욕 회복\n[데이터 수정] 직원 초기 의욕/피로/충성도, 직원 급여/능력치, 프로젝트 인원수\n[코드 수정] 보고서 채택 피로 증가, 의욕 페널티, 충성도 변화 공식");

            if (final.Fatigue >= 80f || final.Desire < 40f)
                EditorGUILayout.HelpBox("직원 상태가 위험 구간입니다. 낮 업무 피로 증가, 금요일 회복량, 보고서 채택 피로 증가 공식을 우선 확인하세요.", MessageType.Warning);
        }

        private static void DrawTraceColumns(string titleA, string bodyA, string titleB, string bodyB, string titleC, string bodyC)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTraceColumn(titleA, bodyA);
                DrawTraceColumn(titleB, bodyB);
                DrawTraceColumn(titleC, bodyC);
            }
        }

        private static void DrawTraceColumn(string title, string body)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(220f)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(body, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawComparisonPanel()
        {
            if (_timeline.Count == 0)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("비교 모드", EditorStyles.boldLabel, GUILayout.Width(80f));
                    if (GUILayout.Button("현재 결과를 기준 A로 저장", GUILayout.Width(170f)))
                    {
                        _comparisonTimeline.Clear();
                        _comparisonTimeline.AddRange(_timeline);
                        _comparisonLabel = BuildCurrentSettingLabel();
                    }

                    using (new EditorGUI.DisabledScope(_comparisonTimeline.Count == 0))
                    {
                        if (GUILayout.Button("기준 지우기", GUILayout.Width(90f)))
                        {
                            _comparisonTimeline.Clear();
                            _comparisonLabel = "비교 기준 없음";
                        }
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(_comparisonLabel, EditorStyles.miniLabel, GUILayout.Width(360f));
                }

                if (_comparisonTimeline.Count == 0)
                {
                    EditorGUILayout.LabelField("현재 결과를 기준 A로 저장한 뒤 값을 바꾸면 A/B 차이를 볼 수 있습니다.", EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                FlowSummary baseline = BuildSummary(_comparisonTimeline);
                FlowSummary current = BuildSummary(_timeline);
                DrawComparisonLine("최종 자금", baseline.FinalGold, current.FinalGold, "G", true);
                DrawComparisonLine("최저 자금", baseline.MinGold, current.MinGold, "G", true);
                DrawComparisonLine("누적 순이익", baseline.TotalNet, current.TotalNet, "G", true);
                DrawComparisonLine("완성도", baseline.Quality, current.Quality, "", true);
                DrawComparisonLine("안정성", baseline.Stability, current.Stability, "", true);
                DrawComparisonLine("매력도", baseline.Charm, current.Charm, "", true);
                DrawComparisonLine("평균 피로도", baseline.Fatigue, current.Fatigue, "", false);
                DrawComparisonLine("평균 의욕", baseline.Desire, current.Desire, "", true);
            }
        }

        private string BuildCurrentSettingLabel()
        {
            return $"A: {_projectSize}/{_pickMode}, {_simulationWeeks}주, 자금 {_initialGold:N0}G, 인기 {_companyPopularity}";
        }

        private static FlowSummary BuildSummary(List<FlowDaySnapshot> rows)
        {
            if (rows.Count == 0)
                return FlowSummary.Empty;

            FlowDaySnapshot final = rows[rows.Count - 1];
            int totalIncome = rows.Sum(t => t.Income);
            int totalExpense = rows.Sum(t => t.Expense);
            return new FlowSummary(
                final.EndGold,
                rows.Min(t => t.EndGold),
                totalIncome - totalExpense,
                final.Quality,
                final.Stability,
                final.Charm,
                final.Desire,
                final.Fatigue);
        }

        private static void DrawComparisonLine(string label, float baseline, float current, string suffix, bool higherIsBetter)
        {
            float delta = current - baseline;
            bool isBetter = Mathf.Approximately(delta, 0f) || (higherIsBetter ? delta > 0f : delta < 0f);
            Color previous = GUI.color;
            GUI.color = Mathf.Approximately(delta, 0f) ? Color.white : isBetter ? new Color(0.62f, 0.9f, 0.62f) : new Color(1f, 0.62f, 0.52f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(90f));
                GUILayout.Label($"A {FormatCompareValue(baseline, suffix)}", GUILayout.Width(130f));
                GUILayout.Label($"현재 {FormatCompareValue(current, suffix)}", GUILayout.Width(150f));
                GUILayout.Label($"변화 {FormatSignedDelta(delta, suffix)}", EditorStyles.boldLabel);
            }

            GUI.color = previous;
        }

        private static string FormatCompareValue(float value, string suffix)
        {
            return string.IsNullOrEmpty(suffix) ? value.ToString("0.#") : $"{value:N0}{suffix}";
        }

        private static string FormatSignedDelta(float value, string suffix)
        {
            string sign = value > 0f ? "+" : string.Empty;
            return string.IsNullOrEmpty(suffix) ? $"{sign}{value:0.#}" : $"{sign}{value:N0}{suffix}";
        }

        private void DrawGraphPanel()
        {
            if (!_showGraph || _timeline.Count == 0)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(_focusGraph ? "그래프 집중 모드" : "그래프", EditorStyles.boldLabel, GUILayout.Width(_focusGraph ? 120f : 60f));
                    _graphMode = (GraphMode)EditorGUILayout.EnumPopup(_graphMode, GUILayout.Width(180f));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("주차선과 위험 기준선을 같이 표시합니다.", EditorStyles.miniLabel, GUILayout.Width(260f));
                }

                GraphSeries[] series = GetGraphSeries(_graphMode);
                Rect rect = GUILayoutUtility.GetRect(10f, _focusGraph ? 430f : 250f, GUILayout.ExpandWidth(true));
                DrawLineGraph(rect, series);
                DrawGraphLegend(series);
            }
        }

        private GraphSeries[] GetGraphSeries(GraphMode mode)
        {
            return mode switch
            {
                GraphMode.Money => new[]
                {
                    new GraphSeries("자금", new Color(0.95f, 0.72f, 0.24f), r => r.EndGold),
                    new GraphSeries("수입", new Color(0.38f, 0.78f, 0.48f), r => r.Income),
                    new GraphSeries("지출", new Color(0.9f, 0.42f, 0.36f), r => r.Expense)
                },
                GraphMode.Staff => new[]
                {
                    new GraphSeries("의욕", new Color(0.38f, 0.72f, 1f), r => r.Desire, 0f, 100f),
                    new GraphSeries("피로도", new Color(1f, 0.52f, 0.42f), r => r.Fatigue, 0f, 100f),
                    new GraphSeries("충성도", new Color(0.66f, 0.86f, 0.42f), r => r.Loyalty, 0f, 100f)
                },
                _ => new[]
                {
                    new GraphSeries("진척도", new Color(0.55f, 0.72f, 1f), r => r.Progress, 0f, 100f),
                    new GraphSeries("완성도", new Color(0.62f, 0.88f, 0.45f), r => r.Quality, 0f, 100f),
                    new GraphSeries("안정성", new Color(0.48f, 0.82f, 0.86f), r => r.Stability, 0f, 100f),
                    new GraphSeries("매력도", new Color(1f, 0.64f, 0.78f), r => r.Charm, 0f, 100f)
                }
            };
        }

        private void DrawLineGraph(Rect rect, GraphSeries[] series)
        {
            Rect plotRect = new Rect(rect.x + 52f, rect.y + 18f, rect.width - 78f, rect.height - 46f);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            EditorGUI.DrawRect(plotRect, new Color(0.08f, 0.08f, 0.08f));

            float min = series.Any(s => s.HasFixedRange) ? series.Min(s => s.Min) : Mathf.Min(0f, series.SelectMany(s => _timeline.Select(r => s.ValueSelector(r))).Min());
            float max = series.Any(s => s.HasFixedRange) ? series.Max(s => s.Max) : Mathf.Max(1f, series.SelectMany(s => _timeline.Select(r => s.ValueSelector(r))).Max());
            if (Mathf.Approximately(min, max))
                max = min + 1f;

            float padding = Mathf.Max(1f, (max - min) * 0.08f);
            if (!series.Any(s => s.HasFixedRange))
            {
                min -= padding;
                max += padding;
            }

            Handles.BeginGUI();
            DrawGrid(plotRect, min, max);
            DrawRiskGuides(plotRect, min, max);
            DrawTimelineMarkers(plotRect);

            foreach (GraphSeries item in series)
                DrawSeriesLine(plotRect, item, min, max);

            Handles.EndGUI();

            GUI.Label(new Rect(rect.x + 8f, plotRect.y - 4f, 42f, 18f), max.ToString("0.#"), EditorStyles.miniLabel);
            GUI.Label(new Rect(rect.x + 8f, plotRect.yMax - 14f, 42f, 18f), min.ToString("0.#"), EditorStyles.miniLabel);
            GUI.Label(new Rect(plotRect.x, plotRect.yMax + 4f, 140f, 18f), "시작", EditorStyles.miniLabel);
            GUI.Label(new Rect(plotRect.xMax - 60f, plotRect.yMax + 4f, 80f, 18f), "마지막", EditorStyles.miniLabel);
        }

        private static void DrawGrid(Rect plotRect, float min, float max)
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


        private void DrawRiskGuides(Rect plotRect, float min, float max)
        {
            switch (_graphMode)
            {
                case GraphMode.Project:
                    DrawHorizontalGuide(plotRect, min, max, 45f, "주의 45", new Color(1f, 0.78f, 0.24f, 0.85f));
                    DrawHorizontalGuide(plotRect, min, max, 70f, "양호 70", new Color(0.38f, 0.82f, 0.44f, 0.85f));
                    break;
                case GraphMode.Staff:
                    DrawHorizontalGuide(plotRect, min, max, 40f, "의욕 위험 40", new Color(1f, 0.78f, 0.24f, 0.85f));
                    DrawHorizontalGuide(plotRect, min, max, 80f, "피로 위험 80", new Color(1f, 0.36f, 0.28f, 0.85f));
                    break;
                case GraphMode.Money:
                    DrawHorizontalGuide(plotRect, min, max, 0f, "적자선 0", new Color(1f, 0.36f, 0.28f, 0.9f));
                    break;
            }
        }

        private static void DrawHorizontalGuide(Rect plotRect, float min, float max, float value, string label, Color color)
        {
            if (value < min || value > max)
                return;

            float normalized = Mathf.InverseLerp(min, max, value);
            float y = Mathf.Lerp(plotRect.yMax, plotRect.y, normalized);
            Handles.color = color;
            Handles.DrawDottedLine(new Vector3(plotRect.x, y), new Vector3(plotRect.xMax, y), 5f);
            GUI.color = color;
            GUI.Label(new Rect(plotRect.xMax - 88f, y - 16f, 86f, 18f), label, EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        private void DrawTimelineMarkers(Rect plotRect)
        {
            if (_timeline.Count < 2)
                return;

            for (int i = 0; i < _timeline.Count; i++)
            {
                FlowDaySnapshot row = _timeline[i];
                float x = Mathf.Lerp(plotRect.x, plotRect.xMax, i / (float)(_timeline.Count - 1));

                if (row.DayOfWeek == 1 && row.EventType == FlowEventType.DayWork)
                {
                    Handles.color = new Color(1f, 1f, 1f, 0.18f);
                    Handles.DrawLine(new Vector3(x, plotRect.y), new Vector3(x, plotRect.yMax));
                    GUI.Label(new Rect(x + 3f, plotRect.y + 2f, 48f, 18f), $"{row.Week}주", EditorStyles.miniLabel);
                }

                if (row.EventType == FlowEventType.FridayReport || row.EventType == FlowEventType.WeeklySettlement)
                {
                    Handles.color = new Color(1f, 0.62f, 0.22f, 0.42f);
                    Handles.DrawAAPolyLine(2f, new Vector3(x, plotRect.y), new Vector3(x, plotRect.yMax));
                }
            }
        }

        private void DrawSeriesLine(Rect plotRect, GraphSeries series, float min, float max)
        {
            if (_timeline.Count < 2)
                return;

            var points = new Vector3[_timeline.Count];
            for (int i = 0; i < _timeline.Count; i++)
            {
                float x = Mathf.Lerp(plotRect.x, plotRect.xMax, i / (float)(_timeline.Count - 1));
                float normalized = Mathf.InverseLerp(min, max, series.ValueSelector(_timeline[i]));
                float y = Mathf.Lerp(plotRect.yMax, plotRect.y, normalized);
                points[i] = new Vector3(x, y);
            }

            Handles.color = series.Color;
            Handles.DrawAAPolyLine(2.5f, points);
        }

        private static void DrawGraphLegend(GraphSeries[] series)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (GraphSeries item in series)
                {
                    Rect colorRect = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f), GUILayout.Height(14f));
                    EditorGUI.DrawRect(colorRect, item.Color);
                    GUILayout.Label(item.Label, EditorStyles.miniLabel, GUILayout.Width(62f));
                }
            }
        }

        private void DrawTimeline()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("일자별 지표 흐름", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "낮 업무는 일일 퀘스트/피로/의욕 흐름을, 금요일 밤은 보고서 채택/주간 비용을, 출시 후는 매출/유지력 감소를 보여줍니다.",
                    EditorStyles.wordWrappedMiniLabel);

                DrawHeader();

                foreach (FlowDaySnapshot row in _timeline)
                {
                    if (!_showDailyRows && row.EventType == FlowEventType.DayWork)
                        continue;
                    if (!_showDevelopment && row.Phase == FlowPhase.Development)
                        continue;
                    if (!_showLaunch && row.Phase == FlowPhase.Launch)
                        continue;

                    DrawRow(row);
                }
            }
        }

        private static void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("날짜", EditorStyles.toolbarButton, GUILayout.Width(95f));
                GUILayout.Label("단계", EditorStyles.toolbarButton, GUILayout.Width(70f));
                GUILayout.Label("이벤트", EditorStyles.toolbarButton, GUILayout.Width(170f));
                GUILayout.Label("진척", EditorStyles.toolbarButton, GUILayout.Width(55f));
                GUILayout.Label("완/안/매", EditorStyles.toolbarButton, GUILayout.Width(95f));
                GUILayout.Label("의/피/충", EditorStyles.toolbarButton, GUILayout.Width(95f));
                GUILayout.Label("수입", EditorStyles.toolbarButton, GUILayout.Width(80f));
                GUILayout.Label("지출", EditorStyles.toolbarButton, GUILayout.Width(80f));
                GUILayout.Label("자금", EditorStyles.toolbarButton, GUILayout.Width(90f));
                GUILayout.Label("메모", EditorStyles.toolbarButton);
            }
        }

        private static void DrawRow(FlowDaySnapshot row)
        {
            GUIStyle memoStyle = row.HasRisk ? EditorStyles.boldLabel : EditorStyles.label;
            Color previous = GUI.color;
            if (row.HasRisk)
                GUI.color = new Color(1f, 0.88f, 0.78f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"{row.Week}주 {GetDayLabel(row.DayOfWeek)}", GUILayout.Width(95f));
                GUILayout.Label(GetPhaseLabel(row.Phase), GUILayout.Width(70f));
                GUILayout.Label(GetEventLabel(row.EventType), GUILayout.Width(170f));
                GUILayout.Label($"{row.Progress:0.#}%", GUILayout.Width(55f));
                GUILayout.Label($"{row.Quality:0}/{row.Stability:0}/{row.Charm:0}", GUILayout.Width(95f));
                GUILayout.Label($"{row.Desire:0}/{row.Fatigue:0}/{row.Loyalty:0}", GUILayout.Width(95f));
                GUILayout.Label($"{row.Income:N0}", GUILayout.Width(80f));
                GUILayout.Label($"{row.Expense:N0}", GUILayout.Width(80f));
                GUILayout.Label($"{row.EndGold:N0}", GUILayout.Width(90f));
                GUILayout.Label(row.Memo, memoStyle);
            }

            GUI.color = previous;
        }

        private void Simulate()
        {
            _timeline.Clear();

            ProjectSO project = GetProject(_projectSize);
            int durationDays = Mathf.Max(5, project != null ? project.durationDays : GetFallbackDurationDays(_projectSize));
            int maxPerPart = Mathf.Max(1, project != null ? project.maxEmployeePerPart : GetFallbackMaxEmployeePerPart(_projectSize));
            int projectStartCost = Mathf.Max(0, project != null ? project.requiredCost : GetFallbackProjectCost(_projectSize));

            List<EmployeeImmutableData> team = BuildTeam(maxPerPart);
            if (team.Count == 0)
                return;

            int gold = _initialGold - projectStartCost;
            float progress = 0f;
            float quality = 0f;
            float stability = 0f;
            float charm = 0f;
            float desire = (float)team.Average(e => e.desire);
            float fatigue = (float)team.Average(e => e.fatigue);
            float loyalty = (float)team.Average(e => e.loyalty);
            float retention = 1f;
            bool launched = false;
            bool launchNextMonday = false;
            int acceptedReportWeeks = 0;
            int dayIndex = 0;

            for (int week = 1; week <= _simulationWeeks; week++)
            {
                int weeklyPlannerQuest = 0;
                int weeklyArtistQuest = 0;
                int weeklyProgrammerQuest = 0;

                for (int day = 1; day <= 5; day++)
                {
                    dayIndex++;

                    if (launchNextMonday && day == 1)
                    {
                        launched = true;
                        launchNextMonday = false;
                        retention = 1f;
                    }

                    if (!launched)
                    {
                        progress = Mathf.Min(100f, dayIndex * (100f / durationDays));
                        weeklyPlannerQuest += _dailyQuestScore;
                        weeklyArtistQuest += _dailyQuestScore;
                        weeklyProgrammerQuest += _dailyQuestScore;
                        fatigue = Mathf.Clamp(fatigue + _dailyFatigueGain, 0f, 100f);
                        desire = Mathf.Clamp(desire - _dailyDesireDecay, 0f, 100f);

                        AddRow(week, day, FlowPhase.Development, FlowEventType.DayWork, progress, quality, stability, charm, desire, fatigue, loyalty, 0, 0, gold,
                            $"낮 업무: 일퀘 누적 +{_dailyQuestScore}, 피로 +{_dailyFatigueGain}");
                    }
                    else
                    {
                        int dailySales = PerkPolicy.CalcDailySales(_projectSize, quality, stability, charm, retention, _companyPopularity);
                        int income = PerkPolicy.CalcDailyGold(_projectSize, dailySales);
                        gold += income;

                        AddRow(week, day, FlowPhase.Launch, FlowEventType.Sales, 100f, quality, stability, charm, desire, fatigue, loyalty, income, 0, gold,
                            $"출시 운영: 판매 {dailySales:N0}, 유지력 {retention:0.00}");

                        retention = Mathf.Clamp01(retention - PerkPolicy.RETENTION_DECAY);
                    }

                    if (day != 5)
                        continue;

                    int fridayExpense = team.Sum(e => Mathf.Max(0, e.weekSalary)) + Mathf.Max(0, _officeWeeklyCost);
                    string memo;
                    FlowEventType eventType;

                    if (!launched)
                    {
                        ReportWeekResult report = SimulateFridayReports(team, acceptedReportWeeks == 0 ? 1 : 0, weeklyPlannerQuest, weeklyArtistQuest, weeklyProgrammerQuest);
                        acceptedReportWeeks++;
                        quality = Mathf.Clamp(report.Quality, 0f, 100f);
                        stability = Mathf.Clamp(report.Stability, 0f, 100f);
                        charm = Mathf.Clamp(report.Charm, 0f, 100f);
                        fatigue = Mathf.Clamp(fatigue + report.FatigueGain - _fridayRestFatigueRecovery, 0f, 100f);
                        desire = Mathf.Clamp(desire - report.DesirePenalty + _fridayRestDesireRecovery, 0f, 100f);
                        loyalty = Mathf.Clamp(loyalty + report.LoyaltyDelta, 0f, 100f);
                        eventType = FlowEventType.FridayReport;
                        memo = report.Memo;

                        if (progress >= 100f)
                            launchNextMonday = true;
                    }
                    else
                    {
                        int weeklySales = PerkPolicy.CalcDailySales(_projectSize, quality, stability, charm, retention, _companyPopularity) * 5;
                        int reputationGain = PerkPolicy.CalcReputationGainFromSales(weeklySales);
                        loyalty = Mathf.Clamp(loyalty + Mathf.Min(2, reputationGain), 0f, 100f);
                        eventType = FlowEventType.WeeklySettlement;
                        memo = $"출시 주간 정산: 예상 평판 +{reputationGain}, 유지비/급여 차감";
                    }

                    gold -= fridayExpense;
                    AddRow(week, day, launched ? FlowPhase.Launch : FlowPhase.Development, eventType, progress, quality, stability, charm, desire, fatigue, loyalty, 0, fridayExpense, gold, memo);
                }
            }
        }

        private ReportWeekResult SimulateFridayReports(List<EmployeeImmutableData> team, int startRepo, int plannerQuest, int artistQuest, int programmerQuest)
        {
            RoleReportResult planner = SimulateRoleReport(Role.PLANNER, team, startRepo, plannerQuest);
            RoleReportResult artist = SimulateRoleReport(Role.ARTIST, team, startRepo, artistQuest);
            RoleReportResult programmer = SimulateRoleReport(Role.PROGRAMMER, team, startRepo, programmerQuest);

            int fatigueGain = planner.FatigueGain + artist.FatigueGain + programmer.FatigueGain;
            int desirePenalty = fatigueGain >= 35 ? 8 : fatigueGain >= 20 ? 4 : 0;
            int loyaltyDelta = planner.HasReport && artist.HasReport && programmer.HasReport ? 1 : -3;

            return new ReportWeekResult(
                planner.Score,
                programmer.Score,
                artist.Score,
                fatigueGain,
                desirePenalty,
                loyaltyDelta,
                $"밤 보고서: 기획 {planner.Label}, 개발 {programmer.Label}, 아트 {artist.Label} / 피로 +{fatigueGain}");
        }

        private RoleReportResult SimulateRoleReport(Role role, List<EmployeeImmutableData> team, int startRepo, int questBonus)
        {
            List<EmployeeImmutableData> members = team.Where(e => e.role == role).ToList();
            var previews = new List<RoleReportPreview>();

            foreach (EmployeeImmutableData employee in members)
            {
                int grade = ReportPolicy.CalcGrade(ReportPolicy.CalcScore(employee, employee.desire));
                AddReportPreviews(previews, employee, employee.mainTrait, grade, startRepo, "대표");
                AddReportPreviews(previews, employee, employee.subTrait, grade, startRepo, "보조");
                AddReportPreviews(previews, employee, employee.riskTrait, grade, startRepo, "리스크");
            }

            RoleReportPreview best = previews
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Grade)
                .ThenBy(p => p.ReportId)
                .FirstOrDefault();

            if (!best.IsValid)
                return RoleReportResult.Empty(role, questBonus);

            return new RoleReportResult(role, best.Score + questBonus, best.Grade, CalcAcceptedFatigue(best.Grade), true,
                $"{best.EmployeeName}/{best.TraitName}/G{best.Grade}+일퀘{questBonus}");
        }

        private void AddReportPreviews(List<RoleReportPreview> previews, EmployeeImmutableData employee, Trait trait, int grade, int startRepo, string source)
        {
            if (trait == Trait.None)
                return;

            foreach (AssetEntry<ReportSO> entry in _reports)
            {
                ReportSO report = entry.Asset;
                if (report == null || report.role != employee.role || report.trait != trait || report.grade != grade || report.startRepo != startRepo)
                    continue;

                float score = CalcRoleScore(employee, report, grade);
                previews.Add(new RoleReportPreview(employee.Name, report.id, GetTraitLabel(report.trait), source, grade, score));
            }
        }

        private static float CalcRoleScore(EmployeeImmutableData employee, ReportSO report, int grade)
        {
            TraitStat[] stats = ReportPolicy.GetRoleStats(employee.role);
            if (stats.Length == 0)
                return 0f;

            int ability = ReportPolicy.CalcLoyaltyAdjustedAbility(employee.ability, employee.loyalty);
            var scores = new Dictionary<TraitStat, float>
            {
                [stats[0]] = ability,
                [stats[1]] = ability,
                [stats[2]] = ability,
            };

            int mainDelta = grade == 1 ? 12 : grade == 2 ? 8 : 4;
            int subDelta = grade == 1 ? 6 : grade == 2 ? 4 : 2;
            int riskDelta = grade == 1 ? -6 : grade == 2 ? -4 : -2;

            ApplyTraitDelta(scores, employee.mainTrait, mainDelta);
            ApplyTraitDelta(scores, employee.subTrait, subDelta);
            ApplyTraitDelta(scores, employee.riskTrait, riskDelta);

            float traitWeight = QATraitUtility.TryGetTraitData(report.trait, out TraitData data)
                ? data.score * 2f
                : 0f;

            return (Mathf.Clamp(scores[stats[0]], 0f, 100f)
                + Mathf.Clamp(scores[stats[1]], 0f, 100f)
                + Mathf.Clamp(scores[stats[2]], 0f, 100f)
                + traitWeight) / 3f;
        }

        private static void ApplyTraitDelta(Dictionary<TraitStat, float> scores, Trait trait, int delta)
        {
            if (!QATraitUtility.TryGetTraitData(trait, out TraitData data))
                return;

            foreach (TraitStat stat in data.affectedStats)
            {
                if (scores.ContainsKey(stat))
                    scores[stat] += delta;
            }
        }

        private List<EmployeeImmutableData> BuildTeam(int maxPerPart)
        {
            var team = new List<EmployeeImmutableData>();
            team.AddRange(PickEmployees(Role.PLANNER, maxPerPart));
            team.AddRange(PickEmployees(Role.ARTIST, maxPerPart));
            team.AddRange(PickEmployees(Role.PROGRAMMER, maxPerPart));
            return team;
        }

        private List<EmployeeImmutableData> PickEmployees(Role role, int count)
        {
            IEnumerable<EmployeeImmutableData> source = _employees
                .Select(e => e.Asset)
                .Where(e => e != null && e.role == role);

            IEnumerable<EmployeeImmutableData> ordered = _pickMode switch
            {
                PickMode.Low => source.OrderBy(e => e.ability).ThenBy(e => e.id),
                PickMode.Strong => source.OrderByDescending(e => e.ability).ThenBy(e => e.id),
                _ => source.OrderBy(e => Mathf.Abs(e.ability - 50)).ThenBy(e => e.id)
            };

            return ordered.Take(count).ToList();
        }

        private void RefreshAssets()
        {
            _employees.Clear();
            _employees.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot)
                .OrderBy(e => e.Asset.role)
                .ThenBy(e => e.Asset.id));

            _projects.Clear();
            _projects.AddRange(QAAssetUtility.FindAssetEntriesByType<ProjectSO>(ProjectRoot)
                .OrderBy(e => e.Asset.scale));

            _reports.Clear();
            _reports.AddRange(QAAssetUtility.FindAssetEntriesByType<ReportSO>(ReportRoot)
                .OrderBy(e => e.Asset.role)
                .ThenBy(e => e.Asset.trait)
                .ThenBy(e => e.Asset.startRepo)
                .ThenBy(e => e.Asset.grade)
                .ThenBy(e => e.Asset.id));
        }

        private ProjectSO GetProject(ProjectSize size)
        {
            return _projects.Select(e => e.Asset).FirstOrDefault(p => p != null && p.scale == size);
        }

        private void AddRow(
            int week,
            int day,
            FlowPhase phase,
            FlowEventType eventType,
            float progress,
            float quality,
            float stability,
            float charm,
            float desire,
            float fatigue,
            float loyalty,
            int income,
            int expense,
            int endGold,
            string memo)
        {
            bool hasRisk = endGold < 0 || fatigue >= 80f || desire < 40f || quality < 30f || stability < 30f || charm < 30f;
            _timeline.Add(new FlowDaySnapshot(week, day, phase, eventType, progress, quality, stability, charm, desire, fatigue, loyalty, income, expense, endGold, memo, hasRisk));
        }

        private static int CalcAcceptedFatigue(int grade)
        {
            return grade switch
            {
                1 => 5,
                2 => 10,
                _ => 15
            };
        }

        private static int GetFallbackMaxEmployeePerPart(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => 2,
                ProjectSize.Large => 3,
                _ => 1
            };
        }

        private static int GetFallbackDurationDays(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => 15,
                ProjectSize.Large => 25,
                _ => 10
            };
        }

        private static int GetFallbackProjectCost(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => 5000,
                ProjectSize.Large => 12000,
                _ => 1000
            };
        }

        private static string GetDayLabel(int day)
        {
            return day switch
            {
                1 => "월",
                2 => "화",
                3 => "수",
                4 => "목",
                _ => "금"
            };
        }

        private static string GetPhaseLabel(FlowPhase phase)
        {
            return phase == FlowPhase.Launch ? "출시" : "개발";
        }

        private static string GetEventLabel(FlowEventType eventType)
        {
            return eventType switch
            {
                FlowEventType.FridayReport => "금요일 밤 보고서",
                FlowEventType.WeeklySettlement => "주간 정산",
                FlowEventType.Sales => "출시 매출",
                _ => "낮 업무"
            };
        }

        private static string GetTraitLabel(Trait trait)
        {
            return QATraitUtility.TryGetTraitData(trait, out TraitData data) ? data.displayName : trait.ToString();
        }

        private enum PickMode
        {
            Low,
            Average,
            Strong
        }

        private enum GraphMode
        {
            Project,
            Staff,
            Money
        }

        private enum TraceMode
        {
            Project,
            Staff,
            Money
        }

        private enum FlowPhase
        {
            Development,
            Launch
        }

        private enum FlowEventType
        {
            DayWork,
            FridayReport,
            Sales,
            WeeklySettlement
        }

        private readonly struct FlowSummary
        {
            public static FlowSummary Empty { get; } = new FlowSummary(0, 0, 0, 0f, 0f, 0f, 0f, 0f);

            public int FinalGold { get; }
            public int MinGold { get; }
            public int TotalNet { get; }
            public float Quality { get; }
            public float Stability { get; }
            public float Charm { get; }
            public float Desire { get; }
            public float Fatigue { get; }

            public FlowSummary(int finalGold, int minGold, int totalNet, float quality, float stability, float charm, float desire, float fatigue)
            {
                FinalGold = finalGold;
                MinGold = minGold;
                TotalNet = totalNet;
                Quality = quality;
                Stability = stability;
                Charm = charm;
                Desire = desire;
                Fatigue = fatigue;
            }
        }

        private readonly struct GraphSeries
        {
            public string Label { get; }
            public Color Color { get; }
            public Func<FlowDaySnapshot, float> ValueSelector { get; }
            public float Min { get; }
            public float Max { get; }
            public bool HasFixedRange { get; }

            public GraphSeries(string label, Color color, Func<FlowDaySnapshot, float> valueSelector)
            {
                Label = label;
                Color = color;
                ValueSelector = valueSelector;
                Min = 0f;
                Max = 0f;
                HasFixedRange = false;
            }

            public GraphSeries(string label, Color color, Func<FlowDaySnapshot, float> valueSelector, float min, float max)
            {
                Label = label;
                Color = color;
                ValueSelector = valueSelector;
                Min = min;
                Max = max;
                HasFixedRange = true;
            }
        }

        private readonly struct FlowDaySnapshot
        {
            public int Week { get; }
            public int DayOfWeek { get; }
            public FlowPhase Phase { get; }
            public FlowEventType EventType { get; }
            public float Progress { get; }
            public float Quality { get; }
            public float Stability { get; }
            public float Charm { get; }
            public float Desire { get; }
            public float Fatigue { get; }
            public float Loyalty { get; }
            public int Income { get; }
            public int Expense { get; }
            public int EndGold { get; }
            public string Memo { get; }
            public bool HasRisk { get; }

            public FlowDaySnapshot(
                int week,
                int dayOfWeek,
                FlowPhase phase,
                FlowEventType eventType,
                float progress,
                float quality,
                float stability,
                float charm,
                float desire,
                float fatigue,
                float loyalty,
                int income,
                int expense,
                int endGold,
                string memo,
                bool hasRisk)
            {
                Week = week;
                DayOfWeek = dayOfWeek;
                Phase = phase;
                EventType = eventType;
                Progress = progress;
                Quality = quality;
                Stability = stability;
                Charm = charm;
                Desire = desire;
                Fatigue = fatigue;
                Loyalty = loyalty;
                Income = income;
                Expense = expense;
                EndGold = endGold;
                Memo = memo;
                HasRisk = hasRisk;
            }
        }

        private readonly struct ReportWeekResult
        {
            public float Quality { get; }
            public float Stability { get; }
            public float Charm { get; }
            public int FatigueGain { get; }
            public int DesirePenalty { get; }
            public int LoyaltyDelta { get; }
            public string Memo { get; }

            public ReportWeekResult(float quality, float stability, float charm, int fatigueGain, int desirePenalty, int loyaltyDelta, string memo)
            {
                Quality = quality;
                Stability = stability;
                Charm = charm;
                FatigueGain = fatigueGain;
                DesirePenalty = desirePenalty;
                LoyaltyDelta = loyaltyDelta;
                Memo = memo;
            }
        }

        private readonly struct RoleReportResult
        {
            public Role Role { get; }
            public float Score { get; }
            public int Grade { get; }
            public int FatigueGain { get; }
            public bool HasReport { get; }
            public string Label { get; }

            public RoleReportResult(Role role, float score, int grade, int fatigueGain, bool hasReport, string label)
            {
                Role = role;
                Score = score;
                Grade = grade;
                FatigueGain = fatigueGain;
                HasReport = hasReport;
                Label = label;
            }

            public static RoleReportResult Empty(Role role, int questBonus)
            {
                return new RoleReportResult(role, questBonus, 3, 0, false, $"보고서 없음+일퀘{questBonus}");
            }
        }

        private readonly struct RoleReportPreview
        {
            public bool IsValid { get; }
            public string EmployeeName { get; }
            public int ReportId { get; }
            public string TraitName { get; }
            public string Source { get; }
            public int Grade { get; }
            public float Score { get; }

            public RoleReportPreview(string employeeName, int reportId, string traitName, string source, int grade, float score)
            {
                IsValid = true;
                EmployeeName = employeeName;
                ReportId = reportId;
                TraitName = traitName;
                Source = source;
                Grade = grade;
                Score = score;
            }
        }
    }
}
