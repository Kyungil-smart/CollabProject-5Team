using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameDevTycoon.UI.Ingame;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class ReportCoverageRuntimeQAWindow : EditorWindow
    {
        private const double ScanInterval = 0.01d;
        private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly FieldInfo ManagerNormalReportsField = typeof(ReportManager).GetField("_allReports", Fields);
        private static readonly FieldInfo ManagerSpyReportsField = typeof(ReportManager).GetField("_allSpyReports", Fields);
        private static readonly FieldInfo CardTitleField = typeof(ReportCardView).GetField("_reportTitleLabel", Fields);
        private static readonly FieldInfo CardNameField = typeof(ReportCardView).GetField("_nameLabel", Fields);
        private static readonly FieldInfo DetailTitleField = typeof(ReportView).GetField("_detailTitleLable", Fields);
        private static readonly FieldInfo DetailNameField = typeof(ReportView).GetField("_detailEmployeeNameLable", Fields);
        private static readonly FieldInfo DetailContentField = typeof(ReportView).GetField("_detailContentLable", Fields);
        private static readonly FieldInfo CanvasField = typeof(ReportView).GetField("_canvasReport", Fields);

        private readonly HashSet<int> _generatedIds = new();
        private readonly Dictionary<int, ReportCheck> _checks = new();
        private readonly Queue<ReportSO> _scanQueue = new();
        private List<ReportSO> _allReports = new();
        private List<EmployeeImmutableData> _employeeData = new();
        private HashSet<int> _normalIds = new();
        private HashSet<int> _spyIds = new();

        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _onlyProblems;
        private bool _scanRunning;
        private int _scanTotal;
        private double _nextTick;

        public static void Open() => GameplayRuntimeQAWindow.OpenReportCoverageTab();

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopScan();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                RefreshData();
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
                StopScan();
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
                return;

            ObserveGeneratedReports();
            if (!_scanRunning || EditorApplication.timeSinceStartup < _nextTick)
                return;

            _nextTick = EditorApplication.timeSinceStartup + ScanInterval;
            RunNextScan();
            Repaint();
        }

        private void ObserveGeneratedReports()
        {
            Project project = Company.Instance != null ? Company.Instance.curProject : null;
            if (project?.pendingReports == null)
                return;

            foreach (Report report in project.pendingReports)
            {
                if (report?.so == null)
                    continue;

                _generatedIds.Add(report.so.id);
                if (!_checks.ContainsKey(report.so.id))
                    _checks[report.so.id] = ValidateReportData(report.so, report.owner, false);
            }
        }

        private void StartScan()
        {
            if (!CanRunUiScan(out _))
                return;

            RefreshData();
            _scanQueue.Clear();
            foreach (ReportSO report in _allReports.OrderBy(report => report.id))
                _scanQueue.Enqueue(report);

            _scanTotal = _scanQueue.Count;
            _scanRunning = _scanTotal > 0;
            _nextTick = EditorApplication.timeSinceStartup;
        }

        private void RunNextScan()
        {
            if (_scanQueue.Count == 0)
            {
                StopScan();
                return;
            }

            ReportSO reportSo = _scanQueue.Dequeue();
            Employee owner = FindRuntimeEmployee(reportSo.role);
            ReportCheck check = ValidateReportData(reportSo, owner, true);

            if (owner != null)
                ValidateReportUi(reportSo, owner, check.Problems);

            _checks[reportSo.id] = check;
            if (_scanQueue.Count == 0)
                StopScan();
        }

        private static void ValidateReportUi(ReportSO reportSo, Employee owner, List<string> problems)
        {
            ReportView view = UnityEngine.Object.FindFirstObjectByType<ReportView>(FindObjectsInactive.Include);
            if (view == null)
            {
                problems.Add("ReportView 없음");
                return;
            }

            int roleIndex = RoleIndex(reportSo.role);
            ReportCardView[] cards = roleIndex >= 0 ? view.GetCards(roleIndex) : null;
            if (cards == null || cards.Length == 0 || cards[0] == null)
            {
                problems.Add("직군별 ReportCardView 없음");
                return;
            }

            var report = new Report { so = reportSo, owner = owner };
            try
            {
                ReportCardView card = cards[0];
                card.Bind(report);
                CompareText(CardTitleField?.GetValue(card) as TMP_Text, reportSo.title, "카드 제목", problems);
                CompareText(CardNameField?.GetValue(card) as TMP_Text, owner.so.Name, "카드 작성자", problems);

                view.SetDetailInfo(report);
                CompareText(DetailTitleField?.GetValue(view) as TMP_Text, reportSo.title, "상세 제목", problems);
                CompareText(DetailNameField?.GetValue(view) as TMP_Text, owner.so.Name, "상세 작성자", problems);
                CompareText(DetailContentField?.GetValue(view) as TMP_Text, reportSo.content, "상세 본문", problems);
            }
            catch (Exception exception)
            {
                problems.Add($"UI 바인딩 예외: {exception.GetBaseException().Message}");
            }
        }

        private ReportCheck ValidateReportData(ReportSO report, Employee owner, bool automated)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(report.title)) problems.Add("제목 비어 있음");
            if (string.IsNullOrWhiteSpace(report.content)) problems.Add("본문 비어 있음");
            if (report.grade is < 1 or > 3) problems.Add($"등급 범위 오류: {report.grade}");
            if (report.startRepo is not (0 or 1)) problems.Add($"startRepo 범위 오류: {report.startRepo}");
            if (RoleIndex(report.role) < 0) problems.Add($"지원하지 않는 직군: {report.role}");
            if (!_normalIds.Contains(report.id) && !_spyIds.Contains(report.id)) problems.Add("ReportManager 목록에 미등록");
            if (!IsReachable(report)) problems.Add("현재 직원 테이블 조합으로 생성 불가");
            if (automated && owner == null) problems.Add($"UI 검사에 사용할 {report.role} 런타임 직원 없음");
            if (!automated && owner == null) problems.Add("생성된 보고서의 작성자 없음");
            else if (owner != null && owner.so.role != report.role) problems.Add("보고서 직군과 작성자 직군 불일치");

            return new ReportCheck
            {
                Id = report.id,
                Title = report.title,
                Role = report.role,
                Trait = report.trait,
                Grade = report.grade,
                StartRepo = report.startRepo,
                Automated = automated,
                Problems = problems
            };
        }

        private bool IsReachable(ReportSO report)
        {
            if (_spyIds.Contains(report.id))
                return _employeeData.Any(employee => employee.role == report.role);

            return _employeeData.Any(employee => employee.role == report.role &&
                (employee.mainTrait == report.trait || employee.subTrait == report.trait || employee.riskTrait == report.trait));
        }

        private static Employee FindRuntimeEmployee(Role role)
        {
            if (_EmployeeManager.Instance?.haveEmployees?.haveEmployeeList == null)
                return null;
            return _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .FirstOrDefault(employee => employee?.so != null && employee.so.role == role);
        }

        private bool CanRunUiScan(out string reason)
        {
            reason = string.Empty;
            if (!EditorApplication.isPlaying) { reason = "Play Mode가 아닙니다."; return false; }
            if (ReportManager.Instance == null) { reason = "ReportManager가 없습니다."; return false; }

            ReportView view = UnityEngine.Object.FindFirstObjectByType<ReportView>(FindObjectsInactive.Include);
            if (view == null) { reason = "ReportView가 없습니다."; return false; }
            GameObject canvas = CanvasField?.GetValue(view) as GameObject;
            if (canvas != null && canvas.activeInHierarchy) { reason = "실제 보고서 화면을 먼저 닫아주세요."; return false; }
            return true;
        }

        private static void CompareText(TMP_Text text, string expected, string label, List<string> problems)
        {
            if (text == null) problems.Add($"{label} TMP 참조 없음");
            else if (text.text != (expected ?? string.Empty)) problems.Add($"{label} 출력 불일치");
        }

        private static int RoleIndex(Role role) => role switch
        {
            Role.PLANNER => 0,
            Role.ARTIST => 1,
            Role.PROGRAMMER => 2,
            _ => -1
        };

        private void RefreshData()
        {
            _allReports = FindAssets<ReportSO>();
            _employeeData = FindAssets<EmployeeImmutableData>();
            _normalIds.Clear();
            _spyIds.Clear();

            if (ReportManager.Instance != null)
            {
                if (ManagerNormalReportsField?.GetValue(ReportManager.Instance) is List<ReportSO> normal)
                    _normalIds = normal.Where(report => report != null).Select(report => report.id).ToHashSet();
                if (ManagerSpyReportsField?.GetValue(ReportManager.Instance) is List<ReportSO> spy)
                    _spyIds = spy.Where(report => report != null).Select(report => report.id).ToHashSet();
            }
            else
            {
                _normalIds = _allReports.Select(report => report.id).ToHashSet();
            }
        }

        private static List<T> FindAssets<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();

        private void StopScan()
        {
            _scanRunning = false;
            _scanQueue.Clear();
        }

        private void OnGUI() => DrawEmbeddedGUI();

        internal void DrawEmbeddedGUI()
        {
            DrawToolbar();
            DrawGuide();
            DrawSummary();
            DrawResults();
        }

        private void DrawToolbar()
        {
            CanRunUiScan(out string reason);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(reason) || _scanRunning))
                {
                    if (GUILayout.Button("UI 전수 검사", EditorStyles.toolbarButton, GUILayout.Width(95f))) StartScan();
                }
                if (_scanRunning && GUILayout.Button("검사 중지", EditorStyles.toolbarButton, GUILayout.Width(75f))) StopScan();
                _onlyProblems = GUILayout.Toggle(_onlyProblems, "문제만", EditorStyles.toolbarButton, GUILayout.Width(65f));
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(105f))) RefreshData();
                if (GUILayout.Button("기록 지우기", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    _generatedIds.Clear();
                    _checks.Clear();
                    _scanTotal = 0;
                }
            }
        }

        private void DrawGuide()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("보고서 생성 및 UI 커버리지", EditorStyles.boldLabel);
            CanRunUiScan(out string reason);
            EditorGUILayout.HelpBox(
                string.IsNullOrEmpty(reason)
                    ? "일반 플레이에서 pendingReports에 생성된 보고서를 자동 기록합니다. UI 전수 검사는 모든 ReportSO를 카드·상세 화면에 바인딩하며 프로젝트 점수와 직원 상태는 변경하지 않습니다."
                    : $"UI 전수 검사 대기: {reason}",
                string.IsNullOrEmpty(reason) ? MessageType.Info : MessageType.Warning);
        }

        private void DrawSummary()
        {
            int total = _allReports.Select(report => report.id).Distinct().Count();
            int failed = _checks.Values.Count(check => !check.Passed);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"전체 {total:N0} / 확인 {_checks.Count:N0} / 실제 생성 {_generatedIds.Count:N0} / PASS {_checks.Count - failed:N0} / FAIL {failed:N0}",
                    EditorStyles.boldLabel);
                if (_scanRunning)
                {
                    int done = _scanTotal - _scanQueue.Count;
                    Rect rect = EditorGUILayout.GetControlRect(false, 18f);
                    EditorGUI.ProgressBar(rect, _scanTotal > 0 ? done / (float)_scanTotal : 0f, $"전수 검사 {done:N0}/{_scanTotal:N0}");
                }
            }
        }

        private void DrawResults()
        {
            IEnumerable<ReportCheck> results = _checks.Values.OrderBy(check => check.Id);
            if (_onlyProblems) results = results.Where(check => !check.Passed);
            if (!string.IsNullOrWhiteSpace(_search))
            {
                string term = _search.Trim();
                results = results.Where(check => check.Id.ToString().Contains(term) ||
                    (check.Title?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    check.Role.ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    check.Trait.ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (ReportCheck check in results)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"{(check.Passed ? "PASS" : "FAIL")} ID {check.Id} / {check.Role} / {check.Trait} / G{check.Grade} / start {check.StartRepo}",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(check.Title ?? string.Empty, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField(_generatedIds.Contains(check.Id) ? "실제 플레이 생성 확인" : "실제 플레이 미확인", EditorStyles.miniLabel);
                    foreach (string problem in check.Problems) EditorGUILayout.HelpBox(problem, MessageType.Error);
                }
            }
            if (!_checks.Any()) EditorGUILayout.HelpBox("아직 기록된 보고서가 없습니다.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private sealed class ReportCheck
        {
            public int Id;
            public string Title;
            public Role Role;
            public Trait Trait;
            public int Grade;
            public int StartRepo;
            public bool Automated;
            public List<string> Problems = new();
            public bool Passed => Problems.Count == 0;
        }
    }
}
