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
            DrawInputPanel();
            DrawSummaryPanel();
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

                if (GUILayout.Button("흐름 다시 계산", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    Simulate();

                GUILayout.FlexibleSpace();
                _showDailyRows = GUILayout.Toggle(_showDailyRows, "일자 행", EditorStyles.toolbarButton, GUILayout.Width(70f));
                _showDevelopment = GUILayout.Toggle(_showDevelopment, "개발", EditorStyles.toolbarButton, GUILayout.Width(60f));
                _showLaunch = GUILayout.Toggle(_showLaunch, "출시", EditorStyles.toolbarButton, GUILayout.Width(60f));
            }
        }

        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("주차/일자 지표 흐름 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "월~금 낮 업무, 금요일 밤 보고서 채택, 출시 후 일일 매출까지 프로젝트/직원/재화 지표가 어떻게 변하는지 한 화면에서 추적합니다.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _projectSize = (ProjectSize)EditorGUILayout.EnumPopup("프로젝트 규모", _projectSize);
                _pickMode = (PickMode)EditorGUILayout.EnumPopup("대표 팀 구성", _pickMode);
                _simulationWeeks = EditorGUILayout.IntSlider("시뮬레이션 주차", _simulationWeeks, 1, 24);
                _initialGold = EditorGUILayout.IntField("초기 자금", _initialGold);
                _companyPopularity = EditorGUILayout.IntSlider("회사 인기", _companyPopularity, 0, 300);
                _officeWeeklyCost = EditorGUILayout.IntField("주간 사무실 유지비", _officeWeeklyCost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("일일/직원 변화 임시값", EditorStyles.boldLabel);
                _dailyQuestScore = EditorGUILayout.IntSlider("일일 업무 직군 보너스", _dailyQuestScore, 0, 10);
                _dailyFatigueGain = EditorGUILayout.IntSlider("낮 업무 피로 증가", _dailyFatigueGain, 0, 10);
                _dailyDesireDecay = EditorGUILayout.IntSlider("낮 업무 의욕 감소", _dailyDesireDecay, 0, 10);
                _fridayRestFatigueRecovery = EditorGUILayout.IntSlider("금요일 밤 휴식 피로 회복", _fridayRestFatigueRecovery, 0, 30);
                _fridayRestDesireRecovery = EditorGUILayout.IntSlider("금요일 밤 의욕 회복", _fridayRestDesireRecovery, 0, 30);

                if (EditorGUI.EndChangeCheck())
                    Simulate();
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

                DrawRiskLabel("자금 적자", minGold < 0);
                DrawRiskLabel("직원 번아웃 위험", final.Fatigue >= 80f);
                DrawRiskLabel("의욕 저하", final.Desire < 40f);
                DrawRiskLabel("품질 저점", final.Quality < 45f || final.Stability < 45f || final.Charm < 45f);
            }
        }

        private static void DrawRiskLabel(string label, bool isRisk)
        {
            string text = isRisk ? $"위험: {label}" : $"정상: {label}";
            MessageType type = isRisk ? MessageType.Warning : MessageType.None;
            EditorGUILayout.HelpBox(text, type);
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
