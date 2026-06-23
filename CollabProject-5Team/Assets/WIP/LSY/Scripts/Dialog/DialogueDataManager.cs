using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    public class DialogueDataManager : MonoBehaviour
    {
        public static DialogueDataManager Instance { get; private set; }

        [Header("Talk_Dialogue SO 목록")]
        [SerializeField] List<DialogueNodeSO> _allNodes = new();

        [Header("Talk_Dialogue_Pool SO 목록")]
        [SerializeField] List<DialoguePoolEntrySO> _allPoolEntries = new();

        private Dictionary<int, DialogueNodeSO> _nodeMap = new();
        private Dictionary<(int, EmployeeDialogueState), List<DialoguePoolEntrySO>> _poolMap = new();

        // 직전에 나온 항목 기억 (다음 뽑기에서 제외)
        private Dictionary<(int, EmployeeDialogueState), DialoguePoolEntrySO> _poolLastPicked = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildMaps();
        }

        void BuildMaps()
        {
            _nodeMap.Clear();
            foreach (DialogueNodeSO node in _allNodes)
            {
                if (node == null) continue;
                _nodeMap[node.id] = node;
            }

            _poolMap.Clear();
            _poolLastPicked.Clear();
            foreach (DialoguePoolEntrySO entry in _allPoolEntries)
            {
                if (entry == null) continue;
                (int, EmployeeDialogueState) key = (entry.employeeId, entry.empStatusReq);
                if (!_poolMap.TryGetValue(key, out List<DialoguePoolEntrySO> list))
                {
                    list = new List<DialoguePoolEntrySO>();
                    _poolMap[key] = list;
                }
                list.Add(entry);
            }
        }
        
        public DialogueNodeSO GetNode(int nodeId)
        {
            if (_nodeMap.TryGetValue(nodeId, out DialogueNodeSO node)) return node;
            Debug.LogWarning($"[DialogueDataManager] 노드 ID {nodeId} 없음");
            return null;
        }

        public DialoguePoolEntrySO GetPoolEntry(int employeeId, EmployeeDialogueState state)
        {
            (int, EmployeeDialogueState) key = (employeeId, state);

            if (!_poolMap.TryGetValue(key, out List<DialoguePoolEntrySO> fullList) || fullList.Count == 0)
            {
                Debug.LogWarning($"[DialogueDataManager] 풀 항목 없음 — employeeId={employeeId}, state={state}");
                return null;
            }

            _poolLastPicked.TryGetValue(key, out DialoguePoolEntrySO lastPicked);

            // 직전 항목을 제외한 후보 목록 구성 (풀이 1개면 제외 없이 그대로 사용)
            List<DialoguePoolEntrySO> candidates = fullList.Count > 1
                ? fullList.FindAll(e => e != lastPicked)
                : fullList;

            DialoguePoolEntrySO picked = candidates[Random.Range(0, candidates.Count)];
            _poolLastPicked[key] = picked;

            return picked;
        }
    }
}
