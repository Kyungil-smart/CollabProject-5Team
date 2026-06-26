using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class ProjectBalanceSimulatorWindow : EditorWindow
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const string ReportRoot = "Assets/Project/DB/Report";

        private readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        private readonly List<AssetEntry<ReportSO>> _reports = new();
        private readonly int[] _plannerSlotIds = { -1, -1, -1 };
        private readonly int[] _artistSlotIds = { -1, -1, -1 };
        private readonly int[] _programmerSlotIds = { -1, -1, -1 };

        private Vector2 _scrollPosition;
        private ProjectSize _projectSize = ProjectSize.Small;
        private int _startRepo = 1;
        private float _retentionFactor = 1f;
        private int _companyPopularity;
        private int _plannerQuestBonus;
        private int _artistQuestBonus;
        private int _programmerQuestBonus;
        private bool _usePlayModeEmployees;

        [MenuItem("Tools/Simulation/Project Balance Simulator")]
        public static void Open()
        {
            ProjectBalanceSimulatorWindow window = GetWindow<ProjectBalanceSimulatorWindow>("Project Balance");
            window.minSize = new Vector2(620f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawInputPanel();
            DrawTeamSlots();
            DrawResult(SimulateTeam(GetMaxEmployeePerPart(_projectSize)));
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    RefreshAssets();

                GUILayout.FlexibleSpace();

                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                _usePlayModeEmployees = GUILayout.Toggle(_usePlayModeEmployees, "Play Mode 직원", EditorStyles.toolbarButton, GUILayout.Width(110f));
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawInputPanel()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("프로젝트 밸런스 시뮬레이터", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "프로젝트 규모와 직군별 인원 조합에 따라 예상 완성도, 안정성, 매력도, 등급, 판매량, 매출을 비교합니다.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _projectSize = (ProjectSize)EditorGUILayout.EnumPopup("프로젝트 규모", _projectSize);
                _startRepo = EditorGUILayout.Popup("보고서 구분", _startRepo == 1 ? 0 : 1, new[] { "1주차 보고서(startRepo=1)", "랜덤 보고서(startRepo=0)" }) == 0 ? 1 : 0;
                _companyPopularity = EditorGUILayout.IntSlider("회사 인기", _companyPopularity, 0, 300);
                _retentionFactor = EditorGUILayout.Slider("유지력 계수", _retentionFactor, 0f, 1f);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("일일 퀘스트 주간 보너스", EditorStyles.boldLabel);
                _plannerQuestBonus = EditorGUILayout.IntSlider("기획 보너스", _plannerQuestBonus, 0, 100);
                _artistQuestBonus = EditorGUILayout.IntSlider("아트 보너스", _artistQuestBonus, 0, 100);
                _programmerQuestBonus = EditorGUILayout.IntSlider("개발 보너스", _programmerQuestBonus, 0, 100);

                using (new EditorGUI.DisabledScope(!Application.isPlaying || QuestManager.Instance == null))
                {
                    if (GUILayout.Button("Play Mode 현재 일퀘 보너스 불러오기"))
                    {
                        _plannerQuestBonus = QuestManager.Instance.GetWeeklyBonus(Role.PLANNER);
                        _artistQuestBonus = QuestManager.Instance.GetWeeklyBonus(Role.ARTIST);
                        _programmerQuestBonus = QuestManager.Instance.GetWeeklyBonus(Role.PROGRAMMER);
                    }
                }

                if (_usePlayModeEmployees)
                {
                    EditorGUILayout.HelpBox(
                        "Play Mode 직원 사용 중입니다. 현재 고용 직원의 성장 능력치, 의욕, 피로도, 충성도를 기준으로 계산합니다.",
                        MessageType.Info);
                }
            }
        }

        private void DrawTeamSlots()
        {
            int maxPerPart = GetMaxEmployeePerPart(_projectSize);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"직군별 최대 인원: {maxPerPart}명", EditorStyles.boldLabel);
                DrawRoleSlots(Role.PLANNER, _plannerSlotIds, maxPerPart);
                DrawRoleSlots(Role.ARTIST, _artistSlotIds, maxPerPart);
                DrawRoleSlots(Role.PROGRAMMER, _programmerSlotIds, maxPerPart);
            }
        }

        private void DrawRoleSlots(Role role, int[] slotIds, int maxPerPart)
        {
            List<EmployeeSnapshot> employees = GetEmployeesForRole(role);
            string[] options = new[] { "미배치" }
                .Concat(employees.Select(e => $"{e.So.id} / {e.So.Name} / 능력 {e.StatAbility} / 의욕 {e.Desire}"))
                .ToArray();
            int[] optionIds = new[] { -1 }
                .Concat(employees.Select(e => e.So.id))
                .ToArray();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(GetRoleLabel(role), EditorStyles.boldLabel);

            for (int i = 0; i < slotIds.Length; i++)
            {
                if (i >= maxPerPart)
                {
                    slotIds[i] = -1;
                    continue;
                }

                int currentIndex = Array.IndexOf(optionIds, slotIds[i]);
                if (currentIndex < 0)
                    currentIndex = 0;

                int nextIndex = EditorGUILayout.Popup($"{i + 1}번 슬롯", currentIndex, options);
                slotIds[i] = optionIds[nextIndex];
            }
        }

        private TeamSimulationResult SimulateTeam(int maxPerPart)
        {
            RoleSimulationResult planner = SimulateRole(Role.PLANNER, GetSelectedTeamMembers(Role.PLANNER, maxPerPart));
            RoleSimulationResult artist = SimulateRole(Role.ARTIST, GetSelectedTeamMembers(Role.ARTIST, maxPerPart));
            RoleSimulationResult programmer = SimulateRole(Role.PROGRAMMER, GetSelectedTeamMembers(Role.PROGRAMMER, maxPerPart));

            float baseQuality = planner.HasReport ? planner.Score : 0f;
            float baseCharm = artist.HasReport ? artist.Score : 0f;
            float baseStability = programmer.HasReport ? programmer.Score : 0f;
            float quality = baseQuality + _plannerQuestBonus;
            float charm = baseCharm + _artistQuestBonus;
            float stability = baseStability + _programmerQuestBonus;
            float totalScore = (quality + stability + charm) / 3f;
            int dailySales = PerkPolicy.CalcDailySales(_projectSize, quality, stability, charm, _retentionFactor, _companyPopularity);
            int dailyGold = PerkPolicy.CalcDailyGold(_projectSize, dailySales);
            int weeklyCost = PerkPolicy.CalcWeeklyCost(_projectSize);

            return new TeamSimulationResult(
                planner,
                artist,
                programmer,
                baseQuality,
                baseStability,
                baseCharm,
                _plannerQuestBonus,
                _programmerQuestBonus,
                _artistQuestBonus,
                quality,
                stability,
                charm,
                totalScore,
                dailySales,
                dailyGold,
                weeklyCost);
        }

        private RoleSimulationResult SimulateRole(Role role, List<EmployeeSnapshot> members)
        {
            var previews = new List<TeamReportPreview>();

            foreach (EmployeeSnapshot member in members)
            {
                int grade = ReportPolicy.CalcGrade(CalcReportScore(member.ReportScoreAbility, member.Desire));
                List<ReportCandidatePreview> candidates = FindCandidateReports(member, grade);

                foreach (ReportCandidatePreview candidate in candidates)
                {
                    float score = CalcRoleAverage(member, candidate.Report, grade);
                    previews.Add(new TeamReportPreview(member, candidate.Report, candidate.MatchSource, grade, score));
                }
            }

            TeamReportPreview best = previews
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Report.id)
                .FirstOrDefault();

            return new RoleSimulationResult(role, members.Count, previews.Count, best);
        }

        private void DrawResult(TeamSimulationResult result)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("예상 결과", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"완성도(기획): {result.Quality:0.#} = 보고서 {result.BaseQuality:0.#} + 일퀘 {result.PlannerQuestBonus}");
                EditorGUILayout.LabelField(
                    $"안정성(개발): {result.Stability:0.#} = 보고서 {result.BaseStability:0.#} + 일퀘 {result.ProgrammerQuestBonus}");
                EditorGUILayout.LabelField(
                    $"매력도(아트): {result.Charm:0.#} = 보고서 {result.BaseCharm:0.#} + 일퀘 {result.ArtistQuestBonus}");
                EditorGUILayout.LabelField($"프로젝트 평균 점수: {result.TotalScore:0.#} / 예상 등급: {GetProjectGrade(result.TotalScore)}");
                EditorGUILayout.LabelField($"예상 일일 판매량: {result.DailySales:N0} / 예상 일일 매출: {result.DailyGold:N0}G / 주간 유지비: {result.WeeklyCost:N0}G");

                DrawRoleResult(result.Planner);
                DrawRoleResult(result.Artist);
                DrawRoleResult(result.Programmer);
            }
        }

        private void DrawRoleResult(RoleSimulationResult result)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"{GetRoleLabel(result.Role)}: 배치 {result.MemberCount}명 / 후보 {result.CandidateCount}개", EditorStyles.boldLabel);

                if (!result.HasReport)
                {
                    EditorGUILayout.HelpBox("배치된 직원이 없거나 선택 가능한 보고서가 없습니다.", MessageType.Warning);
                    return;
                }

                TeamReportPreview best = result.BestReport;
                EditorGUILayout.LabelField($"자동 선택: {best.Employee.So.Name} - {best.Report.title}");
                EditorGUILayout.LabelField($"특성 {GetTraitLabel(best.Report.trait)} / {best.MatchSource} 매칭 / grade={best.Grade} / 점수 {best.Score:0.#}");

                if (GUILayout.Button("보고서 SO 선택", GUILayout.Width(110f)))
                {
                    Selection.activeObject = best.Report;
                    EditorGUIUtility.PingObject(best.Report);
                }
            }
        }

        private List<EmployeeSnapshot> GetSelectedTeamMembers(Role role, int maxPerPart)
        {
            int[] slotIds = GetSlotIds(role);
            List<EmployeeSnapshot> employees = GetEmployeesForRole(role);
            var selected = new List<EmployeeSnapshot>();

            for (int i = 0; i < maxPerPart && i < slotIds.Length; i++)
            {
                int id = slotIds[i];
                if (id < 0)
                    continue;

                EmployeeSnapshot employee = employees.FirstOrDefault(e => e.So.id == id);
                if (employee.IsValid && selected.All(e => e.So.id != employee.So.id))
                    selected.Add(employee);
            }

            return selected;
        }

        private List<ReportCandidatePreview> FindCandidateReports(EmployeeSnapshot employee, int grade)
        {
            var results = new List<ReportCandidatePreview>();
            AddCandidateReports(results, employee, employee.So.mainTrait, "대표", grade);
            AddCandidateReports(results, employee, employee.So.riskTrait, "리스크", grade);
            AddCandidateReports(results, employee, employee.So.subTrait, "보조", grade);
            return results;
        }

        private void AddCandidateReports(
            List<ReportCandidatePreview> results,
            EmployeeSnapshot employee,
            Trait trait,
            string matchSource,
            int grade)
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

                results.Add(new ReportCandidatePreview(report, matchSource));
            }
        }

        private float CalcRoleAverage(EmployeeSnapshot employee, ReportSO report, int grade)
        {
            TraitStat[] stats = ReportPolicy.GetRoleStats(employee.So.role);
            if (stats.Length == 0)
                return 0f;

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

            float traitWeight = QATraitUtility.TryGetTraitData(report.trait, out TraitData data)
                ? data.score * 2f
                : 0f;

            return (Mathf.Clamp(scores[stats[0]], 0f, 100f)
                + Mathf.Clamp(scores[stats[1]], 0f, 100f)
                + Mathf.Clamp(scores[stats[2]], 0f, 100f)
                + traitWeight) / 3f;
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
                .Select(EmployeeSnapshot.FromAsset)
                .ToList();
        }

        private int[] GetSlotIds(Role role)
        {
            return role switch
            {
                Role.PLANNER => _plannerSlotIds,
                Role.ARTIST => _artistSlotIds,
                Role.PROGRAMMER => _programmerSlotIds,
                _ => _plannerSlotIds
            };
        }

        private static int GetMaxEmployeePerPart(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => 2,
                ProjectSize.Large => 3,
                _ => 1
            };
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

        private static string GetProjectGrade(float score)
        {
            return score switch
            {
                > 90f => "S",
                > 75f => "A",
                > 50f => "B",
                _ => "C"
            };
        }

        private static string GetRoleLabel(Role role)
        {
            return role switch
            {
                Role.PLANNER => "기획",
                Role.ARTIST => "아트",
                Role.PROGRAMMER => "개발",
                _ => role.ToString()
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

        private readonly struct TeamSimulationResult
        {
            public RoleSimulationResult Planner { get; }
            public RoleSimulationResult Artist { get; }
            public RoleSimulationResult Programmer { get; }
            public float BaseQuality { get; }
            public float BaseStability { get; }
            public float BaseCharm { get; }
            public int PlannerQuestBonus { get; }
            public int ProgrammerQuestBonus { get; }
            public int ArtistQuestBonus { get; }
            public float Quality { get; }
            public float Stability { get; }
            public float Charm { get; }
            public float TotalScore { get; }
            public int DailySales { get; }
            public int DailyGold { get; }
            public int WeeklyCost { get; }

            public TeamSimulationResult(
                RoleSimulationResult planner,
                RoleSimulationResult artist,
                RoleSimulationResult programmer,
                float baseQuality,
                float baseStability,
                float baseCharm,
                int plannerQuestBonus,
                int programmerQuestBonus,
                int artistQuestBonus,
                float quality,
                float stability,
                float charm,
                float totalScore,
                int dailySales,
                int dailyGold,
                int weeklyCost)
            {
                Planner = planner;
                Artist = artist;
                Programmer = programmer;
                BaseQuality = baseQuality;
                BaseStability = baseStability;
                BaseCharm = baseCharm;
                PlannerQuestBonus = plannerQuestBonus;
                ProgrammerQuestBonus = programmerQuestBonus;
                ArtistQuestBonus = artistQuestBonus;
                Quality = quality;
                Stability = stability;
                Charm = charm;
                TotalScore = totalScore;
                DailySales = dailySales;
                DailyGold = dailyGold;
                WeeklyCost = weeklyCost;
            }
        }

        private readonly struct RoleSimulationResult
        {
            public Role Role { get; }
            public int MemberCount { get; }
            public int CandidateCount { get; }
            public TeamReportPreview BestReport { get; }
            public bool HasReport => BestReport.Report != null;
            public float Score => HasReport ? BestReport.Score : 0f;

            public RoleSimulationResult(Role role, int memberCount, int candidateCount, TeamReportPreview bestReport)
            {
                Role = role;
                MemberCount = memberCount;
                CandidateCount = candidateCount;
                BestReport = bestReport;
            }
        }

        private readonly struct TeamReportPreview
        {
            public EmployeeSnapshot Employee { get; }
            public ReportSO Report { get; }
            public string MatchSource { get; }
            public int Grade { get; }
            public float Score { get; }

            public TeamReportPreview(EmployeeSnapshot employee, ReportSO report, string matchSource, int grade, float score)
            {
                Employee = employee;
                Report = report;
                MatchSource = matchSource;
                Grade = grade;
                Score = score;
            }
        }

        private readonly struct EmployeeSnapshot
        {
            public EmployeeImmutableData So { get; }
            public int ReportScoreAbility { get; }
            public int StatAbility { get; }
            public int Desire { get; }
            public int Loyalty { get; }
            public bool IsValid => So != null;

            private EmployeeSnapshot(
                EmployeeImmutableData so,
                int reportScoreAbility,
                int statAbility,
                int desire,
                int loyalty)
            {
                So = so;
                ReportScoreAbility = reportScoreAbility;
                StatAbility = statAbility;
                Desire = desire;
                Loyalty = loyalty;
            }

            public static EmployeeSnapshot FromAsset(AssetEntry<EmployeeImmutableData> entry)
            {
                EmployeeImmutableData so = entry.Asset;
                return new EmployeeSnapshot(so, so.ability, so.ability, so.desire, so.loyalty);
            }

            public static EmployeeSnapshot FromRuntime(Employee employee)
            {
                return new EmployeeSnapshot(
                    employee.so,
                    employee.so.ability,
                    employee.MutableData.ability,
                    employee.MutableData.desire,
                    employee.MutableData.loyalty);
            }
        }
    }
}
