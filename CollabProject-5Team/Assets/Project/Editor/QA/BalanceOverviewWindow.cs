using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class BalanceOverviewWindow : EditorWindow
    {
        const string EmployeeRoot = "Assets/Project/DB/Employee";
        const string ProjectRoot = "Assets/Project/DB/Project";
        const int DefaultOfficeWeeklyCost = 2000;

        readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        readonly List<AssetEntry<ProjectSO>> _projects = new();
        readonly List<OverviewWeekSnapshot> _weeks = new();

        Vector2 _scrollPosition;
        int _initialGold = 100000;
        int _simulationWeeks = 52;
        int _initialStaffCount = 4;
        int _companyPopularity;
        int _officeWeeklyCost = DefaultOfficeWeeklyCost;
        int _dailyQuestBonus = 2;
        int _weeklyRecruitCost;
        int _weeklyTrainingCost;
        int _smallProjectCount = 3;
        int _mediumProjectCount = 2;
        int _largeProjectCount = 2;
        int _dayBaseSeconds = 75;
        int _dialoguesPerDay = 1;
        int _dialogueSeconds = 30;
        int _nightManagementSeconds = 180;
        bool _showTimeline = true;
        bool _showAdvanced;
        bool _showPlaytimeAdvanced;

        [MenuItem("Tools/Balance/0. Balance Overview", false, 200)]
        public static void Open()
        {
            BalanceOverviewWindow window = GetWindow<BalanceOverviewWindow>("Balance Overview");
            window.minSize = new Vector2(760f, 560f);
            window.Show();
        }

        void OnEnable()
        {
            RefreshAssets();
            RunSimulation();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                {
                    RefreshAssets();
                    RunSimulation();
                }

                if (GUILayout.Button("시뮬레이션 실행", EditorStyles.toolbarButton, GUILayout.Width(130f)))
                    RunSimulation();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Balance Hub", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    BalanceHubWindow.Open();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("전체 밸런스 흐름", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "프로젝트/매출/재화/직원 상태를 한 화면에서 이어서 보는 개요 창입니다. 세부 공식 조정은 각 전용 밸런스 창에서 하고, 여기서는 전체 흐름이 과하게 쉽거나 막히는 구간이 있는지 확인합니다.",
                MessageType.Info);

            DrawInputSection();

            if (_weeks.Count == 0)
            {
                EditorGUILayout.HelpBox("시뮬레이션 결과가 없습니다. 직원/프로젝트 데이터가 있는지 확인한 뒤 다시 실행하세요.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            OverviewSummary summary = BuildSummary();
            DrawSummary(summary);
            DrawPlaytime(summary);
            DrawChecks(summary);
            DrawGraphs();

            _showTimeline = EditorGUILayout.Foldout(_showTimeline, "주차별 상세 흐름", true);
            if (_showTimeline)
                DrawTimeline();

            EditorGUILayout.EndScrollView();
        }

        void DrawInputSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("조절값", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("처음에는 이 값들만 바꿔도 회사가 성장/정체/파산하는 흐름을 볼 수 있습니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUI.BeginChangeCheck();
                _initialGold = EditorGUILayout.IntField("초기 자금", _initialGold);
                _simulationWeeks = EditorGUILayout.IntSlider("관찰 기간(주)", _simulationWeeks, 4, 104);
                _initialStaffCount = EditorGUILayout.IntSlider("초기 직원 수", _initialStaffCount, 1, 12);
                _companyPopularity = EditorGUILayout.IntSlider("회사 인기", _companyPopularity, 0, 200);
                _dailyQuestBonus = EditorGUILayout.IntSlider("일일 업무 보너스", _dailyQuestBonus, 0, 20);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("프로젝트 진행 순서", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _smallProjectCount = EditorGUILayout.IntField("소형", _smallProjectCount);
                    _mediumProjectCount = EditorGUILayout.IntField("중형", _mediumProjectCount);
                    _largeProjectCount = EditorGUILayout.IntField("대형", _largeProjectCount);
                }

                _showPlaytimeAdvanced = EditorGUILayout.Foldout(_showPlaytimeAdvanced, "예상 플레이타임", true);
                if (_showPlaytimeAdvanced)
                {
                    EditorGUILayout.LabelField("실제 유저 행동 시간을 가정하는 값입니다. 수치 밸런스에는 영향을 주지 않고, 도달 예상 시간 계산에만 사용합니다.", EditorStyles.wordWrappedMiniLabel);
                    _dayBaseSeconds = EditorGUILayout.IntSlider("낮 1일 기본 시간(초)", _dayBaseSeconds, 10, 300);
                    _dialoguesPerDay = EditorGUILayout.IntSlider("하루 평균 대화 횟수", _dialoguesPerDay, 0, 5);
                    _dialogueSeconds = EditorGUILayout.IntSlider("대화 1회 평균 시간(초)", _dialogueSeconds, 5, 180);
                    _nightManagementSeconds = EditorGUILayout.IntSlider("금요일 밤 경영 시간(초)", _nightManagementSeconds, 30, 600);
                }

                _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "상세 비용", true);
                if (_showAdvanced)
                {
                    _officeWeeklyCost = EditorGUILayout.IntField("주간 사무실 유지비", _officeWeeklyCost);
                    _weeklyRecruitCost = EditorGUILayout.IntField("주간 모집 비용", _weeklyRecruitCost);
                    _weeklyTrainingCost = EditorGUILayout.IntField("주간 교육 비용", _weeklyTrainingCost);
                }

                if (EditorGUI.EndChangeCheck())
                    RunSimulation();

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField($"불러온 데이터: 직원 {_employees.Count}명 / 프로젝트 SO {_projects.Count}개", EditorStyles.miniLabel);
            }
        }

        void DrawSummary(OverviewSummary summary)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("핵심 결과", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMetricCard("최종 자금", FormatGold(summary.FinalGold), summary.FinalGold < 0);
                DrawMetricCard("최저 자금", FormatGold(summary.MinGold), summary.MinGold < 0);
                DrawMetricCard("총 매출", FormatGold(summary.TotalIncome), false);
                DrawMetricCard("총 비용", FormatGold(summary.TotalExpense), summary.TotalExpense > summary.TotalIncome);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMetricCard("최고 평균 점수", summary.BestAverageScore.ToString("0.0"), summary.BestAverageScore < 70f);
                DrawMetricCard("마지막 피로", summary.FinalFatigue.ToString("0.0"), summary.FinalFatigue >= 80f);
                DrawMetricCard("마지막 의욕", summary.FinalDesire.ToString("0.0"), summary.FinalDesire < 40f);
                DrawMetricCard("서비스 프로젝트", summary.ReleasedProjectCount.ToString(), summary.ReleasedProjectCount == 0);
            }
        }

        static void DrawMetricCard(string title, string value, bool risk)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinHeight(54f)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.miniLabel);
                GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = risk ? new Color(1f, 0.45f, 0.35f) : EditorStyles.boldLabel.normal.textColor }
                };
                EditorGUILayout.LabelField(value, style);
            }
        }

        void DrawPlaytime(OverviewSummary summary)
        {
            PlaytimeSummary playtime = BuildPlaytimeSummary(summary);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("예상 플레이타임", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "아래 값은 실제 게임 시간이 아니라, 위에서 설정한 낮/대화/밤 평균 소요 시간을 시뮬레이션 주차에 곱한 예상 체감 플레이타임입니다.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMetricCard("1주 루프", FormatDuration(playtime.SecondsPerWeek), false);
                DrawMetricCard("첫 출시", FormatMilestone(playtime.FirstReleaseSeconds), playtime.FirstReleaseSeconds < 0);
                DrawMetricCard("중형 진입", FormatMilestone(playtime.MediumStartSeconds), playtime.MediumStartSeconds < 0);
                DrawMetricCard("대형 진입", FormatMilestone(playtime.LargeStartSeconds), playtime.LargeStartSeconds < 0);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("플레이 시간별 예상 도달점", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"30분: {DescribeAtPlaytime(30 * 60, playtime.SecondsPerWeek)}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"1시간: {DescribeAtPlaytime(60 * 60, playtime.SecondsPerWeek)}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"2시간: {DescribeAtPlaytime(120 * 60, playtime.SecondsPerWeek)}", EditorStyles.wordWrappedMiniLabel);
            }
        }

        void DrawChecks(OverviewSummary summary)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("자동 체크", EditorStyles.boldLabel);

            BalanceGuideUI.DrawAutoCheck(
                "자금 생존성",
                summary.FirstDeficitWeek > 0,
                summary.FirstDeficitWeek > 0
                    ? $"{summary.FirstDeficitWeek}주차에 자금이 음수가 됩니다. 초기 자금, 프로젝트 비용, 주간 비용, 매출 가중치를 확인하세요."
                    : "관찰 기간 동안 자금이 음수가 되지 않습니다.");

            BalanceGuideUI.DrawAutoCheck(
                "프로젝트 진입 흐름",
                !summary.HasMediumProject || !summary.HasLargeProject,
                $"중형 진입: {(summary.HasMediumProject ? "도달" : "미도달")} / 대형 진입: {(summary.HasLargeProject ? "도달" : "미도달")}. 프로젝트 비용과 기간, 매출 회수 속도를 함께 보세요.");

            BalanceGuideUI.DrawAutoCheck(
                "점수 난이도",
                summary.BestAverageScore < 70f || summary.BestAverageScore >= 95f,
                summary.BestAverageScore < 70f
                    ? "최고 평균 점수가 낮아 A/S급 프로젝트 검증이 어렵습니다. 보고서 등급 효과, 직원 능력치, 일일 업무 보너스를 확인하세요."
                    : summary.BestAverageScore >= 95f
                        ? "최고 평균 점수가 매우 높습니다. 초반부터 S급이 너무 쉬운지 확인하세요."
                        : "프로젝트 평균 점수가 중간 범위에 있습니다.");

            BalanceGuideUI.DrawAutoCheck(
                "직원 상태",
                summary.FinalFatigue >= 80f || summary.FinalDesire < 40f || summary.FinalLoyalty < 40f,
                $"마지막 주 평균 상태: 의욕 {summary.FinalDesire:0.0}, 피로 {summary.FinalFatigue:0.0}, 충성 {summary.FinalLoyalty:0.0}. 피로 80 이상/의욕 40 미만이면 보고서 리스크가 커집니다.");

            BalanceGuideUI.DrawAutoCheck(
                "매출/비용 비율",
                summary.TotalExpense > summary.TotalIncome,
                summary.TotalExpense > summary.TotalIncome
                    ? "총 비용이 총 매출보다 큽니다. 회사가 성장해도 장기적으로 버티기 어렵습니다."
                    : "총 매출이 총 비용을 상회합니다.");
        }

        void DrawGraphs()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("흐름 그래프", EditorStyles.boldLabel);
            DrawGraph("자금 흐름", _weeks.Select(w => new GraphPoint(w.Week, w.EndGold)).ToList(), new Color(0.25f, 0.65f, 1f));
            DrawDualGraph(
                "주간 매출 / 비용",
                _weeks.Select(w => new GraphPoint(w.Week, w.Income)).ToList(), new Color(0.35f, 0.85f, 0.45f),
                _weeks.Select(w => new GraphPoint(w.Week, w.Expense)).ToList(), new Color(1f, 0.55f, 0.35f));
            DrawTripleGraph(
                "프로젝트 점수",
                _weeks.Select(w => new GraphPoint(w.Week, w.Quality)).ToList(), new Color(0.95f, 0.65f, 0.25f),
                _weeks.Select(w => new GraphPoint(w.Week, w.Stability)).ToList(), new Color(0.25f, 0.75f, 0.95f),
                _weeks.Select(w => new GraphPoint(w.Week, w.Charm)).ToList(), new Color(0.9f, 0.45f, 0.95f),
                "완성도", "안정성", "매력도");
            DrawTripleGraph(
                "직원 상태",
                _weeks.Select(w => new GraphPoint(w.Week, w.Desire)).ToList(), new Color(0.35f, 0.85f, 0.45f),
                _weeks.Select(w => new GraphPoint(w.Week, w.Fatigue)).ToList(), new Color(1f, 0.55f, 0.35f),
                _weeks.Select(w => new GraphPoint(w.Week, w.Loyalty)).ToList(), new Color(0.55f, 0.65f, 1f),
                "의욕", "피로", "충성");
        }

        void DrawTimeline()
        {
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Week / Gold / Income / Expense / Project / Staff / Note", EditorStyles.miniBoldLabel);
                foreach (OverviewWeekSnapshot week in _weeks)
                {
                    string line = $"{week.Week,2}주차  자금 {FormatGold(week.EndGold),10}  매출 {FormatGold(week.Income),9}  비용 {FormatGold(week.Expense),9}  "
                                + $"점수 {week.Quality:0}/{week.Stability:0}/{week.Charm:0}  상태 D{week.Desire:0} F{week.Fatigue:0} L{week.Loyalty:0}  {week.Note}";
                    EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
                }
            }
        }

        void DrawGraph(string title, List<GraphPoint> points, Color color)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetRect(10f, 120f, GUILayout.ExpandWidth(true));
                DrawGraphBackground(rect);
                DrawSeries(rect, points, color, GetMin(points), GetMax(points));
            }
        }

        void DrawDualGraph(string title, List<GraphPoint> a, Color aColor, List<GraphPoint> b, Color bColor)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetRect(10f, 120f, GUILayout.ExpandWidth(true));
                DrawGraphBackground(rect);
                float min = Mathf.Min(GetMin(a), GetMin(b));
                float max = Mathf.Max(GetMax(a), GetMax(b));
                DrawSeries(rect, a, aColor, min, max);
                DrawSeries(rect, b, bColor, min, max);
                DrawLegend(("매출", aColor), ("비용", bColor));
            }
        }

        void DrawTripleGraph(string title, List<GraphPoint> a, Color aColor, List<GraphPoint> b, Color bColor, List<GraphPoint> c, Color cColor, string aLabel, string bLabel, string cLabel)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                Rect rect = GUILayoutUtility.GetRect(10f, 120f, GUILayout.ExpandWidth(true));
                DrawGraphBackground(rect);
                float min = Mathf.Min(GetMin(a), Mathf.Min(GetMin(b), GetMin(c)));
                float max = Mathf.Max(GetMax(a), Mathf.Max(GetMax(b), GetMax(c)));
                DrawSeries(rect, a, aColor, min, max);
                DrawSeries(rect, b, bColor, min, max);
                DrawSeries(rect, c, cColor, min, max);
                DrawLegend((aLabel, aColor), (bLabel, bColor), (cLabel, cColor));
            }
        }

        static void DrawGraphBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            for (int i = 1; i < 4; i++)
            {
                float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
                Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
            }
            Handles.EndGUI();
        }

        static void DrawSeries(Rect rect, List<GraphPoint> points, Color color, float min, float max)
        {
            if (points == null || points.Count < 2)
                return;

            if (Mathf.Approximately(min, max))
            {
                min -= 1f;
                max += 1f;
            }

            Vector3[] vertices = new Vector3[points.Count];
            float minWeek = points[0].Week;
            float maxWeek = points[points.Count - 1].Week;
            if (Mathf.Approximately(minWeek, maxWeek))
                maxWeek = minWeek + 1f;

            for (int i = 0; i < points.Count; i++)
            {
                float x = Mathf.Lerp(rect.xMin + 6f, rect.xMax - 6f, Mathf.InverseLerp(minWeek, maxWeek, points[i].Week));
                float y = Mathf.Lerp(rect.yMax - 8f, rect.yMin + 8f, Mathf.InverseLerp(min, max, points[i].Value));
                vertices[i] = new Vector3(x, y);
            }

            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(2.5f, vertices);
            Handles.EndGUI();
        }

        static void DrawLegend(params (string Label, Color Color)[] items)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach ((string label, Color color) in items)
                {
                    Rect swatch = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(12f));
                    EditorGUI.DrawRect(swatch, color);
                    EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(54f));
                }
            }
        }

        void RefreshAssets()
        {
            _employees.Clear();
            _projects.Clear();
            _employees.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot));
            _projects.AddRange(QAAssetUtility.FindAssetEntriesByType<ProjectSO>(ProjectRoot));
        }

        void RunSimulation()
        {
            _weeks.Clear();
            if (_employees.Count == 0)
                RefreshAssets();

            List<ProjectSize> route = BuildProjectRoute();
            int routeIndex = 0;
            int gold = _initialGold;
            int staffCount = Mathf.Max(1, _initialStaffCount);
            float desire = AverageEmployeeValue(e => e.desire, 50f);
            float fatigue = AverageEmployeeValue(e => e.fatigue, 20f);
            float loyalty = AverageEmployeeValue(e => e.loyalty, 50f);
            float quality = 0f;
            float stability = 0f;
            float charm = 0f;
            int projectWeeksRemaining = 0;
            int projectWeeksTotal = 0;
            int projectProgress = 0;
            ProjectSize activeProjectSize = ProjectSize.Small;
            string activeProjectName = string.Empty;
            var liveProjects = new List<LiveProject>();

            for (int week = 1; week <= _simulationWeeks; week++)
            {
                int startGold = gold;
                int income = 0;
                int expense = 0;
                string note = string.Empty;

                if (projectWeeksRemaining <= 0 && routeIndex < route.Count)
                {
                    activeProjectSize = route[routeIndex++];
                    ProjectSO project = GetProject(activeProjectSize);
                    activeProjectName = project != null ? project.Name : activeProjectSize.ToString();
                    projectWeeksTotal = Mathf.Max(1, Mathf.CeilToInt(GetDurationDays(activeProjectSize) / 5f));
                    projectWeeksRemaining = projectWeeksTotal;
                    projectProgress = 0;
                    int projectCost = GetProjectCost(activeProjectSize);
                    expense += projectCost;
                    note = $"{activeProjectName} 시작(-{projectCost})";
                }

                if (projectWeeksRemaining > 0)
                {
                    RoleWeekScore planner = CalcRoleWeekScore(Role.PLANNER, activeProjectSize, Mathf.RoundToInt(desire), Mathf.RoundToInt(loyalty));
                    RoleWeekScore programmer = CalcRoleWeekScore(Role.PROGRAMMER, activeProjectSize, Mathf.RoundToInt(desire), Mathf.RoundToInt(loyalty));
                    RoleWeekScore artist = CalcRoleWeekScore(Role.ARTIST, activeProjectSize, Mathf.RoundToInt(desire), Mathf.RoundToInt(loyalty));

                    quality = Mathf.Clamp(planner.Score + _dailyQuestBonus, 0f, 100f);
                    stability = Mathf.Clamp(programmer.Score + _dailyQuestBonus, 0f, 100f);
                    charm = Mathf.Clamp(artist.Score + _dailyQuestBonus, 0f, 100f);

                    projectWeeksRemaining--;
                    projectProgress = Mathf.RoundToInt((projectWeeksTotal - projectWeeksRemaining) / (float)projectWeeksTotal * 100f);
                    fatigue = Mathf.Clamp(fatigue + 5f + AverageGradeFatigue(planner.Grade, programmer.Grade, artist.Grade) * 0.25f, 0f, 100f);
                    desire = Mathf.Clamp(desire - 1f, 0f, 100f);

                    if (projectWeeksRemaining <= 0)
                    {
                        float averageScore = (quality + stability + charm) / 3f;
                        char grade = CalcProjectGrade(averageScore);
                        liveProjects.Add(new LiveProject(activeProjectSize, quality, stability, charm));
                        loyalty = Mathf.Clamp(loyalty + PerkPolicy.CalcCompletionLoyaltyDelta(activeProjectSize, grade), 0f, 100f);
                        desire = Mathf.Clamp(desire + (grade switch { 'S' => 8, 'A' => 4, 'B' => 1, _ => -4 }), 0f, 100f);
                        note = string.IsNullOrEmpty(note) ? $"{activeProjectName} 출시({grade})" : $"{note}, 출시({grade})";
                    }
                }
                else
                {
                    fatigue = Mathf.Clamp(fatigue - 3f, 0f, 100f);
                    desire = Mathf.Clamp(desire + 1f, 0f, 100f);
                }

                income += CalcWeeklyIncome(liveProjects);
                expense += CalcWeeklyStaffCost(staffCount) + _officeWeeklyCost + _weeklyRecruitCost + _weeklyTrainingCost;
                gold += income - expense;

                _weeks.Add(new OverviewWeekSnapshot
                {
                    Week = week,
                    StartGold = startGold,
                    Income = income,
                    Expense = expense,
                    EndGold = gold,
                    Quality = quality,
                    Stability = stability,
                    Charm = charm,
                    Desire = desire,
                    Fatigue = fatigue,
                    Loyalty = loyalty,
                    Progress = projectProgress,
                    ReleasedProjects = liveProjects.Count,
                    ActiveProjectSize = projectWeeksTotal > 0 ? activeProjectSize : ProjectSize.Small,
                    ReleasedThisWeek = note.Contains("출시"),
                    Note = string.IsNullOrEmpty(note) ? "-" : note
                });
            }

            Repaint();
        }

        RoleWeekScore CalcRoleWeekScore(Role role, ProjectSize size, int desire, int loyalty)
        {
            int maxCount = Mathf.Max(1, GetProject(size)?.maxEmployeePerPart ?? 1);
            List<EmployeeImmutableData> members = _employees
                .Select(e => e.Asset)
                .Where(e => e != null && e.role == role)
                .OrderByDescending(e => e.ability)
                .Take(maxCount)
                .ToList();

            if (members.Count == 0)
                return new RoleWeekScore(0f, 3);

            float total = 0f;
            int totalGrade = 0;
            foreach (EmployeeImmutableData member in members)
            {
                total += CalcEmployeeReportResult(member, desire, loyalty, out int grade);
                totalGrade += grade;
            }

            return new RoleWeekScore(total / members.Count, Mathf.RoundToInt(totalGrade / (float)members.Count));
        }

        float CalcEmployeeReportResult(EmployeeImmutableData employee, int desire, int loyalty, out int grade)
        {
            float reportScore = ReportPolicy.CalcScore(employee, desire);
            grade = ReportPolicy.CalcGrade(reportScore);

            TraitStat[] stats = ReportPolicy.GetRoleStats(employee.role);
            if (stats.Length == 0)
                return 0f;

            int ability = ReportPolicy.CalcLoyaltyAdjustedAbility(employee.ability, loyalty);
            var scores = new Dictionary<TraitStat, float>
            {
                [stats[0]] = ability,
                [stats[1]] = ability,
                [stats[2]] = ability,
            };

            int mainDelta = grade == 1 ? 12 : grade == 2 ? 8 : 4;
            int subDelta = grade == 1 ? 6 : grade == 2 ? 4 : 2;
            int riskDelta = grade == 1 ? -6 : grade == 2 ? -4 : -2;

            ApplyTrait(scores, employee.mainTrait, mainDelta);
            ApplyTrait(scores, employee.subTrait, subDelta);
            ApplyTrait(scores, employee.riskTrait, riskDelta);

            return (float)scores.Values.Select(v => Mathf.Clamp(v, 0f, 100f)).Average();
        }

        static void ApplyTrait(Dictionary<TraitStat, float> scores, Trait trait, int delta)
        {
            foreach (TraitStat stat in TraitTable.Get(trait).affectedStats)
            {
                if (scores.ContainsKey(stat))
                    scores[stat] += delta;
            }
        }

        int CalcWeeklyIncome(List<LiveProject> liveProjects)
        {
            int income = 0;
            foreach (LiveProject project in liveProjects)
            {
                for (int day = 0; day < 5; day++)
                {
                    int sales = PerkPolicy.CalcDailySales(project.Size, project.Quality, project.Stability, project.Charm, project.Retention, _companyPopularity);
                    income += PerkPolicy.CalcDailyGold(project.Size, sales);
                    project.Retention = Mathf.Clamp01(project.Retention - PerkPolicy.RETENTION_DECAY);
                }
            }
            return income;
        }

        int CalcWeeklyStaffCost(int staffCount)
        {
            float averageSalary = AverageEmployeeValue(e => e.weekSalary, 0f);
            return Mathf.RoundToInt(averageSalary * Mathf.Max(1, staffCount));
        }

        OverviewSummary BuildSummary()
        {
            OverviewWeekSnapshot last = _weeks[_weeks.Count - 1];
            int firstDeficit = _weeks.FirstOrDefault(w => w.EndGold < 0).Week;
            return new OverviewSummary
            {
                FinalGold = last.EndGold,
                MinGold = _weeks.Min(w => w.EndGold),
                TotalIncome = _weeks.Sum(w => w.Income),
                TotalExpense = _weeks.Sum(w => w.Expense),
                FirstDeficitWeek = firstDeficit,
                BestAverageScore = _weeks.Max(w => (w.Quality + w.Stability + w.Charm) / 3f),
                FinalDesire = last.Desire,
                FinalFatigue = last.Fatigue,
                FinalLoyalty = last.Loyalty,
                ReleasedProjectCount = last.ReleasedProjects,
                HasMediumProject = _weeks.Any(w => w.ActiveProjectSize == ProjectSize.Medium),
                HasLargeProject = _weeks.Any(w => w.ActiveProjectSize == ProjectSize.Large),
            };
        }

        PlaytimeSummary BuildPlaytimeSummary(OverviewSummary summary)
        {
            int secondsPerWeek = CalcSecondsPerWeek();
            return new PlaytimeSummary
            {
                SecondsPerWeek = secondsPerWeek,
                FirstReleaseSeconds = WeekToSeconds(FindFirstWeek(w => w.ReleasedThisWeek), secondsPerWeek),
                MediumStartSeconds = WeekToSeconds(FindFirstWeek(w => w.ActiveProjectSize == ProjectSize.Medium), secondsPerWeek),
                LargeStartSeconds = WeekToSeconds(FindFirstWeek(w => w.ActiveProjectSize == ProjectSize.Large), secondsPerWeek),
                DeficitSeconds = WeekToSeconds(summary.FirstDeficitWeek, secondsPerWeek)
            };
        }

        int CalcSecondsPerWeek()
        {
            int daySeconds = Mathf.Max(1, _dayBaseSeconds + _dialoguesPerDay * _dialogueSeconds);
            return daySeconds * 5 + Mathf.Max(1, _nightManagementSeconds);
        }

        int FindFirstWeek(Func<OverviewWeekSnapshot, bool> predicate)
        {
            foreach (OverviewWeekSnapshot week in _weeks)
            {
                if (predicate(week))
                    return week.Week;
            }

            return -1;
        }

        static int WeekToSeconds(int week, int secondsPerWeek)
        {
            return week <= 0 ? -1 : week * secondsPerWeek;
        }

        string DescribeAtPlaytime(int seconds, int secondsPerWeek)
        {
            if (_weeks.Count == 0 || secondsPerWeek <= 0)
                return "시뮬레이션 결과 없음";

            int weekIndex = Mathf.Clamp(Mathf.CeilToInt(seconds / (float)secondsPerWeek) - 1, 0, _weeks.Count - 1);
            OverviewWeekSnapshot week = _weeks[weekIndex];
            string projectState = week.ReleasedProjects > 0 ? $"서비스 프로젝트 {week.ReleasedProjects}개" : "출시 전";
            return $"{week.Week}주차 / {projectState} / 자금 {FormatGold(week.EndGold)} / 점수 {week.Quality:0}/{week.Stability:0}/{week.Charm:0}";
        }

        static string FormatMilestone(int seconds)
        {
            return seconds < 0 ? "미도달" : FormatDuration(seconds);
        }

        static string FormatDuration(int seconds)
        {
            if (seconds < 0)
                return "미도달";

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours >= 1d)
                return $"{(int)span.TotalHours}시간 {span.Minutes}분";

            return $"{span.Minutes}분 {span.Seconds}초";
        }

        List<ProjectSize> BuildProjectRoute()
        {
            var route = new List<ProjectSize>();
            AddRoute(route, ProjectSize.Small, _smallProjectCount);
            AddRoute(route, ProjectSize.Medium, _mediumProjectCount);
            AddRoute(route, ProjectSize.Large, _largeProjectCount);
            return route;
        }

        static void AddRoute(List<ProjectSize> route, ProjectSize size, int count)
        {
            for (int i = 0; i < Mathf.Max(0, count); i++)
                route.Add(size);
        }

        ProjectSO GetProject(ProjectSize size)
            => _projects.Select(p => p.Asset).FirstOrDefault(p => p != null && p.scale == size);

        int GetDurationDays(ProjectSize size)
        {
            ProjectSO project = GetProject(size);
            if (project != null && project.durationDays > 0)
                return project.durationDays;

            return size switch
            {
                ProjectSize.Medium => 30,
                ProjectSize.Large => 40,
                _ => 20,
            };
        }

        int GetProjectCost(ProjectSize size)
        {
            ProjectSO project = GetProject(size);
            if (project != null && project.requiredCost > 0)
                return project.requiredCost;

            return size switch
            {
                ProjectSize.Medium => 35000,
                ProjectSize.Large => 100000,
                _ => 10000,
            };
        }

        float AverageEmployeeValue(Func<EmployeeImmutableData, int> selector, float fallback)
        {
            List<EmployeeImmutableData> valid = _employees.Select(e => e.Asset).Where(e => e != null).ToList();
            return valid.Count == 0 ? fallback : (float)valid.Average(e => selector(e));
        }

        static float AverageGradeFatigue(int a, int b, int c)
            => (GradeFatigue(a) + GradeFatigue(b) + GradeFatigue(c)) / 3f;

        static int GradeFatigue(int grade) => grade switch
        {
            1 => 5,
            2 => 8,
            _ => 10,
        };

        static char CalcProjectGrade(float averageScore)
        {
            if (averageScore >= 90f) return 'S';
            if (averageScore >= 75f) return 'A';
            if (averageScore >= 55f) return 'B';
            return 'C';
        }

        static float GetMin(List<GraphPoint> points) => points.Count == 0 ? 0f : points.Min(p => p.Value);
        static float GetMax(List<GraphPoint> points) => points.Count == 0 ? 1f : points.Max(p => p.Value);
        static string FormatGold(int value) => value.ToString("N0") + " G";

        struct OverviewWeekSnapshot
        {
            public int Week;
            public int StartGold;
            public int Income;
            public int Expense;
            public int EndGold;
            public float Quality;
            public float Stability;
            public float Charm;
            public float Desire;
            public float Fatigue;
            public float Loyalty;
            public int Progress;
            public int ReleasedProjects;
            public ProjectSize ActiveProjectSize;
            public bool ReleasedThisWeek;
            public string Note;
        }

        struct PlaytimeSummary
        {
            public int SecondsPerWeek;
            public int FirstReleaseSeconds;
            public int MediumStartSeconds;
            public int LargeStartSeconds;
            public int DeficitSeconds;
        }

        struct OverviewSummary
        {
            public int FinalGold;
            public int MinGold;
            public int TotalIncome;
            public int TotalExpense;
            public int FirstDeficitWeek;
            public float BestAverageScore;
            public float FinalDesire;
            public float FinalFatigue;
            public float FinalLoyalty;
            public int ReleasedProjectCount;
            public bool HasMediumProject;
            public bool HasLargeProject;
        }

        readonly struct GraphPoint
        {
            public readonly float Week;
            public readonly float Value;

            public GraphPoint(float week, float value)
            {
                Week = week;
                Value = value;
            }
        }

        readonly struct RoleWeekScore
        {
            public readonly float Score;
            public readonly int Grade;

            public RoleWeekScore(float score, int grade)
            {
                Score = score;
                Grade = grade;
            }
        }

        sealed class LiveProject
        {
            public readonly ProjectSize Size;
            public readonly float Quality;
            public readonly float Stability;
            public readonly float Charm;
            public float Retention;

            public LiveProject(ProjectSize size, float quality, float stability, float charm)
            {
                Size = size;
                Quality = quality;
                Stability = stability;
                Charm = charm;
                Retention = 1f;
            }
        }
    }
}
