using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class RevenueSimulatorWindow : EditorWindow
    {
        private readonly List<RevenueDaySnapshot> _days = new();

        private Vector2 _scrollPosition;
        private ProjectSize _projectSize = ProjectSize.Small;
        private float _quality = 70f;
        private float _stability = 70f;
        private float _charm = 70f;
        private int _companyPopularity;
        private float _startRetention = 1f;
        private int _serviceWeeks = 8;
        private bool _chargeWeeklyCost = true;
        private bool _decayRetentionWeekly = true;
        private bool _showScenarioMatrix;
        private bool _showPresetPanel;
        private bool _showAdvancedAnalysis;
        private bool _showTimelineDetails;
        private bool _hasBaseline;
        private RevenueSummary _baselineSummary;
        private int _targetTotalSalesMin;
        private int _targetTotalSalesMax = 10000000;
        private int _targetTotalGoldMin;
        private int _targetTotalGoldMax = 100000000;
        private int _targetNetGoldMin;
        private int _targetNetGoldMax = 100000000;
        private float _targetCostCoverageMin = 1f;
        private float _targetCostCoverageMax = 50f;
        private int _targetFirstDeficitDayMin;
        private int _targetRetentionZeroDayMin;
        private int _burstDailyGoldLimit = 1000000;
        private int _lowDailyGoldLimit = 100;

        [MenuItem("Tools/Balance/4. Revenue Balance", false, 204)]
        public static void Open()
        {
            RevenueSimulatorWindow window = GetWindow<RevenueSimulatorWindow>("Revenue Simulator");
            window.minSize = new Vector2(700f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            Simulate();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            RevenueSummary summary = BuildSummary(_days);
            DrawSummary(summary);
            DrawRevenueGuide();
            DrawInputPanel();
            DrawFoldoutSection(ref _showPresetPanel, "대표 출시 케이스", DrawPresetPanel);
            DrawAdvancedAnalysisPanel(summary);
            DrawTimelineDetailsPanel();
            EditorGUILayout.EndScrollView();
        }


        private static void DrawRevenueGuide()
        {
            BalanceGuideUI.Draw(
                "매출 밸런싱 가이드",
                "프로젝트 규모\n완성도/안정성/매력도\n회사 인기\n초기 유지력\n서비스 기간",
                "총 판매량\n총 매출/순수익\n유지비 대비 매출\n유지력 0 도달 시점",
                "초반 매출 폭발\n유지력이 너무 빨리 0 도달\n낮은 등급도 수익이 과하게 높음");
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("시뮬레이션 실행", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    Simulate();

                GUILayout.FlexibleSpace();
            }
        }


        private static void DrawFoldoutSection(ref bool show, string title, System.Action drawContent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                show = EditorGUILayout.Foldout(show, title, true);
                if (show)
                    drawContent();
                else
                    EditorGUILayout.LabelField("필요할 때만 펼쳐서 확인합니다.", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawAdvancedAnalysisPanel(RevenueSummary summary)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showAdvancedAnalysis = EditorGUILayout.Foldout(_showAdvancedAnalysis, "고급 분석", true);
                if (!_showAdvancedAnalysis)
                {
                    EditorGUILayout.LabelField("목표 범위, 리스크 신호, 대표 케이스 매트릭스는 필요할 때만 펼쳐서 봅니다.", EditorStyles.wordWrappedMiniLabel);
                    return;
                }

                DrawTargetCheck(summary);
                DrawRiskPanel(summary);
                DrawScenarioMatrix();
            }
        }

        private void DrawTimelineDetailsPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _showTimelineDetails = EditorGUILayout.Foldout(_showTimelineDetails, "일자별 상세 표", true);
                if (_showTimelineDetails)
                    DrawTimeline();
                else
                    EditorGUILayout.LabelField("매출 곡선에서 이상한 구간을 발견했을 때 펼쳐서 일자별 수치를 확인합니다.", EditorStyles.wordWrappedMiniLabel);
            }
        }


        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("출시 매출 수치 조절", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "출시된 게임의 점수와 시장 조건을 바꿔 일일 판매량, 총매출, 순수익, 유지력 감소 흐름을 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);

            BalanceGuideUI.DrawSourceLegend();
            BalanceGuideUI.DrawImpactMap("완성도/안정성/매력도 -> 구매율과 총 판매량\n회사 인기 -> 기본 판매량 가중\n유지력/서비스 기간 -> 매출 감소 속도와 순수익");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("1. 프로젝트 결과값", EditorStyles.boldLabel);
                _projectSize = (ProjectSize)EditorGUILayout.EnumPopup(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "개발 규모"), _projectSize);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _quality = EditorGUILayout.Slider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "완성도"), _quality, 0f, 150f);
                    _stability = EditorGUILayout.Slider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "안정성"), _stability, 0f, 150f);
                    _charm = EditorGUILayout.Slider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "매력도"), _charm, 0f, 150f);
                }
                EditorGUILayout.LabelField("세 점수는 각각 구매율 계산에 들어가며, 합산되어 최종 판매량을 만듭니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("2. 시장/서비스 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _companyPopularity = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "회사 인기"), _companyPopularity, 0, 300);
                    _startRetention = EditorGUILayout.Slider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "초기 유지력"), _startRetention, 0f, 1f);
                    _serviceWeeks = EditorGUILayout.IntSlider(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "서비스 기간(주)"), _serviceWeeks, 1, 52);
                }
                EditorGUILayout.LabelField("인기는 기본 판매량을 키우고, 유지력은 시간이 지날수록 판매가 줄어드는 흐름을 만듭니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("3. 운영 비용/감소 규칙", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _chargeWeeklyCost = EditorGUILayout.Toggle(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "주간 유지비 차감"), _chargeWeeklyCost);
                    _decayRetentionWeekly = EditorGUILayout.Toggle(BalanceGuideUI.WithSource(BalanceGuideUI.WindowSource, "주간 유지력 감소"), _decayRetentionWeekly);
                }
                EditorGUILayout.LabelField("순수익과 장기 서비스 곡선을 볼 때 켜고 끄는 검증용 규칙입니다.", EditorStyles.wordWrappedMiniLabel);

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }
        }

        private void DrawPresetPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("대표 출시 케이스", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "자주 검증할 등급/규모 조합을 빠르게 불러와 매출 곡선을 비교합니다.",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawPresetButton("소형 B급", ProjectSize.Small, 55f, 55f, 55f, 0, 1f, 8);
                    DrawPresetButton("소형 A급", ProjectSize.Small, 75f, 75f, 75f, 20, 1f, 8);
                    DrawPresetButton("중형 B급", ProjectSize.Medium, 60f, 60f, 60f, 30, 1f, 10);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawPresetButton("중형 A급", ProjectSize.Medium, 78f, 78f, 78f, 50, 1f, 10);
                    DrawPresetButton("대형 A급", ProjectSize.Large, 82f, 82f, 82f, 80, 1f, 12);
                    DrawPresetButton("대형 S급", ProjectSize.Large, 95f, 95f, 95f, 120, 1f, 16);
                }
            }
        }

        private void DrawPresetButton(
            string label,
            ProjectSize size,
            float quality,
            float stability,
            float charm,
            int popularity,
            float retention,
            int weeks)
        {
            if (!GUILayout.Button(label, GUILayout.Height(28f)))
                return;

            _projectSize = size;
            _quality = quality;
            _stability = stability;
            _charm = charm;
            _companyPopularity = popularity;
            _startRetention = retention;
            _serviceWeeks = weeks;
            Simulate();
        }

        private void DrawTargetCheck(RevenueSummary summary)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("목표 범위 검증", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "대표 케이스가 의도한 판매량/매출/순수익 범위에 들어오는지 확인합니다.",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetTotalSalesMin = EditorGUILayout.IntField("총 판매량 최소", _targetTotalSalesMin);
                    _targetTotalSalesMax = EditorGUILayout.IntField("총 판매량 최대", _targetTotalSalesMax);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetTotalGoldMin = EditorGUILayout.IntField("총 매출 최소", _targetTotalGoldMin);
                    _targetTotalGoldMax = EditorGUILayout.IntField("총 매출 최대", _targetTotalGoldMax);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetNetGoldMin = EditorGUILayout.IntField("순수익 최소", _targetNetGoldMin);
                    _targetNetGoldMax = EditorGUILayout.IntField("순수익 최대", _targetNetGoldMax);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetCostCoverageMin = EditorGUILayout.FloatField("유지비 대비 최소", _targetCostCoverageMin);
                    _targetCostCoverageMax = EditorGUILayout.FloatField("유지비 대비 최대", _targetCostCoverageMax);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetFirstDeficitDayMin = EditorGUILayout.IntField("적자 최소 일차(0=무시)", _targetFirstDeficitDayMin);
                    _targetRetentionZeroDayMin = EditorGUILayout.IntField("유지력 0 최소 일차(0=무시)", _targetRetentionZeroDayMin);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _burstDailyGoldLimit = EditorGUILayout.IntField("매출 과폭발 기준", _burstDailyGoldLimit);
                    _lowDailyGoldLimit = EditorGUILayout.IntField("출시 직후 저매출 기준", _lowDailyGoldLimit);
                }

                DrawTargetMetric("총 판매량", summary.TotalSales, _targetTotalSalesMin, _targetTotalSalesMax);
                DrawTargetMetric("총 매출", summary.TotalGold, _targetTotalGoldMin, _targetTotalGoldMax);
                DrawTargetMetric("순수익", summary.TotalNetGold, _targetNetGoldMin, _targetNetGoldMax);
                DrawTargetMetric("유지비 대비 매출", summary.CostCoverage, _targetCostCoverageMin, _targetCostCoverageMax);
                DrawOptionalDayMetric("누적 적자 전환", summary.FirstCumulativeDeficitDay, _targetFirstDeficitDayMin);
                DrawOptionalDayMetric("유지력 0 도달", summary.RetentionZeroDay, _targetRetentionZeroDayMin);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(28f)))
                    {
                        _baselineSummary = summary;
                        _hasBaseline = true;
                    }

                    EditorGUI.BeginDisabledGroup(!_hasBaseline);
                    if (GUILayout.Button("기준값 지우기", GUILayout.Height(28f)))
                        _hasBaseline = false;
                    EditorGUI.EndDisabledGroup();
                }

                DrawBaselineCompare(summary);
            }
        }

        private static void DrawTargetMetric(string label, float value, float min, float max)
        {
            bool passed = value >= min && value <= max;
            EditorGUILayout.LabelField(
                $"{(passed ? "OK" : "NG")} {label}: {value:0.#} / 목표 {min:0.#}~{max:0.#}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private static void DrawOptionalDayMetric(string label, int day, int minDay)
        {
            if (minDay <= 0)
            {
                EditorGUILayout.LabelField($"SKIP {label}: {FormatDay(day)}", EditorStyles.miniLabel);
                return;
            }

            bool passed = day == 0 || day >= minDay;
            EditorGUILayout.LabelField(
                $"{(passed ? "OK" : "NG")} {label}: {FormatDay(day)} / 목표 {minDay}일차 이후",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private void DrawBaselineCompare(RevenueSummary current)
        {
            if (!_hasBaseline)
            {
                EditorGUILayout.HelpBox(
                    "수치 변경 전 결과를 기준값으로 저장하면, 이후 매출 곡선 변화량을 바로 비교할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("기준값 대비 변화", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(BuildDeltaText("총 판매량", current.TotalSales, _baselineSummary.TotalSales), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("총 매출", current.TotalGold, _baselineSummary.TotalGold), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("순수익", current.TotalNetGold, _baselineSummary.TotalNetGold), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("유지비 대비", current.CostCoverage, _baselineSummary.CostCoverage), EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"누적 적자 전환: {FormatDay(current.FirstCumulativeDeficitDay)} / 기준 {FormatDay(_baselineSummary.FirstCumulativeDeficitDay)}", EditorStyles.miniLabel);
        }

        private static string BuildDeltaText(string label, float current, float baseline)
        {
            float delta = current - baseline;
            return $"{label}: {current:0.#} ({delta:+0.#;-0.#;0} / 기준 {baseline:0.#})";
        }

        private void DrawSummary(RevenueSummary summary)
        {
            if (_days.Count == 0)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"총 판매량: {summary.TotalSales:N0}");
                EditorGUILayout.LabelField($"총 매출: {summary.TotalGold:N0}G");
                EditorGUILayout.LabelField($"총 유지비: {summary.TotalCost:N0}G");
                EditorGUILayout.LabelField($"순수익: {summary.TotalNetGold:N0}G");
                EditorGUILayout.LabelField($"예상 평판 증가: +{PerkPolicy.CalcReputationGainFromSales(summary.TotalSales):N0}");
                EditorGUILayout.LabelField($"마지막 유지력: {summary.FinalRetentionFactor:P0}");
                EditorGUILayout.LabelField($"누적 적자 전환: {FormatDay(summary.FirstCumulativeDeficitDay)}");
                EditorGUILayout.LabelField($"유지력 0 도달: {FormatDay(summary.RetentionZeroDay)}");
                EditorGUILayout.LabelField($"최고 일일 매출: {summary.PeakDailyGold:N0}G / 마지막 일일 매출: {summary.LastDailyGold:N0}G");
                EditorGUILayout.LabelField($"유지비 대비 매출: {summary.CostCoverage:0.##}배");
                BalanceGuideUI.DrawFormulaNotice("판매량, 매출, 순수익은 현재 매출 공식으로 계산된 예상값입니다.");
                DrawSummaryBaselineControls(summary);
                DrawRevenueAutoChecks(summary);
                DrawRevenueInterpretation(summary);
            }
        }

        private void DrawSummaryBaselineControls(RevenueSummary summary)
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
                DrawBaselineCompare(summary);
            }
        }

        private void DrawRevenueAutoChecks(RevenueSummary summary)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 감지", EditorStyles.boldLabel);
                BalanceGuideUI.DrawAutoCheck("누적 적자 전환", summary.FirstCumulativeDeficitDay > 0, summary.FirstCumulativeDeficitDay > 0 ? $"{summary.FirstCumulativeDeficitDay}일차에 누적 순수익이 음수입니다." : "서비스 기간 동안 누적 적자가 없습니다.");
                BalanceGuideUI.DrawAutoCheck("유지력 0 도달", summary.RetentionZeroDay > 0, summary.RetentionZeroDay > 0 ? $"{summary.RetentionZeroDay}일차에 유지력이 0에 도달합니다." : "서비스 기간 동안 유지력이 0까지 떨어지지 않습니다.");
                BalanceGuideUI.DrawAutoCheck("최고 일일 매출", summary.PeakDailyGold >= _burstDailyGoldLimit, summary.PeakDailyGold >= _burstDailyGoldLimit ? $"최고 일일 매출 {summary.PeakDailyGold:N0}G로 과폭발 기준 이상입니다." : $"최고 일일 매출 {summary.PeakDailyGold:N0}G입니다.");
            }
        }

        private static void DrawRevenueInterpretation(RevenueSummary summary)
        {
            if (summary.TotalNetGold < 0)
            {
                BalanceGuideUI.DrawInterpretation("서비스 기간 전체로 보면 적자입니다. 유지비, 단가, 기본 판매량을 먼저 확인하세요.", MessageType.Warning);
                return;
            }

            if (summary.RetentionZeroDay > 0 && summary.RetentionZeroDay <= 20)
            {
                BalanceGuideUI.DrawInterpretation("유지력이 빠르게 0에 도달합니다. 유지력 감소값이나 서비스 기간 가정을 확인하세요.", MessageType.Warning);
                return;
            }

            if (summary.CostCoverage >= 1f)
                BalanceGuideUI.DrawInterpretation("현재 조건에서는 서비스 기간 동안 유지비를 회수하는 구조입니다.");
            else
                BalanceGuideUI.DrawInterpretation("매출이 유지비를 충분히 덮지 못합니다. 규모별 기본 판매량/단가를 확인하세요.", MessageType.Warning);
        }

        private void DrawRiskPanel(RevenueSummary summary)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("위험 신호", EditorStyles.boldLabel);

                bool hasRisk = false;

                if (summary.PeakDailyGold >= _burstDailyGoldLimit)
                {
                    DrawRisk("매출 과폭발", $"최고 일일 매출이 {_burstDailyGoldLimit:N0}G 이상입니다. 회사 인기/규모 계수/점수 가중치가 과할 수 있습니다.");
                    hasRisk = true;
                }

                if (summary.FirstDayGold <= _lowDailyGoldLimit)
                {
                    DrawRisk("출시 직후 저매출", $"1일차 매출이 {_lowDailyGoldLimit:N0}G 이하입니다. 낮은 점수 프로젝트의 보상이 너무 약할 수 있습니다.");
                    hasRisk = true;
                }

                if (summary.TotalNetGold < 0)
                {
                    DrawRisk("서비스 기간 전체 적자", "총 순수익이 음수입니다. 유지비 대비 매출 구조를 확인해야 합니다.");
                    hasRisk = true;
                }

                if (summary.RetentionZeroDay > 0 && summary.RetentionZeroDay <= 20)
                {
                    DrawRisk("유지력 급락", $"유지력이 {summary.RetentionZeroDay}일차에 0에 도달합니다. 서비스 종료 판단이 너무 빨리 올 수 있습니다.");
                    hasRisk = true;
                }

                if (_projectSize == ProjectSize.Large && summary.CostCoverage < 2f)
                {
                    DrawRisk("대형 프로젝트 보상 약함", "대형 프로젝트인데 유지비 대비 매출이 낮습니다. 대형 프로젝트를 할 이유가 약해질 수 있습니다.");
                    hasRisk = true;
                }

                if (_projectSize == ProjectSize.Small && summary.CostCoverage > 20f)
                {
                    DrawRisk("소형 프로젝트 효율 과다", "소형 프로젝트가 유지비 대비 지나치게 효율적입니다. 중형/대형으로 넘어갈 이유가 약해질 수 있습니다.");
                    hasRisk = true;
                }

                if (!hasRisk)
                    EditorGUILayout.LabelField("현재 기준으로 뚜렷한 위험 신호는 없습니다.", EditorStyles.miniLabel);
            }
        }

        private static void DrawRisk(string title, string description)
        {
            EditorGUILayout.HelpBox($"{title}: {description}", MessageType.Warning);
        }

        private void DrawScenarioMatrix()
        {
            EditorGUILayout.Space(4f);
            _showScenarioMatrix = EditorGUILayout.Foldout(_showScenarioMatrix, "대표 매출 케이스 매트릭스", true);
            if (!_showScenarioMatrix)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "대표 등급별 매출/순수익을 한 번에 비교합니다. 목표 범위는 현재 입력된 값을 기준으로 판정합니다.",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("케이스 / 총 매출 / 순수익 / 유지비 대비 / 적자 전환 / 판정", EditorStyles.miniLabel);

                DrawMatrixRow("소형 B급", ProjectSize.Small, 55f, 55f, 55f, 0, 1f, 8);
                DrawMatrixRow("소형 A급", ProjectSize.Small, 75f, 75f, 75f, 20, 1f, 8);
                DrawMatrixRow("중형 B급", ProjectSize.Medium, 60f, 60f, 60f, 30, 1f, 10);
                DrawMatrixRow("중형 A급", ProjectSize.Medium, 78f, 78f, 78f, 50, 1f, 10);
                DrawMatrixRow("대형 A급", ProjectSize.Large, 82f, 82f, 82f, 80, 1f, 12);
                DrawMatrixRow("대형 S급", ProjectSize.Large, 95f, 95f, 95f, 120, 1f, 16);
            }
        }

        private void DrawMatrixRow(
            string label,
            ProjectSize size,
            float quality,
            float stability,
            float charm,
            int popularity,
            float retention,
            int weeks)
        {
            RevenueSummary summary = SimulateSummary(size, quality, stability, charm, popularity, retention, weeks);
            bool passed = IsInTargetRange(summary);

            EditorGUILayout.LabelField(
                $"{label} / {summary.TotalGold:N0}G / {summary.TotalNetGold:N0}G / {summary.CostCoverage:0.##}배 / {FormatDay(summary.FirstCumulativeDeficitDay)} / {(passed ? "OK" : "NG")}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private void DrawTimeline()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("일자별 흐름", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Day / Retention / Sales / Gold / Cost / Net", EditorStyles.miniLabel);

                foreach (RevenueDaySnapshot day in _days)
                {
                    string weekCost = day.WeeklyCost > 0 ? day.WeeklyCost.ToString("N0") : "-";
                    EditorGUILayout.LabelField(
                        $"{day.Day,2}일차  유지력 {day.RetentionFactor:P0}  판매 {day.DailySales,6:N0}  매출 {day.DailyGold,8:N0}G  유지비 {weekCost,8}  순 {day.NetGold,8:N0}G",
                        day.NetGold < 0 ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                }
            }
        }

        private void Simulate()
        {
            _days.Clear();
            SimulateInto(_days, _projectSize, _quality, _stability, _charm, _companyPopularity, _startRetention, _serviceWeeks);
        }

        private RevenueSummary SimulateSummary(
            ProjectSize size,
            float quality,
            float stability,
            float charm,
            int companyPopularity,
            float startRetention,
            int serviceWeeks)
        {
            var days = new List<RevenueDaySnapshot>();
            SimulateInto(days, size, quality, stability, charm, companyPopularity, startRetention, serviceWeeks);
            return BuildSummary(days);
        }

        private void SimulateInto(
            List<RevenueDaySnapshot> days,
            ProjectSize size,
            float quality,
            float stability,
            float charm,
            int companyPopularity,
            float startRetention,
            int serviceWeeks)
        {
            days.Clear();

            float retention = Mathf.Clamp01(startRetention);
            int totalDays = Mathf.Max(1, serviceWeeks) * 5;

            for (int day = 1; day <= totalDays; day++)
            {
                int dailySales = PerkPolicy.CalcDailySales(size, quality, stability, charm, retention, companyPopularity);
                int dailyGold = PerkPolicy.CalcDailyGold(size, dailySales);
                bool isWeeklySettlementDay = day % 5 == 0;
                int weeklyCost = _chargeWeeklyCost && isWeeklySettlementDay ? PerkPolicy.CalcWeeklyCost(size) : 0;
                int netGold = dailyGold - weeklyCost;

                days.Add(new RevenueDaySnapshot(day, retention, dailySales, dailyGold, weeklyCost, netGold));

                if (_decayRetentionWeekly && isWeeklySettlementDay)
                    retention = Mathf.Clamp01(retention - PerkPolicy.RETENTION_DECAY);
            }
        }

        private static RevenueSummary BuildSummary(List<RevenueDaySnapshot> days)
        {
            int totalSales = 0;
            int totalGold = 0;
            int totalCost = 0;
            int totalNetGold = 0;
            int cumulativeNet = 0;
            int firstCumulativeDeficitDay = 0;
            int retentionZeroDay = 0;
            int peakDailyGold = 0;
            int firstDayGold = days.Count > 0 ? days[0].DailyGold : 0;
            int lastDailyGold = days.Count > 0 ? days[days.Count - 1].DailyGold : 0;
            float finalRetentionFactor = days.Count > 0 ? days[days.Count - 1].RetentionFactor : 0f;

            foreach (RevenueDaySnapshot day in days)
            {
                totalSales += day.DailySales;
                totalGold += day.DailyGold;
                totalCost += day.WeeklyCost;
                totalNetGold += day.NetGold;
                cumulativeNet += day.NetGold;
                peakDailyGold = Mathf.Max(peakDailyGold, day.DailyGold);

                if (firstCumulativeDeficitDay == 0 && cumulativeNet < 0)
                    firstCumulativeDeficitDay = day.Day;

                if (retentionZeroDay == 0 && day.RetentionFactor <= 0.001f)
                    retentionZeroDay = day.Day;
            }

            float costCoverage = totalCost > 0 ? totalGold / (float)totalCost : 0f;

            return new RevenueSummary(
                totalSales,
                totalGold,
                totalCost,
                totalNetGold,
                finalRetentionFactor,
                firstCumulativeDeficitDay,
                retentionZeroDay,
                peakDailyGold,
                firstDayGold,
                lastDailyGold,
                costCoverage);
        }

        private bool IsInTargetRange(RevenueSummary summary)
        {
            if (summary.TotalSales < _targetTotalSalesMin || summary.TotalSales > _targetTotalSalesMax)
                return false;

            if (summary.TotalGold < _targetTotalGoldMin || summary.TotalGold > _targetTotalGoldMax)
                return false;

            if (summary.TotalNetGold < _targetNetGoldMin || summary.TotalNetGold > _targetNetGoldMax)
                return false;

            if (summary.CostCoverage < _targetCostCoverageMin || summary.CostCoverage > _targetCostCoverageMax)
                return false;

            if (_targetFirstDeficitDayMin > 0 && summary.FirstCumulativeDeficitDay > 0 && summary.FirstCumulativeDeficitDay < _targetFirstDeficitDayMin)
                return false;

            if (_targetRetentionZeroDayMin > 0 && summary.RetentionZeroDay > 0 && summary.RetentionZeroDay < _targetRetentionZeroDayMin)
                return false;

            return true;
        }

        private static string FormatDay(int day)
        {
            return day > 0 ? $"{day}일차" : "없음";
        }

        private readonly struct RevenueSummary
        {
            public int TotalSales { get; }
            public int TotalGold { get; }
            public int TotalCost { get; }
            public int TotalNetGold { get; }
            public float FinalRetentionFactor { get; }
            public int FirstCumulativeDeficitDay { get; }
            public int RetentionZeroDay { get; }
            public int PeakDailyGold { get; }
            public int FirstDayGold { get; }
            public int LastDailyGold { get; }
            public float CostCoverage { get; }

            public RevenueSummary(
                int totalSales,
                int totalGold,
                int totalCost,
                int totalNetGold,
                float finalRetentionFactor,
                int firstCumulativeDeficitDay,
                int retentionZeroDay,
                int peakDailyGold,
                int firstDayGold,
                int lastDailyGold,
                float costCoverage)
            {
                TotalSales = totalSales;
                TotalGold = totalGold;
                TotalCost = totalCost;
                TotalNetGold = totalNetGold;
                FinalRetentionFactor = finalRetentionFactor;
                FirstCumulativeDeficitDay = firstCumulativeDeficitDay;
                RetentionZeroDay = retentionZeroDay;
                PeakDailyGold = peakDailyGold;
                FirstDayGold = firstDayGold;
                LastDailyGold = lastDailyGold;
                CostCoverage = costCoverage;
            }
        }

        private readonly struct RevenueDaySnapshot
        {
            public int Day { get; }
            public float RetentionFactor { get; }
            public int DailySales { get; }
            public int DailyGold { get; }
            public int WeeklyCost { get; }
            public int NetGold { get; }

            public RevenueDaySnapshot(int day, float retentionFactor, int dailySales, int dailyGold, int weeklyCost, int netGold)
            {
                Day = day;
                RetentionFactor = retentionFactor;
                DailySales = dailySales;
                DailyGold = dailyGold;
                WeeklyCost = weeklyCost;
                NetGold = netGold;
            }
        }
    }
}
