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
        private Dictionary<(int, EmployeeDialogueState), DialoguePoolEntrySO> _poolMap = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
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
            foreach (DialoguePoolEntrySO entry in _allPoolEntries)
            {
                if (entry == null) continue;
                _poolMap[(entry.employeeId, entry.empStatusReq)] = entry;
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
            if (_poolMap.TryGetValue((employeeId, state), out DialoguePoolEntrySO entry)) return entry;
            Debug.LogWarning($"[DialogueDataManager] 풀 항목 없음 — employeeId={employeeId}, state={state}");
            return null;
        }
    }
}