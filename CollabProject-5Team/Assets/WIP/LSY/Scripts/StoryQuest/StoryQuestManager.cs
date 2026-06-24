using System.Collections.Generic;
using UnityEngine;


public class StoryQuestManager : MonoBehaviour
{
    public static StoryQuestManager Instance { get; private set; }

    private HashSet<int> _completedQuestIds = new();

    public int CurrentQuestId { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public StoryQuestPoolSO FindAvailableNextQuest()
    {
        foreach (StoryQuestPoolSO quest in StoryQuestDataManager.Instance.GetAllPoolEntries())
        {
            if (quest == null) continue;
            if (_completedQuestIds.Contains(quest.id)) continue;
            if (!IsParentSatisfied(quest)) continue;
            if (!IsConditionSatisfied(quest)) continue;
            return quest;
        }
        return null;
    }

    private bool IsParentSatisfied(StoryQuestPoolSO quest)
    {
        if (quest.parentIds == null || quest.parentIds.Length == 0) return true;

        foreach (int parentId in quest.parentIds)
        {
            if (parentId == 0) return true;
            if (_completedQuestIds.Contains(parentId)) return true;
        }
        return false;
    }

    private bool IsConditionSatisfied(StoryQuestPoolSO quest)
    {
        if (Company.Instance == null) return false;
        if (Company.Instance.level < quest.conditionCompanyLv) return false;
        if (Company.Instance.gold.Value < quest.conditionGold) return false;
        if (Company.Instance.reputation < quest.conditionReputation) return false;
        return true;
    }

    public void SetCurrentQuest(StoryQuestPoolSO quest)
    {
        CurrentQuestId = quest != null ? quest.id : 0;
    }

    public void CompleteCurrentQuest()
    {
        if (CurrentQuestId == 0) return;
        _completedQuestIds.Add(CurrentQuestId);
        CurrentQuestId = 0;
    }

    public bool IsQuestCompleted(int questId) => _completedQuestIds.Contains(questId);

    public void LoadCompletedQuestIds(IEnumerable<int> ids)
    {
        _completedQuestIds = new HashSet<int>(ids);
    }

    public List<int> GetCompletedQuestIdsForSave() => new List<int>(_completedQuestIds);
}