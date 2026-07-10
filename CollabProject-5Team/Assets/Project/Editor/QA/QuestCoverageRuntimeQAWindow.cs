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
    public sealed class QuestCoverageRuntimeQAWindow : EditorWindow
    {
        private const double ScanInterval = 0.03d;
        private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly FieldInfo PresenterViewField = typeof(QuestPresenter).GetField("_view", Fields);
        private static readonly FieldInfo AlertPopupField = typeof(QuestView).GetField("_dailyQuestAlertPopup", Fields);
        private static readonly FieldInfo AlertTypeField = typeof(QuestView).GetField("_alertQuestTypeLabel", Fields);
        private static readonly FieldInfo AlertNameField = typeof(QuestView).GetField("_alertQuestNameLabel", Fields);
        private static readonly FieldInfo AlertProgressField = typeof(QuestView).GetField("_alertProgressLabel", Fields);

        private readonly HashSet<int> _generatedIds = new();
        private readonly Dictionary<int, QuestCheck> _checks = new();
        private readonly Queue<QuestSO> _scanQueue = new();
        private List<QuestSO> _allQuests = new();
        private HashSet<int> _managerQuestIds = new();

        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _onlyProblems;
        private bool _scanRunning;
        private int _scanTotal;
        private double _nextTick;
        private int _lastObservedQuestId;

        public static void Open() => GameplayRuntimeQAWindow.OpenQuestCoverageTab();

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
            StopScan(_scanRunning);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                RefreshData();
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
            {
                StopScan(false);
                _lastObservedQuestId = 0;
            }
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
                return;

            ObserveCurrentQuest();
            if (!_scanRunning || EditorApplication.timeSinceStartup < _nextTick)
                return;

            _nextTick = EditorApplication.timeSinceStartup + ScanInterval;
            RunNextScan();
            Repaint();
        }

        private void ObserveCurrentQuest()
        {
            DailyQuest current = QuestManager.Instance?.curDailyQuest;
            if (current?.so == null)
                return;

            QuestSO quest = current.so;
            _generatedIds.Add(quest.id);
            if (_lastObservedQuestId == quest.id)
                return;

            QuestCheck check = ValidateQuestData(quest, false);
            QuestView view = FindQuestView();
            GameObject popup = view != null ? AlertPopupField?.GetValue(view) as GameObject : null;
            if (popup != null && popup.activeInHierarchy)
                ValidateAlertUi(view, quest, current.curCount, current.TargetCount, check.Problems);
            else
                check.UiObserved = false;

            _checks[quest.id] = check;
            _lastObservedQuestId = quest.id;
        }

        private void StartScan()
        {
            if (!CanRunUiScan(out _))
                return;

            RefreshData();
            _scanQueue.Clear();
            foreach (QuestSO quest in _allQuests.OrderBy(quest => quest.id))
                _scanQueue.Enqueue(quest);

            _scanTotal = _scanQueue.Count;
            _scanRunning = _scanTotal > 0;
            _nextTick = EditorApplication.timeSinceStartup;
        }

        private void RunNextScan()
        {
            if (_scanQueue.Count == 0)
            {
                StopScan(true);
                return;
            }

            QuestSO quest = _scanQueue.Dequeue();
            QuestCheck check = ValidateQuestData(quest, true);
            QuestView view = FindQuestView();
            if (view == null)
            {
                check.Problems.Add("QuestView 없음");
            }
            else
            {
                try
                {
                    view.ShowDailyQuestAlert(quest.Name, 0, quest.targetCount);
                    ValidateAlertUi(view, quest, 0, quest.targetCount, check.Problems);
                    view.HideDailyQuestAlert();
                    check.UiObserved = true;
                }
                catch (Exception exception)
                {
                    check.Problems.Add($"UI 바인딩 예외: {exception.GetBaseException().Message}");
                }
            }

            _checks[quest.id] = check;
            if (_scanQueue.Count == 0)
                StopScan(true);
        }

        private QuestCheck ValidateQuestData(QuestSO quest, bool automated)
        {
            var problems = new List<string>();
            if (string.IsNullOrWhiteSpace(quest.Name)) problems.Add("퀘스트 이름 비어 있음");
            if (quest.controlType == ControlType.NONE) problems.Add("1단계 조작 방식 NONE");
            if (quest.targetCount <= 0) problems.Add($"1단계 목표 수치 오류: {quest.targetCount}");
            if (quest.controlType2 == ControlType.NONE && quest.targetCount2 != 0)
                problems.Add("2단계 조작은 NONE인데 목표 수치가 존재함");
            if (quest.controlType2 != ControlType.NONE && quest.targetCount2 <= 0)
                problems.Add("2단계 조작이 있지만 목표 수치가 0 이하");
            if (quest.successEffect <= 0) problems.Add($"성공 보정값 오류: {quest.successEffect}");
            if (quest.role is not (Role.PLANNER or Role.ARTIST or Role.PROGRAMMER))
                problems.Add($"지원하지 않는 직군: {quest.role}");
            if (!_managerQuestIds.Contains(quest.id)) problems.Add("QuestManager.dailyQuests에 미등록");

            return new QuestCheck
            {
                Id = quest.id,
                Name = quest.Name,
                Role = quest.role,
                Control = quest.controlType,
                Target = quest.targetCount,
                Control2 = quest.controlType2,
                Target2 = quest.targetCount2,
                Reward = quest.successEffect,
                Automated = automated,
                Problems = problems
            };
        }

        private static void ValidateAlertUi(QuestView view, QuestSO quest, int current, int total, List<string> problems)
        {
            GameObject popup = AlertPopupField?.GetValue(view) as GameObject;
            if (popup == null) problems.Add("일일 퀘스트 알림 팝업 참조 없음");
            else if (!popup.activeInHierarchy) problems.Add("일일 퀘스트 알림 팝업 비활성");

            CompareText(AlertTypeField?.GetValue(view) as TMP_Text, "일일 퀘스트", "퀘스트 종류", problems);
            CompareText(AlertNameField?.GetValue(view) as TMP_Text, quest.Name, "퀘스트 이름", problems);
            CompareText(AlertProgressField?.GetValue(view) as TMP_Text, $"{current} / {total}", "진행도", problems);
        }

        private static void CompareText(TMP_Text text, string expected, string label, List<string> problems)
        {
            if (text == null) problems.Add($"{label} TMP 참조 없음");
            else if (text.text != (expected ?? string.Empty)) problems.Add($"{label} 출력 불일치");
        }

        private static QuestView FindQuestView()
        {
            QuestPresenter presenter = UnityEngine.Object.FindFirstObjectByType<QuestPresenter>(FindObjectsInactive.Include);
            if (presenter != null && PresenterViewField?.GetValue(presenter) is QuestView presenterView)
                return presenterView;
            return UnityEngine.Object.FindFirstObjectByType<QuestView>(FindObjectsInactive.Include);
        }

        private bool CanRunUiScan(out string reason)
        {
            reason = string.Empty;
            if (!EditorApplication.isPlaying) { reason = "Play Mode가 아닙니다."; return false; }
            if (QuestManager.Instance == null) { reason = "QuestManager가 없습니다."; return false; }
            QuestView view = FindQuestView();
            if (view == null) { reason = "QuestView가 없습니다."; return false; }
            GameObject popup = AlertPopupField?.GetValue(view) as GameObject;
            if (popup != null && popup.activeInHierarchy) { reason = "현재 퀘스트 알림을 먼저 닫아주세요."; return false; }
            return true;
        }

        private void RefreshData()
        {
            _allQuests = AssetDatabase.FindAssets("t:QuestSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<QuestSO>)
                .Where(quest => quest != null)
                .ToList();
            _managerQuestIds = QuestManager.Instance?.dailyQuests != null
                ? QuestManager.Instance.dailyQuests.Where(quest => quest != null).Select(quest => quest.id).ToHashSet()
                : _allQuests.Select(quest => quest.id).ToHashSet();
        }

        private void StopScan(bool hideAlert)
        {
            _scanRunning = false;
            _scanQueue.Clear();
            if (hideAlert && EditorApplication.isPlaying)
                FindQuestView()?.HideDailyQuestAlert();
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
                if (_scanRunning && GUILayout.Button("검사 중지", EditorStyles.toolbarButton, GUILayout.Width(75f))) StopScan(true);
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
            EditorGUILayout.LabelField("퀘스트 생성 및 UI 커버리지", EditorStyles.boldLabel);
            CanRunUiScan(out string reason);
            EditorGUILayout.HelpBox(
                string.IsNullOrEmpty(reason)
                    ? "일반 플레이에서 실제 선택된 일일 퀘스트를 기록합니다. UI 전수 검사는 모든 QuestSO의 알림 제목·이름·진행도를 확인하며 퀘스트 완료나 보상을 강제로 적용하지 않습니다. 진행·보상은 전체 흐름 탭에서 실제 플레이로 검증합니다."
                    : $"UI 전수 검사 대기: {reason}",
                string.IsNullOrEmpty(reason) ? MessageType.Info : MessageType.Warning);
        }

        private void DrawSummary()
        {
            int total = _allQuests.Select(quest => quest.id).Distinct().Count();
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
            IEnumerable<QuestCheck> results = _checks.Values.OrderBy(check => check.Id);
            if (_onlyProblems) results = results.Where(check => !check.Passed);
            if (!string.IsNullOrWhiteSpace(_search))
            {
                string term = _search.Trim();
                results = results.Where(check => check.Id.ToString().Contains(term) ||
                    (check.Name?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    check.Role.ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (QuestCheck check in results)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"{(check.Passed ? "PASS" : "FAIL")} ID {check.Id} / {check.Role} / {check.Control} {check.Target}" +
                        (check.Control2 != ControlType.NONE ? $" → {check.Control2} {check.Target2}" : string.Empty),
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{check.Name} / 보정 +{check.Reward}", EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField(_generatedIds.Contains(check.Id) ? "실제 플레이 생성 확인" : "실제 플레이 미확인", EditorStyles.miniLabel);
                    if (!check.UiObserved && !check.Automated)
                        EditorGUILayout.HelpBox("실제 생성은 확인했지만 알림 UI가 열린 시점은 관찰하지 못했습니다.", MessageType.Warning);
                    foreach (string problem in check.Problems) EditorGUILayout.HelpBox(problem, MessageType.Error);
                }
            }
            if (!_checks.Any()) EditorGUILayout.HelpBox("아직 기록된 퀘스트가 없습니다.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private sealed class QuestCheck
        {
            public int Id;
            public string Name;
            public Role Role;
            public ControlType Control;
            public int Target;
            public ControlType Control2;
            public int Target2;
            public int Reward;
            public bool Automated;
            public bool UiObserved;
            public List<string> Problems = new();
            public bool Passed => Problems.Count == 0;
        }
    }
}
