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

        [MenuItem("Tools/Simulation/Revenue Simulator")]
        public static void Open()
        {
            RevenueSimulatorWindow window = GetWindow<RevenueSimulatorWindow>("Revenue Simulator");
            window.minSize = new Vector2(620f, 520f);
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
            DrawInputPanel();
            DrawSummary();
            DrawTimeline();
            EditorGUILayout.EndScrollView();
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

        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("매출 밸런스 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "출시 후 서비스 기간 동안 완성도, 안정성, 매력도, 회사 인기, 유지력 계수가 일일 판매량과 매출에 미치는 영향을 확인합니다.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                _projectSize = (ProjectSize)EditorGUILayout.EnumPopup("프로젝트 규모", _projectSize);
                _quality = EditorGUILayout.Slider("완성도", _quality, 0f, 150f);
                _stability = EditorGUILayout.Slider("안정성", _stability, 0f, 150f);
                _charm = EditorGUILayout.Slider("매력도", _charm, 0f, 150f);
                _companyPopularity = EditorGUILayout.IntSlider("회사 인기", _companyPopularity, 0, 300);
                _startRetention = EditorGUILayout.Slider("초기 유지력 계수", _startRetention, 0f, 1f);
                _serviceWeeks = EditorGUILayout.IntSlider("서비스 기간(주)", _serviceWeeks, 1, 52);
                _chargeWeeklyCost = EditorGUILayout.Toggle("주간 유지비 차감", _chargeWeeklyCost);
                _decayRetentionWeekly = EditorGUILayout.Toggle("주간 유지력 감소", _decayRetentionWeekly);

                if (EditorGUI.EndChangeCheck())
                    Simulate();
            }
        }

        private void DrawSummary()
        {
            if (_days.Count == 0)
                return;

            int totalSales = 0;
            int totalGold = 0;
            int totalCost = 0;
            int totalNetGold = 0;
            int finalRetentionPercent = Mathf.RoundToInt(_days[_days.Count - 1].RetentionFactor * 100f);

            foreach (RevenueDaySnapshot day in _days)
            {
                totalSales += day.DailySales;
                totalGold += day.DailyGold;
                totalCost += day.WeeklyCost;
                totalNetGold += day.NetGold;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("요약", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"총 판매량: {totalSales:N0}");
                EditorGUILayout.LabelField($"총 매출: {totalGold:N0}G");
                EditorGUILayout.LabelField($"총 유지비: {totalCost:N0}G");
                EditorGUILayout.LabelField($"순수익: {totalNetGold:N0}G");
                EditorGUILayout.LabelField($"예상 평판 증가: +{PerkPolicy.CalcReputationGainFromSales(totalSales):N0}");
                EditorGUILayout.LabelField($"마지막 유지력: {finalRetentionPercent}%");

                if (totalNetGold < 0)
                    EditorGUILayout.HelpBox("서비스 기간 전체 기준으로 유지비가 매출보다 큽니다.", MessageType.Warning);
            }
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

            float retention = Mathf.Clamp01(_startRetention);
            int totalDays = Mathf.Max(1, _serviceWeeks) * 5;

            for (int day = 1; day <= totalDays; day++)
            {
                int dailySales = PerkPolicy.CalcDailySales(_projectSize, _quality, _stability, _charm, retention, _companyPopularity);
                int dailyGold = PerkPolicy.CalcDailyGold(_projectSize, dailySales);
                bool isWeeklySettlementDay = day % 5 == 0;
                int weeklyCost = _chargeWeeklyCost && isWeeklySettlementDay ? PerkPolicy.CalcWeeklyCost(_projectSize) : 0;
                int netGold = dailyGold - weeklyCost;

                _days.Add(new RevenueDaySnapshot(day, retention, dailySales, dailyGold, weeklyCost, netGold));

                if (_decayRetentionWeekly && isWeeklySettlementDay)
                    retention = Mathf.Clamp01(retention - PerkPolicy.RETENTION_DECAY);
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
