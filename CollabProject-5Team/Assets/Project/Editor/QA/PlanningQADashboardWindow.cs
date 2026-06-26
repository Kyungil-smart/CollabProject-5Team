using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class PlanningQADashboardWindow : EditorWindow
    {
        private const string EmployeeRoot = "Assets/Project/DB/Employee";
        private const string ReportRoot = "Assets/Project/DB/Report";
        private const string QuestRoot = "Assets/Project/DB/Quest";
        private const string CommentRoot = "Assets/Project/DB/CommentData";

        private static readonly Role[] CoreRoles =
        {
            Role.PLANNER,
            Role.ARTIST,
            Role.PROGRAMMER
        };

        private static readonly int[] ReportStartRepos = { 1, 0 };
        private static readonly int[] ReportGrades = { 1, 2, 3 };
        private static readonly int[] CommentTriggers = { 0, 50 };

        private readonly List<AssetEntry<EmployeeImmutableData>> _employees = new();
        private readonly List<AssetEntry<ReportSO>> _reports = new();
        private readonly List<AssetEntry<QuestSO>> _quests = new();
        private readonly List<AssetEntry<EmployeeCommentData>> _comments = new();
        private readonly List<PlanningIssue> _issues = new();

        private Vector2 _scrollPosition;
        private bool _showOverview = true;
        private bool _showEmployees = true;
        private bool _showReports = true;
        private bool _showQuests = true;
        private bool _showComments = true;
        private bool _showTextChecks = true;
        private bool _showIssues = true;
        private int _reportTitleLimit = 20;
        private int _weeklyCommentLimit = 16;
        private int _questNameLimit = 20;
        private string _issueSearchText = string.Empty;

        [MenuItem("Tools/QA/Planning QA Dashboard")]
        public static void Open()
        {
            PlanningQADashboardWindow window = GetWindow<PlanningQADashboardWindow>("Planning QA");
            window.minSize = new Vector2(720f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawOverview();
            DrawEmployeeDistribution();
            DrawReportDistribution();
            DrawQuestDistribution();
            DrawCommentDistribution();
            DrawTextChecks();
            DrawIssueList();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(110f)))
                    RefreshData();

                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("텍스트 기준", GUILayout.Width(64f));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("보고서", GUILayout.Width(38f));
                _reportTitleLimit = EditorGUILayout.IntField(_reportTitleLimit, GUILayout.Width(36f));
                EditorGUILayout.LabelField("코멘트", GUILayout.Width(42f));
                _weeklyCommentLimit = EditorGUILayout.IntField(_weeklyCommentLimit, GUILayout.Width(36f));
                EditorGUILayout.LabelField("퀘스트", GUILayout.Width(42f));
                _questNameLimit = EditorGUILayout.IntField(_questNameLimit, GUILayout.Width(36f));
                if (EditorGUI.EndChangeCheck())
                {
                    _reportTitleLimit = Mathf.Max(1, _reportTitleLimit);
                    _weeklyCommentLimit = Mathf.Max(1, _weeklyCommentLimit);
                    _questNameLimit = Mathf.Max(1, _questNameLimit);
                    RebuildIssues();
                }
            }
        }

        private void DrawOverview()
        {
            _showOverview = EditorGUILayout.Foldout(_showOverview, "요약", true);
            if (!_showOverview)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("기획 QA Dashboard", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "기획 데이터의 양, 분포, 빈 조합, 텍스트 길이 리스크를 한눈에 보는 창입니다. 개발 참조 오류보다는 데이터 설계 상태를 보는 용도입니다.",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawMetric("직원", _employees.Count.ToString());
                    DrawMetric("보고서", _reports.Count.ToString());
                    DrawMetric("퀘스트", _quests.Count.ToString());
                    DrawMetric("주간 코멘트", _comments.Count.ToString());
                    DrawMetric("기획 이슈", _issues.Count.ToString());
                }

                EditorGUILayout.Space(4f);
                foreach (Role role in CoreRoles)
                {
                    EditorGUILayout.LabelField(
                        $"{GetRoleLabel(role)}: 직원 {_employees.Count(e => e.Asset.role == role)} / 보고서 {_reports.Count(r => r.Asset.role == role)} / 퀘스트 {_quests.Count(q => q.Asset.role == role)} / 코멘트 {_comments.Count(c => c.Asset.target_role == role)}",
                        EditorStyles.miniLabel);
                }
            }
        }

        private void DrawEmployeeDistribution()
        {
            _showEmployees = EditorGUILayout.Foldout(_showEmployees, "직원 분포", true);
            if (!_showEmployees)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (Role role in CoreRoles)
                {
                    List<EmployeeImmutableData> roleEmployees = _employees
                        .Where(e => e.Asset.role == role)
                        .Select(e => e.Asset)
                        .OrderBy(e => e.id)
                        .ToList();

                    EditorGUILayout.LabelField($"{GetRoleLabel(role)} 직원 {roleEmployees.Count}명", EditorStyles.boldLabel);
                    if (roleEmployees.Count == 0)
                    {
                        EditorGUILayout.HelpBox("해당 직군 직원 데이터가 없습니다.", MessageType.Warning);
                        continue;
                    }

                    EditorGUILayout.LabelField(
                        $"능력 평균 {Average(roleEmployees, e => e.ability):0.#} / 의욕 평균 {Average(roleEmployees, e => e.desire):0.#} / 피로도 평균 {Average(roleEmployees, e => e.fatigue):0.#} / 충성도 평균 {Average(roleEmployees, e => e.loyalty):0.#}",
                        EditorStyles.miniLabel);

                    EditorGUILayout.LabelField(
                        "등급 분포: " + string.Join(" / ", roleEmployees
                            .GroupBy(e => e.grade)
                            .OrderBy(g => g.Key)
                            .Select(g => $"{GetEmployeeGradeLabel(g.Key)} {g.Count()}")),
                        EditorStyles.miniLabel);

                    DrawTraitUsage(role, roleEmployees);
                    EditorGUILayout.Space(4f);
                }
            }
        }

        private void DrawTraitUsage(Role role, List<EmployeeImmutableData> employees)
        {
            Dictionary<Trait, int> usage = new();

            foreach (EmployeeImmutableData employee in employees)
            {
                AddTraitUsage(usage, employee.mainTrait);
                AddTraitUsage(usage, employee.subTrait);
                AddTraitUsage(usage, employee.riskTrait);
            }

            string usageText = usage.Count == 0
                ? "없음"
                : string.Join(", ", usage
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => GetTraitLabel(pair.Key))
                    .Select(pair => $"{GetTraitLabel(pair.Key)} {pair.Value}"));

            EditorGUILayout.LabelField($"{GetRoleLabel(role)} 특성 사용: {usageText}", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawReportDistribution()
        {
            _showReports = EditorGUILayout.Foldout(_showReports, "보고서 분포", true);
            if (!_showReports)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (Role role in CoreRoles)
                {
                    List<ReportSO> roleReports = _reports
                        .Where(r => r.Asset.role == role)
                        .Select(r => r.Asset)
                        .ToList();

                    EditorGUILayout.LabelField($"{GetRoleLabel(role)} 보고서 {roleReports.Count}개", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"1주차 {roleReports.Count(r => r.startRepo == 1)} / 랜덤 {roleReports.Count(r => r.startRepo == 0)} / 1등급 {roleReports.Count(r => r.grade == 1)} / 2등급 {roleReports.Count(r => r.grade == 2)} / 3등급 {roleReports.Count(r => r.grade == 3)}",
                        EditorStyles.miniLabel);

                    DrawReportTraitRows(role, roleReports);
                    EditorGUILayout.Space(4f);
                }
            }
        }

        private void DrawReportTraitRows(Role role, List<ReportSO> roleReports)
        {
            List<Trait> traits = GetExpectedTraits(role).ToList();
            if (traits.Count == 0)
                return;

            foreach (Trait trait in traits)
            {
                string startCount = BuildGradeCountText(roleReports, trait, 1);
                string randomCount = BuildGradeCountText(roleReports, trait, 0);
                EditorGUILayout.LabelField(
                    $"{GetTraitLabel(trait)}: 1주차 [{startCount}] / 랜덤 [{randomCount}]",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawQuestDistribution()
        {
            _showQuests = EditorGUILayout.Foldout(_showQuests, "일일 퀘스트 분포", true);
            if (!_showQuests)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (Role role in CoreRoles)
                {
                    List<QuestSO> roleQuests = _quests
                        .Where(q => q.Asset.role == role)
                        .Select(q => q.Asset)
                        .OrderBy(q => q.id)
                        .ToList();

                    EditorGUILayout.LabelField($"{GetRoleLabel(role)} 퀘스트 {roleQuests.Count}개", EditorStyles.boldLabel);
                    if (roleQuests.Count == 0)
                    {
                        EditorGUILayout.HelpBox("해당 직군 일일 퀘스트가 없습니다.", MessageType.Warning);
                        continue;
                    }

                    EditorGUILayout.LabelField(
                        $"보상 평균 {Average(roleQuests, q => q.successEffect):0.#} / 목표 수 평균 {Average(roleQuests, q => q.targetCount):0.#} / 2단계 퀘스트 {roleQuests.Count(q => q.controlType2 != ControlType.NONE)}개",
                        EditorStyles.miniLabel);

                    string controls = string.Join(" / ", roleQuests
                        .GroupBy(q => q.controlType)
                        .OrderBy(g => g.Key.ToString())
                        .Select(g => $"{g.Key} {g.Count()}"));
                    EditorGUILayout.LabelField($"조작 타입: {controls}", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCommentDistribution()
        {
            _showComments = EditorGUILayout.Foldout(_showComments, "주간 코멘트 분포", true);
            if (!_showComments)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (Role role in CoreRoles)
                {
                    List<EmployeeCommentData> roleComments = _comments
                        .Where(c => c.Asset.target_role == role)
                        .Select(c => c.Asset)
                        .ToList();

                    EditorGUILayout.LabelField($"{GetRoleLabel(role)} 주간 코멘트 {roleComments.Count}개", EditorStyles.boldLabel);

                    foreach (int desire in CommentTriggers)
                    foreach (int fatigue in CommentTriggers)
                    foreach (int loyalty in CommentTriggers)
                    {
                        int count = roleComments.Count(c =>
                            c.trigger_desire == desire
                            && c.trigger_fatigue == fatigue
                            && c.trigger_loyalty == loyalty);

                        EditorGUILayout.LabelField(
                            $"의욕 {GetTriggerLabel(desire)} / 피로도 {GetTriggerLabel(fatigue)} / 충성도 {GetTriggerLabel(loyalty)}: {count}개",
                            count == 0 ? EditorStyles.boldLabel : EditorStyles.miniLabel);
                    }

                    EditorGUILayout.Space(4f);
                }
            }
        }

        private void DrawTextChecks()
        {
            _showTextChecks = EditorGUILayout.Foldout(_showTextChecks, "텍스트 길이 체크", true);
            if (!_showTextChecks)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int longReportTitles = _reports.Count(r => SafeLength(r.Asset.title) > _reportTitleLimit);
                int longQuestNames = _quests.Count(q => SafeLength(q.Asset.Name) > _questNameLimit);
                int longComments = _comments.Count(c => SafeLength(c.Asset.comment_text) > _weeklyCommentLimit);

                EditorGUILayout.LabelField($"보고서 제목 {_reportTitleLimit}자 초과: {longReportTitles}개");
                EditorGUILayout.LabelField($"퀘스트 이름 {_questNameLimit}자 초과: {longQuestNames}개");
                EditorGUILayout.LabelField($"주간 코멘트 {_weeklyCommentLimit}자 초과: {longComments}개");
                EditorGUILayout.LabelField(
                    "이 기준은 UI 배치 확인용 임시 기준입니다. 실제 제한이 바뀌면 상단 입력값만 바꿔서 다시 볼 수 있습니다.",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawIssueList()
        {
            _showIssues = EditorGUILayout.Foldout(_showIssues, $"기획 이슈 목록 {_issues.Count}개", true);
            if (!_showIssues)
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("검색", GUILayout.Width(36f));
                    _issueSearchText = EditorGUILayout.TextField(_issueSearchText);
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_issueSearchText)))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(54f)))
                            _issueSearchText = string.Empty;
                    }
                }

                foreach (PlanningIssue issue in _issues.Where(MatchesIssueSearch).Take(200))
                    DrawIssue(issue);

                if (_issues.Count > 200)
                    EditorGUILayout.HelpBox("이슈가 200개를 초과해 앞쪽 200개만 표시합니다. 검색어로 좁혀 보세요.", MessageType.Info);
            }
        }

        private void DrawIssue(PlanningIssue issue)
        {
            MessageType messageType = issue.Severity switch
            {
                PlanningIssueSeverity.Error => MessageType.Error,
                PlanningIssueSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info
            };

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox($"[{issue.Category}] {issue.Message}", messageType);
                if (issue.Target == null && string.IsNullOrEmpty(issue.AssetPath))
                    return;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(issue.Target == null))
                    {
                        if (GUILayout.Button("Select", GUILayout.Width(70f)))
                        {
                            Selection.activeObject = issue.Target;
                            EditorGUIUtility.PingObject(issue.Target);
                        }
                    }

                    EditorGUILayout.SelectableLabel(issue.AssetPath, GUILayout.Height(18f));
                }
            }
        }

        private void RefreshData()
        {
            _employees.Clear();
            _reports.Clear();
            _quests.Clear();
            _comments.Clear();

            _employees.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeImmutableData>(EmployeeRoot));
            _reports.AddRange(QAAssetUtility.FindAssetEntriesByType<ReportSO>(ReportRoot));
            _quests.AddRange(QAAssetUtility.FindAssetEntriesByType<QuestSO>(QuestRoot));
            _comments.AddRange(QAAssetUtility.FindAssetEntriesByType<EmployeeCommentData>(CommentRoot));

            RebuildIssues();
        }

        private void RebuildIssues()
        {
            _issues.Clear();
            AddEmployeeIssues();
            AddReportIssues();
            AddQuestIssues();
            AddCommentIssues();
            AddTextIssues();
        }

        private void AddEmployeeIssues()
        {
            foreach (Role role in CoreRoles)
            {
                if (_employees.Any(e => e.Asset.role == role))
                    continue;

                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Error,
                    "Employee",
                    $"{GetRoleLabel(role)} 직원 데이터가 없습니다.",
                    EmployeeRoot));
            }

            foreach (AssetEntry<EmployeeImmutableData> entry in _employees)
            {
                EmployeeImmutableData employee = entry.Asset;
                if (!CoreRoles.Contains(employee.role))
                    continue;

                if (!QATraitUtility.TryGetExpectedTraitRole(employee.role, out TraitRole expectedRole))
                    continue;

                AddTraitRoleIssue(entry, employee.mainTrait, "대표 특성", expectedRole);
                AddTraitRoleIssue(entry, employee.subTrait, "보조 특성", expectedRole);
                AddTraitRoleIssue(entry, employee.riskTrait, "리스크 특성", expectedRole);
            }
        }

        private void AddTraitRoleIssue(
            AssetEntry<EmployeeImmutableData> entry,
            Trait trait,
            string label,
            TraitRole expectedRole)
        {
            if (trait == Trait.None)
                return;

            if (!QATraitUtility.TryGetTraitData(trait, out TraitData data))
            {
                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Error,
                    "Employee Trait",
                    $"{entry.Asset.Name}의 {label} {trait}가 TraitTable에 없습니다.",
                    entry.Path,
                    entry.Asset));
                return;
            }

            if (data.role == expectedRole)
                return;

            _issues.Add(new PlanningIssue(
                PlanningIssueSeverity.Error,
                "Employee Trait",
                $"{entry.Asset.Name}의 {label} '{data.displayName}' 직군({data.role})이 직원 직군({entry.Asset.role})과 맞지 않습니다.",
                entry.Path,
                entry.Asset));
        }

        private void AddReportIssues()
        {
            var reportLookup = new HashSet<(Trait trait, int startRepo, int grade)>(
                _reports
                    .Where(r => r.Asset.trait != Trait.None)
                    .Select(r => (r.Asset.trait, r.Asset.startRepo, r.Asset.grade)));

            foreach (Role role in CoreRoles)
            {
                if (!_reports.Any(r => r.Asset.role == role))
                {
                    _issues.Add(new PlanningIssue(
                        PlanningIssueSeverity.Error,
                        "Report",
                        $"{GetRoleLabel(role)} 보고서 데이터가 없습니다.",
                        ReportRoot));
                }
            }

            foreach (AssetEntry<EmployeeImmutableData> employeeEntry in _employees)
            {
                EmployeeImmutableData employee = employeeEntry.Asset;
                if (!CoreRoles.Contains(employee.role))
                    continue;

                foreach (int startRepo in ReportStartRepos)
                foreach (int grade in ReportGrades)
                {
                    bool hasAny = GetEmployeeTraits(employee)
                        .Where(trait => trait != Trait.None)
                        .Any(trait => reportLookup.Contains((trait, startRepo, grade)));

                    if (hasAny)
                        continue;

                    _issues.Add(new PlanningIssue(
                        PlanningIssueSeverity.Warning,
                        "Report Coverage",
                        $"{employee.Name} 직원 특성으로 생성 가능한 보고서가 없습니다. startRepo={startRepo}, grade={grade}",
                        employeeEntry.Path,
                        employeeEntry.Asset));
                }
            }

            foreach (var group in _reports.GroupBy(r => (r.Asset.role, r.Asset.trait, r.Asset.startRepo, r.Asset.grade)))
            {
                if (group.Count() <= 1)
                    continue;

                AssetEntry<ReportSO> first = group.First();
                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Info,
                    "Report Volume",
                    $"{GetRoleLabel(group.Key.role)} / {GetTraitLabel(group.Key.trait)} / startRepo={group.Key.startRepo} / grade={group.Key.grade} 보고서가 {group.Count()}개 있습니다. 랜덤 후보 의도인지 확인하세요.",
                    first.Path,
                    first.Asset));
            }
        }

        private void AddQuestIssues()
        {
            foreach (Role role in CoreRoles)
            {
                if (_quests.Any(q => q.Asset.role == role))
                    continue;

                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Warning,
                    "Quest Coverage",
                    $"{GetRoleLabel(role)} 일일 퀘스트가 없습니다.",
                    QuestRoot));
            }

            foreach (AssetEntry<QuestSO> entry in _quests)
            {
                QuestSO quest = entry.Asset;
                if (quest.successEffect < 0)
                {
                    _issues.Add(new PlanningIssue(
                        PlanningIssueSeverity.Error,
                        "Quest",
                        $"{quest.Name} successEffect가 음수입니다. 현재값 {quest.successEffect}",
                        entry.Path,
                        entry.Asset));
                }

                if (string.IsNullOrWhiteSpace(quest.npcDialogue))
                {
                    _issues.Add(new PlanningIssue(
                        PlanningIssueSeverity.Warning,
                        "Quest Text",
                        $"{quest.Name} 완료 말풍선 텍스트가 비어 있습니다.",
                        entry.Path,
                        entry.Asset));
                }
            }
        }

        private void AddCommentIssues()
        {
            var commentLookup = new HashSet<(Role role, int desire, int fatigue, int loyalty)>(
                _comments.Select(c => (
                    c.Asset.target_role,
                    c.Asset.trigger_desire,
                    c.Asset.trigger_fatigue,
                    c.Asset.trigger_loyalty)));

            foreach (Role role in CoreRoles)
            foreach (int desire in CommentTriggers)
            foreach (int fatigue in CommentTriggers)
            foreach (int loyalty in CommentTriggers)
            {
                if (commentLookup.Contains((role, desire, fatigue, loyalty)))
                    continue;

                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Warning,
                    "Comment Coverage",
                    $"{GetRoleLabel(role)} 주간 코멘트 조합이 비어 있습니다. desire={desire}, fatigue={fatigue}, loyalty={loyalty}",
                    CommentRoot));
            }
        }

        private void AddTextIssues()
        {
            foreach (AssetEntry<ReportSO> entry in _reports)
            {
                if (SafeLength(entry.Asset.title) <= _reportTitleLimit)
                    continue;

                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Warning,
                    "Text Length",
                    $"보고서 제목이 {_reportTitleLimit}자를 초과합니다. {SafeLength(entry.Asset.title)}자: {entry.Asset.title}",
                    entry.Path,
                    entry.Asset));
            }

            foreach (AssetEntry<QuestSO> entry in _quests)
            {
                if (SafeLength(entry.Asset.Name) <= _questNameLimit)
                    continue;

                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Warning,
                    "Text Length",
                    $"퀘스트 이름이 {_questNameLimit}자를 초과합니다. {SafeLength(entry.Asset.Name)}자: {entry.Asset.Name}",
                    entry.Path,
                    entry.Asset));
            }

            foreach (AssetEntry<EmployeeCommentData> entry in _comments)
            {
                if (SafeLength(entry.Asset.comment_text) <= _weeklyCommentLimit)
                    continue;

                _issues.Add(new PlanningIssue(
                    PlanningIssueSeverity.Info,
                    "Text Length",
                    $"주간 코멘트가 {_weeklyCommentLimit}자를 초과합니다. {SafeLength(entry.Asset.comment_text)}자",
                    entry.Path,
                    entry.Asset));
            }
        }

        private bool MatchesIssueSearch(PlanningIssue issue)
        {
            if (string.IsNullOrWhiteSpace(_issueSearchText))
                return true;

            string keyword = _issueSearchText.Trim();
            return Contains(issue.Category, keyword)
                || Contains(issue.Message, keyword)
                || Contains(issue.AssetPath, keyword);
        }

        private static bool Contains(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source)
                && source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawMetric(string label, string value)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(90f)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            }
        }

        private static string BuildGradeCountText(List<ReportSO> reports, Trait trait, int startRepo)
        {
            return string.Join(", ", ReportGrades.Select(grade =>
                $"G{grade}:{reports.Count(r => r.trait == trait && r.startRepo == startRepo && r.grade == grade)}"));
        }

        private static IEnumerable<Trait> GetExpectedTraits(Role role)
        {
            if (!QATraitUtility.TryGetExpectedTraitRole(role, out TraitRole traitRole))
                yield break;

            foreach (KeyValuePair<Trait, TraitData> pair in TraitTable.All)
            {
                if (pair.Key == Trait.None || pair.Value.role != traitRole)
                    continue;

                yield return pair.Key;
            }
        }

        private static IEnumerable<Trait> GetEmployeeTraits(EmployeeImmutableData employee)
        {
            yield return employee.mainTrait;
            yield return employee.riskTrait;
            yield return employee.subTrait;
        }

        private static void AddTraitUsage(Dictionary<Trait, int> usage, Trait trait)
        {
            if (trait == Trait.None)
                return;

            usage.TryGetValue(trait, out int count);
            usage[trait] = count + 1;
        }

        private static float Average<T>(List<T> values, Func<T, int> selector)
        {
            return values.Count == 0 ? 0f : (float)values.Average(v => selector(v));
        }

        private static int SafeLength(string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : text.Trim().Length;
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
                && !string.IsNullOrWhiteSpace(data.displayName)
                ? data.displayName
                : trait.ToString();
        }

        private static string GetEmployeeGradeLabel(int grade)
        {
            return grade switch
            {
                1 => "S",
                2 => "A",
                3 => "B",
                4 => "C",
                _ => grade.ToString()
            };
        }

        private static string GetTriggerLabel(int trigger)
        {
            return trigger == 50 ? "50 이상" : "50 미만";
        }

        private readonly struct PlanningIssue
        {
            public PlanningIssueSeverity Severity { get; }
            public string Category { get; }
            public string Message { get; }
            public string AssetPath { get; }
            public UnityEngine.Object Target { get; }

            public PlanningIssue(
                PlanningIssueSeverity severity,
                string category,
                string message,
                string assetPath = "",
                UnityEngine.Object target = null)
            {
                Severity = severity;
                Category = category;
                Message = message;
                AssetPath = assetPath;
                Target = target;
            }
        }

        private enum PlanningIssueSeverity
        {
            Info,
            Warning,
            Error
        }
    }
}
