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

        private enum EmployeePickMode
        {
            Low,
            Average,
            Strong
        }

        private Vector2 _scrollPosition;
        private ProjectSize _projectSize = ProjectSize.Small;
        private int _startRepo = 1;
        private float _retentionFactor = 1f;
        private int _companyPopularity;
        private int _plannerQuestBonus;
        private int _artistQuestBonus;
        private int _programmerQuestBonus;
        private bool _useManualTeamState;
        private int _manualTeamDesire = 70;
        private int _manualTeamFatigue = 20;
        private int _manualTeamLoyalty = 70;
        private bool _usePlayModeEmployees;
        private bool _hasBaseline;
        private bool _showScenarioMatrix;
        private bool _showPresetControls;
        private bool _showTargetSettings;
        private TeamSimulationResult _baselineResult;
        private float _targetTotalMin = 50f;
        private float _targetTotalMax = 90f;
        private float _targetQualityMin = 45f;
        private float _targetStabilityMin = 45f;
        private float _targetCharmMin = 45f;
        private int _targetDailyGoldMin;
        private int _targetDailyGoldMax = 100000;
        private int _targetWeeklyNetMin;
        private int _targetWeeklyNetMax = 500000;

        [MenuItem("Tools/Balance/2. Project Balance", false, 202)]
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
            DrawProjectGuide();
            DrawInputPanel();
            DrawTeamSlots();

            TeamSimulationResult result = SimulateTeam(GetMaxEmployeePerPart(_projectSize));
            DrawResult(result);
            DrawFoldoutSection(ref _showPresetControls, "대표 케이스", DrawScenarioControls);
            DrawFoldoutSection(ref _showTargetSettings, "목표 범위", () => DrawTargetCheck(result));
            DrawScenarioMatrix();
            EditorGUILayout.EndScrollView();
        }


        private static void DrawProjectGuide()
        {
            BalanceGuideUI.Draw(
                "프로젝트 밸런싱 가이드",
                "프로젝트 규모\n직군별 직원 조합\n일일 퀘스트 보너스\n회사 인기/유지력",
                "완성도/안정성/매력도\n예상 등급\n일일 매출\n직군별 낮은 축",
                "한 직군 점수만 낮음\n일일 매출이 목표보다 과함/부족함\n평균 점수는 높지만 특정 축이 45 미만");
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
            EditorGUILayout.LabelField("기획서 수치 조절", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "프로젝트 시작/진행 문서의 핵심 조건만 먼저 조정합니다. 팀 배치는 아래 직군별 슬롯에서 따로 선택합니다.",
                EditorStyles.wordWrappedMiniLabel);
            BalanceGuideUI.DrawDataFlow(
                "개발 규모, 회사 인기, 유지력, 일일 업무 보너스는 이 창에서만 바꾸는 시뮬레이션 값입니다. 직원 능력치/특성은 Employee SO, 보고서 후보는 Report SO에서 불러옵니다.",
                "이 창의 입력값은 원본 SO/테이블을 수정하지 않습니다. 팀 배치와 보너스를 바꿔 결과만 미리 봅니다.",
                "직원/보고서 점수 + 일일 업무 보너스 -> 완성도/안정성/매력도 -> 평균 점수/등급 -> 예상 판매량/매출");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("1. 프로젝트 기본 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _projectSize = (ProjectSize)EditorGUILayout.EnumPopup("개발 규모", _projectSize);
                    _startRepo = EditorGUILayout.Popup("보고서 시점", _startRepo == 1 ? 0 : 1, new[] { "프로젝트 1주차", "진행 중 랜덤" }) == 0 ? 1 : 0;
                }
                EditorGUILayout.LabelField("규모는 직군별 최대 배치 인원과 기본 판매량 계산에 영향을 줍니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("2. 출시/시장 가정", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _companyPopularity = EditorGUILayout.IntSlider("회사 인기", _companyPopularity, 0, 300);
                    _retentionFactor = EditorGUILayout.Slider("유지력 계수", _retentionFactor, 0f, 1f);
                }
                EditorGUILayout.LabelField("인기와 유지력은 예상 일일 판매량/매출을 보는 임시 시장 조건입니다.", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("3. 직원 상태 영향", EditorStyles.boldLabel);
                _useManualTeamState = EditorGUILayout.Toggle("팀 상태 임시 적용", _useManualTeamState);
                if (_useManualTeamState)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("선택된 직원들의 의욕/피로/충성도를 이 값으로 가정합니다. 원본 직원 데이터는 수정하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
                        _manualTeamDesire = EditorGUILayout.IntSlider("팀 의욕", _manualTeamDesire, 0, 100);
                        _manualTeamFatigue = EditorGUILayout.IntSlider("팀 피로도", _manualTeamFatigue, 0, 100);
                        _manualTeamLoyalty = EditorGUILayout.IntSlider("팀 충성도", _manualTeamLoyalty, 0, 100);
                        EditorGUILayout.LabelField("의욕은 보고서 등급, 충성도는 능력 보정, 피로도는 프로젝트 점수 페널티에 반영됩니다.", EditorStyles.wordWrappedMiniLabel);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("직원 SO 또는 Play Mode 직원의 현재 의욕/피로/충성도를 그대로 사용합니다.", EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("4. 일일 업무 보너스", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _plannerQuestBonus = EditorGUILayout.IntSlider("기획", _plannerQuestBonus, 0, 100);
                    _artistQuestBonus = EditorGUILayout.IntSlider("아트", _artistQuestBonus, 0, 100);
                    _programmerQuestBonus = EditorGUILayout.IntSlider("개발", _programmerQuestBonus, 0, 100);
                }
                EditorGUILayout.LabelField("일일 업무/퀘스트가 해당 주차 직군 점수에 더해지는 보정값입니다.", EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUI.DisabledScope(!Application.isPlaying || QuestManager.Instance == null))
                {
                    if (GUILayout.Button("Play Mode 현재 일퀘 보너스 불러오기"))
                    {
                        _plannerQuestBonus = Mathf.RoundToInt(QuestManager.Instance.GetWeeklyBonus(Role.PLANNER));
                        _artistQuestBonus = Mathf.RoundToInt(QuestManager.Instance.GetWeeklyBonus(Role.ARTIST));
                        _programmerQuestBonus = Mathf.RoundToInt(QuestManager.Instance.GetWeeklyBonus(Role.PROGRAMMER));
                    }
                }

                if (_usePlayModeEmployees)
                {
                    EditorGUILayout.HelpBox(
                        "Play Mode 직원 사용 중입니다. 현재 고용 직원의 성장 능력치, 의욕, 피로도, 충성도를 기준으로 계산합니다.",
                        MessageType.Info);
                }

                if (EditorGUI.EndChangeCheck())
                    Repaint();
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

        private static void DrawFoldoutSection(ref bool show, string title, Action drawContent)
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

        private void DrawScenarioControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("대표 케이스 빠른 설정", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "같은 대표 케이스를 반복해서 돌리며 수치 변경 전후를 비교하는 용도입니다.",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawPresetButton("소형/평균", ProjectSize.Small, EmployeePickMode.Average, 0, 1f, 0, 0, 0);
                    DrawPresetButton("소형/상위", ProjectSize.Small, EmployeePickMode.Strong, 20, 1f, 5, 5, 5);
                    DrawPresetButton("중형/평균", ProjectSize.Medium, EmployeePickMode.Average, 30, 0.9f, 5, 5, 5);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawPresetButton("중형/상위", ProjectSize.Medium, EmployeePickMode.Strong, 60, 0.95f, 10, 10, 10);
                    DrawPresetButton("대형/상위", ProjectSize.Large, EmployeePickMode.Strong, 100, 1f, 15, 15, 15);
                    DrawPresetButton("대형/압박", ProjectSize.Large, EmployeePickMode.Average, 40, 0.75f, 0, 0, 0);
                }
            }
        }

        private void DrawPresetButton(
            string label,
            ProjectSize size,
            EmployeePickMode pickMode,
            int popularity,
            float retention,
            int plannerBonus,
            int artistBonus,
            int programmerBonus)
        {
            if (!GUILayout.Button(label, GUILayout.Height(28f)))
                return;

            ApplyPreset(size, pickMode, popularity, retention, plannerBonus, artistBonus, programmerBonus);
        }

        private void ApplyPreset(
            ProjectSize size,
            EmployeePickMode pickMode,
            int popularity,
            float retention,
            int plannerBonus,
            int artistBonus,
            int programmerBonus)
        {
            _projectSize = size;
            _companyPopularity = popularity;
            _retentionFactor = retention;
            _plannerQuestBonus = plannerBonus;
            _artistQuestBonus = artistBonus;
            _programmerQuestBonus = programmerBonus;
            AutoFillTeam(pickMode);
        }

        private void AutoFillTeam(EmployeePickMode pickMode)
        {
            int maxPerPart = GetMaxEmployeePerPart(_projectSize);
            FillSlots(Role.PLANNER, _plannerSlotIds, maxPerPart, pickMode);
            FillSlots(Role.ARTIST, _artistSlotIds, maxPerPart, pickMode);
            FillSlots(Role.PROGRAMMER, _programmerSlotIds, maxPerPart, pickMode);
        }

        private void FillSlots(Role role, int[] slotIds, int maxPerPart, EmployeePickMode pickMode)
        {
            List<EmployeeSnapshot> picked = PickEmployees(GetEmployeesForRole(role), maxPerPart, pickMode);

            for (int i = 0; i < slotIds.Length; i++)
                slotIds[i] = i < picked.Count ? picked[i].So.id : -1;
        }

        private static List<EmployeeSnapshot> PickEmployees(List<EmployeeSnapshot> source, int count, EmployeePickMode pickMode)
        {
            if (source.Count == 0 || count <= 0)
                return new List<EmployeeSnapshot>();

            IEnumerable<EmployeeSnapshot> ordered = pickMode switch
            {
                EmployeePickMode.Low => source.OrderBy(e => e.StatAbility).ThenBy(e => e.So.id),
                EmployeePickMode.Strong => source.OrderByDescending(e => e.StatAbility).ThenBy(e => e.So.id),
                _ => source.OrderBy(e => Mathf.Abs(e.StatAbility - 50)).ThenBy(e => e.So.id)
            };

            return ordered.Take(count).ToList();
        }

        private void DrawTargetCheck(TeamSimulationResult result)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("목표 범위 검증", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "밸런싱 목표치를 정해두고 현재 조합이 의도한 범위에 들어오는지 확인합니다.",
                    EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetTotalMin = EditorGUILayout.FloatField("평균 최소", _targetTotalMin);
                    _targetTotalMax = EditorGUILayout.FloatField("평균 최대", _targetTotalMax);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetQualityMin = EditorGUILayout.FloatField("완성도 최소", _targetQualityMin);
                    _targetStabilityMin = EditorGUILayout.FloatField("안정성 최소", _targetStabilityMin);
                    _targetCharmMin = EditorGUILayout.FloatField("매력도 최소", _targetCharmMin);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetDailyGoldMin = EditorGUILayout.IntField("일일 매출 최소", _targetDailyGoldMin);
                    _targetDailyGoldMax = EditorGUILayout.IntField("일일 매출 최대", _targetDailyGoldMax);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetWeeklyNetMin = EditorGUILayout.IntField("주간 순수익 최소", _targetWeeklyNetMin);
                    _targetWeeklyNetMax = EditorGUILayout.IntField("주간 순수익 최대", _targetWeeklyNetMax);
                }

                DrawTargetMetric("평균 점수", result.TotalScore, _targetTotalMin, _targetTotalMax);
                DrawTargetMetric("완성도", result.Quality, _targetQualityMin, 150f);
                DrawTargetMetric("안정성", result.Stability, _targetStabilityMin, 150f);
                DrawTargetMetric("매력도", result.Charm, _targetCharmMin, 150f);
                DrawTargetMetric("일일 매출", result.DailyGold, _targetDailyGoldMin, _targetDailyGoldMax);
                DrawTargetMetric("주간 순수익", GetWeeklyNetGold(result), _targetWeeklyNetMin, _targetWeeklyNetMax);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(28f)))
                    {
                        _baselineResult = result;
                        _hasBaseline = true;
                    }

                    EditorGUI.BeginDisabledGroup(!_hasBaseline);
                    if (GUILayout.Button("기준값 지우기", GUILayout.Height(28f)))
                    {
                        _hasBaseline = false;
                    }
                    EditorGUI.EndDisabledGroup();
                }

                DrawBaselineCompare(result);
            }
        }

        private static void DrawTargetMetric(string label, float value, float min, float max)
        {
            bool passed = value >= min && value <= max;
            EditorGUILayout.LabelField(
                $"{(passed ? "OK" : "NG")} {label}: {value:0.#} / 목표 {min:0.#}~{max:0.#}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private void DrawBaselineCompare(TeamSimulationResult current)
        {
            if (!_hasBaseline)
            {
                EditorGUILayout.HelpBox(
                    "수치 변경 전 결과를 기준값으로 저장하면, 이후 변경 결과와 차이를 바로 비교할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("기준값 대비 변화", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(BuildDeltaText("평균", current.TotalScore, _baselineResult.TotalScore), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("완성도", current.Quality, _baselineResult.Quality), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("안정성", current.Stability, _baselineResult.Stability), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("매력도", current.Charm, _baselineResult.Charm), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("일일 매출", current.DailyGold, _baselineResult.DailyGold), EditorStyles.miniLabel);
            EditorGUILayout.LabelField(BuildDeltaText("주간 순수익", GetWeeklyNetGold(current), GetWeeklyNetGold(_baselineResult)), EditorStyles.miniLabel);
        }

        private static string BuildDeltaText(string label, float current, float baseline)
        {
            float delta = current - baseline;
            return $"{label}: {current:0.#} ({delta:+0.#;-0.#;0} / 기준 {baseline:0.#})";
        }

        private void DrawScenarioMatrix()
        {
            EditorGUILayout.Space(4f);
            _showScenarioMatrix = EditorGUILayout.Foldout(_showScenarioMatrix, "대표 케이스 매트릭스", true);
            if (!_showScenarioMatrix)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "직접 슬롯을 바꾸지 않아도 프로젝트 규모와 직원 수준별 결과를 한 번에 비교합니다.",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("규모 / 직원 수준 / 평균 / 등급 / 일일 매출 / 주간 순수익 / 판정", EditorStyles.miniLabel);

                foreach (ProjectSize size in new[] { ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large })
                {
                    DrawMatrixRow(size, EmployeePickMode.Low);
                    DrawMatrixRow(size, EmployeePickMode.Average);
                    DrawMatrixRow(size, EmployeePickMode.Strong);
                }
            }
        }

        private void DrawMatrixRow(ProjectSize size, EmployeePickMode pickMode)
        {
            TeamSimulationResult result = SimulatePreset(size, pickMode);
            bool passed = result.TotalScore >= _targetTotalMin
                && result.TotalScore <= _targetTotalMax
                && result.Quality >= _targetQualityMin
                && result.Stability >= _targetStabilityMin
                && result.Charm >= _targetCharmMin
                && result.DailyGold >= _targetDailyGoldMin
                && result.DailyGold <= _targetDailyGoldMax
                && GetWeeklyNetGold(result) >= _targetWeeklyNetMin
                && GetWeeklyNetGold(result) <= _targetWeeklyNetMax;

            EditorGUILayout.LabelField(
                $"{GetProjectSizeLabel(size)} / {GetPickModeLabel(pickMode)} / {result.TotalScore:0.#} / {GetProjectGrade(result.TotalScore)} / {result.DailyGold:N0}G / {GetWeeklyNetGold(result):N0}G / {(passed ? "OK" : "NG")}",
                passed ? EditorStyles.miniLabel : EditorStyles.boldLabel);
        }

        private TeamSimulationResult SimulatePreset(ProjectSize size, EmployeePickMode pickMode)
        {
            int maxPerPart = GetMaxEmployeePerPart(size);
            List<EmployeeSnapshot> planners = PickEmployees(GetEmployeesForRole(Role.PLANNER), maxPerPart, pickMode);
            List<EmployeeSnapshot> artists = PickEmployees(GetEmployeesForRole(Role.ARTIST), maxPerPart, pickMode);
            List<EmployeeSnapshot> programmers = PickEmployees(GetEmployeesForRole(Role.PROGRAMMER), maxPerPart, pickMode);

            return SimulateTeam(
                planners,
                artists,
                programmers,
                _plannerQuestBonus,
                _artistQuestBonus,
                _programmerQuestBonus,
                _retentionFactor,
                _companyPopularity,
                size);
        }


        private void DrawRoleSlots(Role role, int[] slotIds, int maxPerPart)
        {
            List<EmployeeSnapshot> employees = GetEmployeesForRole(role);
            string[] options = new[] { "미배치" }
                .Concat(employees.Select(e => $"{e.So.id} / {e.So.Name} / 능력 {e.StatAbility} / 의욕 {e.Desire} / 피로 {e.Fatigue} / 충성 {e.Loyalty}"))
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
            return SimulateTeam(
                GetSelectedTeamMembers(Role.PLANNER, maxPerPart),
                GetSelectedTeamMembers(Role.ARTIST, maxPerPart),
                GetSelectedTeamMembers(Role.PROGRAMMER, maxPerPart),
                _plannerQuestBonus,
                _artistQuestBonus,
                _programmerQuestBonus,
                _retentionFactor,
                _companyPopularity,
                _projectSize);
        }

        private TeamSimulationResult SimulateTeam(
            List<EmployeeSnapshot> planners,
            List<EmployeeSnapshot> artists,
            List<EmployeeSnapshot> programmers,
            int plannerQuestBonus,
            int artistQuestBonus,
            int programmerQuestBonus,
            float retentionFactor,
            int companyPopularity,
            ProjectSize projectSize)
        {
            RoleSimulationResult planner = SimulateRole(Role.PLANNER, planners);
            RoleSimulationResult artist = SimulateRole(Role.ARTIST, artists);
            RoleSimulationResult programmer = SimulateRole(Role.PROGRAMMER, programmers);

            float baseQuality = planner.HasReport ? planner.Score : 0f;
            float baseCharm = artist.HasReport ? artist.Score : 0f;
            float baseStability = programmer.HasReport ? programmer.Score : 0f;
            float quality = baseQuality + plannerQuestBonus;
            float charm = baseCharm + artistQuestBonus;
            float stability = baseStability + programmerQuestBonus;
            float totalScore = (quality + stability + charm) / 3f;
            int dailySales = PerkPolicy.CalcDailySales(projectSize, quality, stability, charm, retentionFactor, companyPopularity);
            int dailyGold = PerkPolicy.CalcDailyGold(projectSize, dailySales);
            int weeklyCost = PerkPolicy.CalcWeeklyCost(projectSize);

            return new TeamSimulationResult(
                planner,
                artist,
                programmer,
                baseQuality,
                baseStability,
                baseCharm,
                plannerQuestBonus,
                programmerQuestBonus,
                artistQuestBonus,
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
                BalanceGuideUI.DrawFormulaNotice("프로젝트 평균, 판매량, 매출은 현재 코드 공식으로 계산된 예상값입니다.");
                DrawResultBaselineControls(result);
                DrawProjectAutoChecks(result);
                DrawProjectInterpretation(result);

                DrawRoleResult(result.Planner);
                DrawRoleResult(result.Artist);
                DrawRoleResult(result.Programmer);
            }
        }

        private void DrawResultBaselineControls(TeamSimulationResult result)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("변경 전후 비교", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("현재 결과를 기준값으로 저장", GUILayout.Height(24f)))
                    {
                        _baselineResult = result;
                        _hasBaseline = true;
                    }

                    using (new EditorGUI.DisabledScope(!_hasBaseline))
                    {
                        if (GUILayout.Button("기준값 지우기", GUILayout.Width(110f), GUILayout.Height(24f)))
                            _hasBaseline = false;
                    }
                }

                BalanceGuideUI.DrawBaselineHint(_hasBaseline);
                DrawBaselineCompare(result);
            }
        }

        private static void DrawProjectAutoChecks(TeamSimulationResult result)
        {
            float lowestAxis = Mathf.Min(result.Quality, result.Stability, result.Charm);
            int weeklyNet = result.DailyGold * 5 - result.WeeklyCost;
            bool hasMissingReport = !result.Planner.HasReport || !result.Artist.HasReport || !result.Programmer.HasReport;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("자동 감지", EditorStyles.boldLabel);
                BalanceGuideUI.DrawAutoCheck("보고서 후보", hasMissingReport, hasMissingReport ? "직군 중 보고서가 없는 축이 있습니다." : "모든 직군에서 후보 보고서를 찾았습니다.");
                BalanceGuideUI.DrawAutoCheck("낮은 직군 축", lowestAxis < 45f, lowestAxis < 45f ? $"가장 낮은 축이 {lowestAxis:0.#}점입니다." : "완성도/안정성/매력도 모두 45 이상입니다.");
                BalanceGuideUI.DrawAutoCheck("주간 순수익", weeklyNet < 0, weeklyNet < 0 ? $"예상 주간 순수익 {weeklyNet:N0}G입니다." : $"예상 주간 순수익 {weeklyNet:N0}G입니다.");
            }
        }

        private static void DrawProjectInterpretation(TeamSimulationResult result)
        {
            float lowestAxis = Mathf.Min(result.Quality, result.Stability, result.Charm);
            int weeklyNet = result.DailyGold * 5 - result.WeeklyCost;

            if (!result.Planner.HasReport || !result.Artist.HasReport || !result.Programmer.HasReport)
            {
                BalanceGuideUI.DrawInterpretation("직군 중 보고서가 없는 축이 있어 프로젝트 결과를 신뢰하기 어렵습니다.", MessageType.Warning);
                return;
            }

            if (lowestAxis < 45f)
            {
                BalanceGuideUI.DrawInterpretation("평균보다 낮은 직군 축이 있습니다. 해당 직군 직원/보고서/일일 업무 보너스를 먼저 확인하세요.", MessageType.Warning);
                return;
            }

            if (weeklyNet < 0)
            {
                BalanceGuideUI.DrawInterpretation("점수는 나쁘지 않아도 주간 순수익이 적자입니다. 판매량 또는 유지비 쪽을 확인하세요.", MessageType.Warning);
                return;
            }

            if (result.TotalScore >= 70f)
                BalanceGuideUI.DrawInterpretation("현재 조합은 프로젝트 점수와 수익이 안정권입니다.");
            else
                BalanceGuideUI.DrawInterpretation("현재 조합은 검증 가능하지만 상위 등급을 노리기엔 평균 점수가 낮습니다.", MessageType.Info);
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
                EditorGUILayout.LabelField($"상태 능력 {best.Employee.StatAbility} / 의욕 {best.Employee.Desire} / 피로 {best.Employee.Fatigue} / 충성 {best.Employee.Loyalty}");
                EditorGUILayout.LabelField($"특성 {GetTraitLabel(best.Report.trait)} / {best.MatchSource} 매칭 / grade={best.Grade} / 점수 {best.Score:0.#}");
                float fatiguePenalty = CalcFatigueProjectPenalty(best.Employee.Fatigue);
                if (fatiguePenalty > 0f)
                    EditorGUILayout.HelpBox($"피로도 {best.Employee.Fatigue}로 프로젝트 점수 -{fatiguePenalty:0.#} 페널티가 적용되었습니다.", MessageType.Warning);

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
                    selected.Add(ApplyTeamStateOverride(employee));
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
            float fatiguePenalty = CalcFatigueProjectPenalty(employee.Fatigue);

            return Mathf.Max(0f, (Mathf.Clamp(scores[stats[0]], 0f, 100f)
                + Mathf.Clamp(scores[stats[1]], 0f, 100f)
                + Mathf.Clamp(scores[stats[2]], 0f, 100f)
                + traitWeight) / 3f - fatiguePenalty);
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

        private static float CalcFatigueProjectPenalty(int fatigue)
        {
            if (fatigue >= 80)
                return 10f;

            if (fatigue >= 60)
                return 5f;

            return 0f;
        }

        private EmployeeSnapshot ApplyTeamStateOverride(EmployeeSnapshot employee)
        {
            if (!_useManualTeamState)
                return employee;

            return employee.WithState(_manualTeamDesire, _manualTeamFatigue, _manualTeamLoyalty);
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

        private static int GetWeeklyNetGold(TeamSimulationResult result)
        {
            return result.DailyGold * 5 - result.WeeklyCost;
        }

        private static string GetProjectSizeLabel(ProjectSize size)
        {
            return size switch
            {
                ProjectSize.Medium => "중형",
                ProjectSize.Large => "대형",
                _ => "소형"
            };
        }

        private static string GetPickModeLabel(EmployeePickMode pickMode)
        {
            return pickMode switch
            {
                EmployeePickMode.Low => "하위 직원",
                EmployeePickMode.Strong => "상위 직원",
                _ => "평균 직원"
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

            public static EmployeeSnapshot FromAsset(AssetEntry<EmployeeImmutableData> entry)
            {
                EmployeeImmutableData so = entry.Asset;
                return new EmployeeSnapshot(so, so.ability, so.ability, so.desire, so.fatigue, so.loyalty);
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

            public EmployeeSnapshot WithState(int desire, int fatigue, int loyalty)
            {
                return new EmployeeSnapshot(So, ReportScoreAbility, StatAbility, desire, fatigue, loyalty);
            }
        }
    }
}
