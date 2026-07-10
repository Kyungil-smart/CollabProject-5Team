using System;
using System.Collections.Generic;
using System.Reflection;
using Dialogue;
using R3;
using UnityEditor;
using UnityEngine;

namespace GameDevTycoon.EditorQA
{
    public sealed class DialogueRewardRuntimeQAWindow : EditorWindow
    {
        private const int MaxRecords = 100;

        private readonly Dictionary<int, StatSnapshot> _latestSnapshots = new();
        private readonly List<PendingVerification> _pending = new();
        private readonly List<VerificationRecord> _records = new();

        private IDisposable _statSubscription;
        private Vector2 _scrollPosition;
        private bool _captureEnabled = true;
        private bool _showPassed = true;
        private bool _showFailed = true;

        private static readonly FieldInfo CurrentPoolEntryField = typeof(DialogueManager).GetField(
            "_currentPoolEntry", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ChosenBranchField = typeof(DialogueManager).GetField(
            "_chosenBranch", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Open()
        {
            GameplayRuntimeQAWindow.OpenDialogueTab();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            TrySubscribe();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            DisposeSubscription();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _latestSnapshots.Clear();
                _pending.Clear();
                TrySubscribe();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                DisposeSubscription();
                _latestSnapshots.Clear();
                _pending.Clear();
            }

            Repaint();
        }

        private void TrySubscribe()
        {
            if (!EditorApplication.isPlaying || _statSubscription != null)
                return;

            RefreshSnapshots();
            _statSubscription = DialogueEvents.OnStatChangeRequested.Subscribe(OnStatChangeRequested);
        }

        private void DisposeSubscription()
        {
            _statSubscription?.Dispose();
            _statSubscription = null;
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
                return;

            TrySubscribe();

            if (_pending.Count > 0)
            {
                for (int i = 0; i < _pending.Count; i++)
                    CompleteVerification(_pending[i]);

                _pending.Clear();
                Repaint();
            }

            RefreshSnapshots();
        }

        private void OnStatChangeRequested(StatDelta delta)
        {
            if (!_captureEnabled)
                return;

            Employee employee = FindEmployee(delta.employeeId);
            StatSnapshot before = _latestSnapshots.TryGetValue(delta.employeeId, out StatSnapshot cached)
                ? cached
                : ReadSnapshot(employee);

            ReadDialogueContext(out DialoguePoolEntrySO pool, out int branch);
            string effectScript = GetEffectScript(pool, branch);
            ParsedEffect parsed = ParseEffect(effectScript);

            _pending.Add(new PendingVerification
            {
                EmployeeId = delta.employeeId,
                EmployeeName = employee != null && employee.so != null ? employee.so.Name : "직원 미발견",
                PoolId = pool != null ? pool.id : 0,
                TalkId = pool != null ? pool.talkId : 0,
                Branch = branch,
                EffectScript = effectScript,
                Before = before,
                EventDelta = delta,
                Parsed = parsed
            });
        }

        private void CompleteVerification(PendingVerification pending)
        {
            Employee employee = FindEmployee(pending.EmployeeId);
            StatSnapshot actual = ReadSnapshot(employee);
            StatSnapshot expected = new StatSnapshot
            {
                IsValid = pending.Before.IsValid,
                Desire = Mathf.Clamp(pending.Before.Desire + pending.EventDelta.desireDelta, 0, 100),
                Fatigue = Mathf.Clamp(pending.Before.Fatigue + pending.EventDelta.fatigueDelta, 0, 100),
                Loyalty = Mathf.Clamp(pending.Before.Loyalty + pending.EventDelta.loyaltyDelta, 0, 100),
                Gold = pending.Before.Gold + pending.Parsed.Gold
            };

            bool contextValid = pending.PoolId != 0 && pending.Branch is 1 or 2 &&
                                !string.IsNullOrWhiteSpace(pending.EffectScript);
            bool parserMatch = pending.Parsed.IsValid &&
                               pending.Parsed.Desire == pending.EventDelta.desireDelta &&
                               pending.Parsed.Fatigue == pending.EventDelta.fatigueDelta &&
                               pending.Parsed.Loyalty == pending.EventDelta.loyaltyDelta;
            bool runtimeMatch = pending.Before.IsValid && actual.IsValid &&
                                expected.Desire == actual.Desire &&
                                expected.Fatigue == actual.Fatigue &&
                                expected.Loyalty == actual.Loyalty &&
                                expected.Gold == actual.Gold;

            _records.Insert(0, new VerificationRecord
            {
                Timestamp = DateTime.Now,
                EmployeeId = pending.EmployeeId,
                EmployeeName = pending.EmployeeName,
                PoolId = pending.PoolId,
                TalkId = pending.TalkId,
                Branch = pending.Branch,
                EffectScript = pending.EffectScript,
                Before = pending.Before,
                Expected = expected,
                Actual = actual,
                EventDelta = pending.EventDelta,
                Parsed = pending.Parsed,
                ContextValid = contextValid,
                ParserMatch = parserMatch,
                RuntimeMatch = runtimeMatch
            });

            if (_records.Count > MaxRecords)
                _records.RemoveRange(MaxRecords, _records.Count - MaxRecords);

            if (actual.IsValid)
                _latestSnapshots[pending.EmployeeId] = actual;
        }

        private static Employee FindEmployee(int employeeId)
        {
            if (_EmployeeManager.Instance == null || _EmployeeManager.Instance.haveEmployees == null)
                return null;

            return _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .Find(employee => employee != null && employee.so != null && employee.so.id == employeeId);
        }

        private void RefreshSnapshots()
        {
            if (_EmployeeManager.Instance == null || _EmployeeManager.Instance.haveEmployees == null)
                return;

            foreach (Employee employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
            {
                if (employee == null || employee.so == null)
                    continue;

                _latestSnapshots[employee.so.id] = ReadSnapshot(employee);
            }
        }

        private static StatSnapshot ReadSnapshot(Employee employee)
        {
            int gold = Company.Instance != null ? Company.Instance.gold.Value : 0;
            if (employee == null)
                return new StatSnapshot { IsValid = false, Gold = gold };

            EmployeeMutableData data = employee.MutableData;
            return new StatSnapshot
            {
                IsValid = true,
                Desire = data.desire,
                Fatigue = data.fatigue,
                Loyalty = data.loyalty,
                Gold = gold
            };
        }

        private static void ReadDialogueContext(out DialoguePoolEntrySO pool, out int branch)
        {
            pool = null;
            branch = 0;

            DialogueManager manager = DialogueManager.Instance;
            if (manager == null)
                return;

            pool = CurrentPoolEntryField?.GetValue(manager) as DialoguePoolEntrySO;
            if (ChosenBranchField?.GetValue(manager) is int selectedBranch)
                branch = selectedBranch;
        }

        private static string GetEffectScript(DialoguePoolEntrySO pool, int branch)
        {
            if (pool == null)
                return string.Empty;

            return branch == 1 ? pool.branch01Effect :
                   branch == 2 ? pool.branch02Effect : string.Empty;
        }

        private static ParsedEffect ParseEffect(string effectScript)
        {
            ParsedEffect result = new ParsedEffect { IsValid = !string.IsNullOrWhiteSpace(effectScript) };
            if (!result.IsValid)
                return result;

            string[] parts = effectScript.Split(',');
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                int sign;
                string[] pair;

                if (part.Contains("+="))
                {
                    sign = 1;
                    pair = part.Split(new[] { "+=" }, StringSplitOptions.None);
                }
                else if (part.Contains("-="))
                {
                    sign = -1;
                    pair = part.Split(new[] { "-=" }, StringSplitOptions.None);
                }
                else
                {
                    result.IsValid = false;
                    result.Error = $"파싱할 수 없는 항목: {part}";
                    continue;
                }

                if (pair.Length != 2 || !int.TryParse(pair[1].Trim(), out int value))
                {
                    result.IsValid = false;
                    result.Error = $"숫자 형식 오류: {part}";
                    continue;
                }

                value *= sign;
                switch (pair[0].Trim())
                {
                    case "desire": result.Desire += value; break;
                    case "fatigue": result.Fatigue += value; break;
                    case "loyalty": result.Loyalty += value; break;
                    case "gold": result.Gold += value; break;
                    default:
                        result.IsValid = false;
                        result.Error = $"알 수 없는 보상 항목: {pair[0].Trim()}";
                        break;
                }
            }

            return result;
        }

        private void OnGUI()
        {
            DrawEmbeddedGUI();
        }

        internal void DrawEmbeddedGUI()
        {
            DrawToolbar();
            DrawGuide();
            DrawSummary();
            DrawRecords();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _captureEnabled = GUILayout.Toggle(
                    _captureEnabled,
                    _captureEnabled ? "기록 중" : "기록 정지",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(90f));

                GUILayout.FlexibleSpace();

                _showPassed = GUILayout.Toggle(_showPassed, "PASS", EditorStyles.toolbarButton, GUILayout.Width(60f));
                _showFailed = GUILayout.Toggle(_showFailed, "FAIL", EditorStyles.toolbarButton, GUILayout.Width(60f));

                if (GUILayout.Button("결과 지우기", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    _records.Clear();
            }
        }

        private void DrawGuide()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("대화 선택지 보상 - 실제 플레이 검증", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode에서 이 창을 열어 둔 뒤 직원과 대화하고 선택지를 누르세요. 대화가 끝나 보상이 적용되면 결과가 자동 기록됩니다.",
                    MessageType.Info);
                return;
            }

            bool managerReady = _EmployeeManager.Instance != null && DialogueManager.Instance != null;
            EditorGUILayout.HelpBox(
                managerReady
                    ? "런타임 연결 완료. 직원 대화 선택지를 누른 뒤 대화를 끝까지 진행하면 테이블 효과와 실제 적용 결과가 비교됩니다."
                    : "EmployeeManager 또는 DialogueManager를 찾지 못했습니다. GameScene이 완전히 시작됐는지 확인하세요.",
                managerReady ? MessageType.Info : MessageType.Warning);
        }

