using System.Collections.Generic;
using UnityEngine;

/// <summary> StoryQuestNodeSO / StoryQuestPoolSO 목록을 모아 ID로 조회하게 해주는 매니저 </summary>
public class StoryQuestDataManager : MonoBehaviour
{
    public static StoryQuestDataManager Instance { get; private set; }

    [Header("StoryQuest SO 목록")]
    [SerializeField] List<StoryQuestNodeSO> _allNodes = new();

    [Header("StoryQuest_Pool SO 목록")]
    [SerializeField] List<StoryQuestPoolSO> _allPoolEntries = new();

    private Dictionary<int, StoryQuestNodeSO> _nodeMap = new();
    private Dictionary<int, StoryQuestPoolSO> _poolMap = new();

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
        foreach (StoryQuestNodeSO node in _allNodes)
        {
            if (node == null) continue;
            _nodeMap[node.id] = node;
        }

        _poolMap.Clear();
        foreach (StoryQuestPoolSO entry in _allPoolEntries)
        {
            if (entry == null) continue;
            _poolMap[entry.id] = entry;
        }
    }

    public StoryQuestNodeSO GetNode(int nodeId)
    {
        if (_nodeMap.TryGetValue(nodeId, out StoryQuestNodeSO node)) return node;
        return null;
    }

    public StoryQuestPoolSO GetPoolEntry(int poolId)
    {
        if (_poolMap.TryGetValue(poolId, out StoryQuestPoolSO entry)) return entry;
        return null;
    }

    public List<StoryQuestPoolSO> GetAllPoolEntries() => _allPoolEntries;
}