using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dialogue;
using R3;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class DialogueCoverageRuntimeQAWindow : EditorWindow
    {
        private const double ScanInterval = 0.01d;

        private readonly HashSet<int> _playedNodeIds = new();
        private readonly Dictionary<int, NodeCheck> _nodeChecks = new();
        private readonly Queue<ScanTarget> _scanQueue = new();
        private List<DialogueNodeSO> _allNodes = new();
        private List<DialoguePoolEntrySO> _allPools = new();
        private Dictionary<int, DialoguePoolEntrySO> _nodeOwners = new();

        private IDisposable _readySubscription;
        private Vector2 _scrollPosition;
        private string _search = string.Empty;
        private bool _showOnlyProblems;
        private bool _scanRunning;
        private int _scanTotal;
        private double _nextScanTime;

        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly FieldInfo CurrentNodeIdField = typeof(DialogueManager).GetField("_currentNodeId", InstanceFlags);
        private static readonly FieldInfo CurrentEmployeeIdField = typeof(DialogueManager).GetField("_currentEmployeeId", InstanceFlags);
        private static readonly FieldInfo CurrentStateField = typeof(DialogueManager).GetField("_currentState", InstanceFlags);
        private static readonly FieldInfo CurrentPoolField = typeof(DialogueManager).GetField("_currentPoolEntry", InstanceFlags);
        private static readonly FieldInfo IsRunningField = typeof(DialogueManager).GetField("_isDialogueRunning", InstanceFlags);
        private static readonly FieldInfo CurrentViewField = typeof(DialogueManager).GetField("_currentView", InstanceFlags);
        private static readonly FieldInfo Choice01Field = typeof(DialogueManager).GetField("_choiceItem01", InstanceFlags);
        private static readonly FieldInfo Choice02Field = typeof(DialogueManager).GetField("_choiceItem02", InstanceFlags);
        private static readonly MethodInfo ShowNodeMethod = typeof(DialogueManager).GetMethod("ShowNode", InstanceFlags);
        private static readonly MethodInfo HideAllMethod = typeof(DialogueManager).GetMethod("HideAll", InstanceFlags);
        private static readonly FieldInfo DialogueTextField = typeof(DialogueBaseView).GetField("_dialogueText", InstanceFlags);
        private static readonly FieldInfo ChoiceTextField = typeof(ChoiceItemView).GetField("_choiceText", InstanceFlags);
        private static readonly FieldInfo EmployeeNameTextField = typeof(EmployeeDialogueView).GetField("_nameText", InstanceFlags);
        private static readonly FieldInfo PlayerNameTextField = typeof(PlayerDialogueView).GetField("_nameText", InstanceFlags);

        public static void Open() => GameplayRuntimeQAWindow.OpenDialogueCoverageTab();

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RefreshDataCache();
            TrySubscribe();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopScan(_scanRunning);
            DisposeSubscription();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                TrySubscribe();
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopScan(false);
                DisposeSubscription();
            }

            Repaint();
        }

        private void TrySubscribe()
        {
            if (!EditorApplication.isPlaying || _readySubscription != null)
                return;

            _readySubscription = DialogueEvents.OnDialogueReady.Subscribe(OnDialogueReady);
        }

        private void DisposeSubscription()
        {
            _readySubscription?.Dispose();
            _readySubscription = null;
        }

        private void OnDialogueReady(DialogueStartPayload payload)
        {
            DialogueManager manager = DialogueManager.Instance;
            int nodeId = ReadInt(CurrentNodeIdField, manager);
            if (nodeId <= 0)
                return;

            _playedNodeIds.Add(nodeId);
            NodeCheck check = ValidateCurrentView(nodeId, payload, false);
            if (_nodeChecks.TryGetValue(nodeId, out NodeCheck previous) && previous.Automated)
                check.Automated = true;
            _nodeChecks[nodeId] = check;
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
                return;

            TrySubscribe();
            if (!_scanRunning || EditorApplication.timeSinceStartup < _nextScanTime)
                return;

            _nextScanTime = EditorApplication.timeSinceStartup + ScanInterval;
            RunNextScanTarget();
            Repaint();
        }

        private void StartScan()
        {
            if (!EditorApplication.isPlaying || DialogueManager.Instance == null || DialogueDataManager.Instance == null)
                return;

            if (IsDialogueRunning())
            {
                Debug.LogWarning("[Dialogue Coverage QA] 진행 중인 대화를 끝낸 뒤 UI 전수 검사를 실행하세요.");
                return;
            }

            StopScan(false);
            RefreshDataCache();
            DialoguePoolEntrySO fallbackPool = _allPools.FirstOrDefault(pool => pool != null);

            foreach (DialogueNodeSO node in _allNodes.Where(node => node != null).OrderBy(node => node.id))
            {
                _nodeOwners.TryGetValue(node.id, out DialoguePoolEntrySO owner);
                _scanQueue.Enqueue(new ScanTarget
                {
                    Node = node,
                    Pool = owner != null ? owner : fallbackPool,
                    HasPoolRoute = owner != null
                });
            }

            _scanTotal = _scanQueue.Count;
            _scanRunning = _scanTotal > 0;
            _nextScanTime = EditorApplication.timeSinceStartup;
        }

        private void RunNextScanTarget()
        {
            if (_scanQueue.Count == 0)
            {
                StopScan(true);
                return;
            }

            DialogueManager manager = DialogueManager.Instance;
            ScanTarget target = _scanQueue.Dequeue();
            if (manager == null || target.Node == null || ShowNodeMethod == null)
            {
                StopScan(false);
                return;
            }

            DialoguePoolEntrySO pool = target.Pool;
            int employeeId = pool != null ? pool.employeeId : 0;
            EmployeeDialogueState state = pool != null ? pool.empStatusReq : EmployeeDialogueState.Normal;

            IsRunningField?.SetValue(manager, true);
            CurrentEmployeeIdField?.SetValue(manager, employeeId);
            CurrentStateField?.SetValue(manager, state);
            CurrentPoolField?.SetValue(manager, pool);
            ShowNodeMethod.Invoke(manager, new object[] { target.Node.id });

            DialogueBaseView view = CurrentViewField?.GetValue(manager) as DialogueBaseView;
            if (view != null && view.IsTyping)
                view.SkipTyping();

            DialogueStartPayload payload = ToPayload(target.Node, employeeId, state);
            NodeCheck check = ValidateCurrentView(target.Node.id, payload, true);
            check.HasPoolRoute = target.HasPoolRoute;
            _nodeChecks[target.Node.id] = check;
            _playedNodeIds.Add(target.Node.id);

            if (_scanQueue.Count == 0)
                StopScan(true);
        }

        private void StopScan(bool hideDialogue)
        {
            _scanRunning = false;
            _scanQueue.Clear();
            if (hideDialogue && DialogueManager.Instance != null)
            {
                HideAllMethod?.Invoke(DialogueManager.Instance, null);
                IsRunningField?.SetValue(DialogueManager.Instance, false);
                CurrentPoolField?.SetValue(DialogueManager.Instance, null);
                CurrentEmployeeIdField?.SetValue(DialogueManager.Instance, 0);
                CurrentNodeIdField?.SetValue(DialogueManager.Instance, 0);
            }
        }

        private static bool IsDialogueRunning() =>
            DialogueManager.Instance != null && IsRunningField?.GetValue(DialogueManager.Instance) is true;

        private static NodeCheck ValidateCurrentView(int nodeId, DialogueStartPayload payload, bool automated)
        {
            DialogueManager manager = DialogueManager.Instance;
            DialogueBaseView view = CurrentViewField?.GetValue(manager) as DialogueBaseView;
            TextMeshProUGUI dialogueText = view != null ? DialogueTextField?.GetValue(view) as TextMeshProUGUI : null;

            var problems = new List<string>();
            if (view == null) problems.Add("활성 대화 View 없음");
            else if (!view.gameObject.activeInHierarchy) problems.Add("대화 View 비활성");
            if (dialogueText == null) problems.Add("본문 TMP 참조 없음");
            else if (dialogueText.text != (payload.text ?? string.Empty)) problems.Add("본문 출력 불일치");

            if (view != null)
            {
                bool correctView = payload.isUser ? view is PlayerDialogueView : view is EmployeeDialogueView;
                if (!correctView) problems.Add(payload.isUser ? "플레이어 View 불일치" : "직원 View 불일치");

                if (correctView)
                {
                    TextMeshProUGUI nameText = payload.isUser
                        ? PlayerNameTextField?.GetValue(view) as TextMeshProUGUI
                        : EmployeeNameTextField?.GetValue(view) as TextMeshProUGUI;
                    if (nameText == null) problems.Add("화자명 TMP 참조 없음");
                    else if (nameText.text != (payload.desc ?? string.Empty)) problems.Add("화자명 출력 불일치");
                }
            }

            if (automated && payload.isChoice)
            {
                ValidateChoice(Choice01Field?.GetValue(manager) as ChoiceItemView, payload.choice01, "선택지 1", problems);
                ValidateChoice(Choice02Field?.GetValue(manager) as ChoiceItemView, payload.choice02, "선택지 2", problems);
            }

            return new NodeCheck
            {
                NodeId = nodeId,
                Speaker = payload.desc,
                Text = payload.text,
                IsChoice = payload.isChoice,
                Automated = automated,
                Problems = problems
            };
        }

        private static void ValidateChoice(ChoiceItemView item, string expected, string label, List<string> problems)
        {
            bool shouldShow = !string.IsNullOrEmpty(expected);
            if (item == null)
            {
                if (shouldShow) problems.Add($"{label} View 없음");
                return;
            }

            if (item.gameObject.activeInHierarchy != shouldShow)
                problems.Add($"{label} 표시 상태 불일치");

            TextMeshProUGUI choiceText = ChoiceTextField?.GetValue(item) as TextMeshProUGUI;
            if (shouldShow && (choiceText == null || choiceText.text != expected))
                problems.Add($"{label} 문구 불일치");
        }

        private static DialogueStartPayload ToPayload(DialogueNodeSO node, int employeeId, EmployeeDialogueState state) =>
            new DialogueStartPayload
            {
                employeeId = employeeId,
                state = state,
                desc = node.desc,
                text = node.text,
                isChoice = node.isChoice,
                isUser = node.isUser,
                choice01 = node.choice01,
                choice02 = node.choice02
            };

        private static int ReadInt(FieldInfo field, object target) =>
            field?.GetValue(target) is int value ? value : 0;

        private void RefreshDataCache()
        {
            _allNodes = AssetDatabase.FindAssets("t:DialogueNodeSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DialogueNodeSO>)
                .Where(node => node != null)
                .ToList();
            _allPools = AssetDatabase.FindAssets("t:DialoguePoolEntrySO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DialoguePoolEntrySO>)
                .Where(pool => pool != null)
                .ToList();
            _nodeOwners = BuildNodeOwnerMap(_allNodes, _allPools);
        }

        private static Dictionary<int, DialoguePoolEntrySO> BuildNodeOwnerMap(
            IEnumerable<DialogueNodeSO> allNodes,
            IEnumerable<DialoguePoolEntrySO> allPools)
        {
            Dictionary<int, DialogueNodeSO> nodes = allNodes.GroupBy(node => node.id).ToDictionary(group => group.Key, group => group.First());
            var owners = new Dictionary<int, DialoguePoolEntrySO>();

            foreach (DialoguePoolEntrySO pool in allPools)
            {
                var pending = new Stack<int>();
                var visited = new HashSet<int>();
                pending.Push(pool.talkId);

                while (pending.Count > 0)
                {
                    int id = pending.Pop();
                    if (id <= 0 || id == 20000 || !visited.Add(id) || !nodes.TryGetValue(id, out DialogueNodeSO node))
                        continue;

                    if (!owners.ContainsKey(id)) owners[id] = pool;
                    if (node.isChoice)
                    {
                        pending.Push(node.nextId01);
                        pending.Push(node.nextId02);
                    }
                    else
                    {
                        pending.Push(node.nextId);
                    }
                }
            }

            return owners;
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
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || _scanRunning || IsDialogueRunning()))
                {
                    if (GUILayout.Button("UI 전수 검사", EditorStyles.toolbarButton, GUILayout.Width(95f))) StartScan();
                }

                if (_scanRunning && GUILayout.Button("검사 중지", EditorStyles.toolbarButton, GUILayout.Width(75f)))
                    StopScan(true);

                GUILayout.Space(8f);
                _showOnlyProblems = GUILayout.Toggle(_showOnlyProblems, "문제만", EditorStyles.toolbarButton, GUILayout.Width(65f));
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("기록 지우기", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                {
                    _playedNodeIds.Clear();
                    _nodeChecks.Clear();
                    _scanTotal = 0;
                }

                if (GUILayout.Button("데이터 새로고침", EditorStyles.toolbarButton, GUILayout.Width(105f)))
                    RefreshDataCache();

                if (GUILayout.Button("미확인 ID 복사", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    CopyUntestedIds();
            }
        }

        private void DrawGuide()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("대화 실제 출력 및 UI 커버리지", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                EditorApplication.isPlaying
                    ? "일반 플레이에서 출력된 대화 ID를 자동 기록합니다. 'UI 전수 검사'는 모든 DialogueNode를 실제 Dialogue View에 순차 바인딩해 본문·화자 View·선택지 표시를 확인하며 보상은 적용하지 않습니다."
                    : "GameScene을 Play Mode로 실행한 뒤 사용하세요. 일반 플레이 기록과 자동 UI 전수 검사 결과를 분리해서 확인할 수 있습니다.",
                MessageType.Info);
        }

        private void DrawSummary()
        {
            int total = _allNodes.Select(node => node.id).Distinct().Count();
            int checkedCount = _nodeChecks.Count;
            int failed = _nodeChecks.Values.Count(check => !check.Passed);
            int manual = _nodeChecks.Values.Count(check => !check.Automated);
            int automated = _nodeChecks.Values.Count(check => check.Automated);
            float coverage = total > 0 ? checkedCount * 100f / total : 0f;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"전체 {total:N0} / 확인 {checkedCount:N0} ({coverage:0.0}%) / 미확인 {Mathf.Max(0, total - checkedCount):N0} / PASS {checkedCount - failed:N0} / FAIL {failed:N0}",
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"일반 플레이 감지 {manual:N0} / 자동 UI 검사 {automated:N0}", EditorStyles.miniLabel);

                if (_scanRunning)
                {
                    int done = _scanTotal - _scanQueue.Count;
                    Rect rect = EditorGUILayout.GetControlRect(false, 18f);
                    EditorGUI.ProgressBar(rect, _scanTotal > 0 ? done / (float)_scanTotal : 0f, $"전수 검사 {done:N0}/{_scanTotal:N0}");
                }
            }
        }

        private void CopyUntestedIds()
        {
            HashSet<int> checkedIds = _nodeChecks.Keys.ToHashSet();
            IEnumerable<int> untested = _allNodes.Select(node => node.id).Distinct()
                .Where(id => !checkedIds.Contains(id)).OrderBy(id => id);
            EditorGUIUtility.systemCopyBuffer = string.Join(",", untested);
        }

        private void DrawResults()
        {
            IEnumerable<NodeCheck> results = _nodeChecks.Values.OrderBy(check => check.NodeId);
            if (_showOnlyProblems) results = results.Where(check => !check.Passed);
            if (!string.IsNullOrWhiteSpace(_search))
            {
                string term = _search.Trim();
                results = results.Where(check => check.NodeId.ToString().Contains(term) ||
                    (check.Speaker?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (check.Text?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (NodeCheck check in results)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"{(check.Passed ? "PASS" : "FAIL")}  ID {check.NodeId} / {check.Speaker} / {(check.Automated ? "자동 검사" : "일반 플레이")}",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(check.Text ?? string.Empty, EditorStyles.wordWrappedMiniLabel);
                    if (!check.HasPoolRoute && check.Automated)
                        EditorGUILayout.HelpBox("어떤 대화 풀에서도 이 노드로 도달할 수 없습니다.", MessageType.Warning);
                    foreach (string problem in check.Problems)
                        EditorGUILayout.HelpBox(problem, MessageType.Error);
                }
            }

            if (!_nodeChecks.Any())
                EditorGUILayout.HelpBox("아직 기록된 대화가 없습니다.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private sealed class ScanTarget
        {
            public DialogueNodeSO Node;
            public DialoguePoolEntrySO Pool;
            public bool HasPoolRoute;
        }

        private sealed class NodeCheck
        {
            public int NodeId;
            public string Speaker;
            public string Text;
            public bool IsChoice;
            public bool Automated;
            public bool HasPoolRoute = true;
            public List<string> Problems = new();
            public bool Passed => Problems.Count == 0;
        }
    }
}