        private void DrawSummary()
        {
            int passed = 0;
            int failed = 0;
            foreach (VerificationRecord record in _records)
            {
                if (record.Passed) passed++;
                else failed++;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"검증 결과: PASS {passed} / FAIL {failed} / 전체 {_records.Count}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    "검사 경로: 선택지 효과 문자열 → DialogueEffectParser → StatDelta 이벤트 → EmployeeMutableData/회사 자금",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawRecords()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_records.Count == 0)
            {
                EditorGUILayout.HelpBox("아직 기록된 선택지 보상이 없습니다.", MessageType.None);
            }

            foreach (VerificationRecord record in _records)
            {
                if ((record.Passed && !_showPassed) || (!record.Passed && !_showFailed))
                    continue;

                DrawRecord(record);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawRecord(VerificationRecord record)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string status = record.Passed ? "PASS" : "FAIL";
                MessageType messageType = record.Passed ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(
                    $"[{status}] {record.Timestamp:HH:mm:ss}  {record.EmployeeName} ({record.EmployeeId})  " +
                    $"Pool {record.PoolId} / Talk {record.TalkId} / 선택지 {record.Branch}",
                    messageType);

                EditorGUILayout.LabelField("원본 효과", string.IsNullOrEmpty(record.EffectScript) ? "(확인 불가)" : record.EffectScript);
                EditorGUILayout.LabelField(
                    "파싱 결과",
                    $"의욕 {FormatDelta(record.Parsed.Desire)} / 피로 {FormatDelta(record.Parsed.Fatigue)} / " +
                    $"충성 {FormatDelta(record.Parsed.Loyalty)} / 자금 {FormatDelta(record.Parsed.Gold)}");
                EditorGUILayout.LabelField("적용 전", FormatSnapshot(record.Before));
                EditorGUILayout.LabelField("예상 결과", FormatSnapshot(record.Expected));
                EditorGUILayout.LabelField("실제 결과", FormatSnapshot(record.Actual));

                if (!record.ContextValid)
                    EditorGUILayout.HelpBox("선택한 대화 풀 또는 분기 정보를 읽지 못했습니다.", MessageType.Warning);
                if (!record.Parsed.IsValid)
                    EditorGUILayout.HelpBox($"효과 문자열 파싱 실패: {record.Parsed.Error}", MessageType.Error);
                else if (!record.ParserMatch)
                    EditorGUILayout.HelpBox("효과 문자열의 값과 StatDelta 이벤트 값이 다릅니다.", MessageType.Error);
                if (!record.RuntimeMatch)
                    EditorGUILayout.HelpBox("예상 결과와 실제 직원 상태 또는 회사 자금이 다릅니다.", MessageType.Error);
            }
        }

        private static string FormatSnapshot(StatSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                return "직원 상태 확인 불가";

            return $"의욕 {snapshot.Desire} / 피로 {snapshot.Fatigue} / 충성 {snapshot.Loyalty} / 자금 {snapshot.Gold}";
        }

        private static string FormatDelta(int value) => value > 0 ? $"+{value}" : value.ToString();

        private struct StatSnapshot
        {
            public bool IsValid;
            public int Desire;
            public int Fatigue;
            public int Loyalty;
            public int Gold;
        }

        private struct ParsedEffect
        {
            public bool IsValid;
            public int Desire;
            public int Fatigue;
            public int Loyalty;
            public int Gold;
            public string Error;
        }

        private sealed class PendingVerification
        {
            public int EmployeeId;
            public string EmployeeName;
            public int PoolId;
            public int TalkId;
            public int Branch;
            public string EffectScript;
            public StatSnapshot Before;
            public StatDelta EventDelta;
            public ParsedEffect Parsed;
        }

        private sealed class VerificationRecord
        {
            public DateTime Timestamp;
            public int EmployeeId;
            public string EmployeeName;
            public int PoolId;
            public int TalkId;
            public int Branch;
            public string EffectScript;
            public StatSnapshot Before;
            public StatSnapshot Expected;
            public StatSnapshot Actual;
            public StatDelta EventDelta;
            public ParsedEffect Parsed;
            public bool ContextValid;
            public bool ParserMatch;
            public bool RuntimeMatch;
            public bool Passed => ContextValid && Parsed.IsValid && ParserMatch && RuntimeMatch;
        }
    }
}
