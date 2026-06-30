using System.Collections.Generic;
using R3;
using UnityEngine;

public class StoryQuestManager : MonoBehaviour
{
    public static StoryQuestManager Instance { get; private set; }

    private const int FirstStoryQuestId = 1001;
    private const int FirstHireQuestId = 1002;
    private const string StoryBubbleMessage = "<b>...</b>";

    public ReactiveProperty<QuestState> storyQuestState = new(QuestState.Ready);
    public ReactiveProperty<int> storyQuestProgress = new(0);

    public StoryQuest curStoryQuest;
    public int curQuestId = FirstStoryQuestId;

    private Employee _currentSpeaker;
    private SpeechBubble _currentBubble;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryStartStoryQuestForToday()
    {
        StoryQuestPoolSO questSO = StoryQuestDataManager.Instance.GetPoolEntry(curQuestId);
        if (questSO == null) return false;
        if (!IsConditionSatisfied(questSO)) return false;

        return StartStoryQuest(questSO);
    }

    public void ResetForNewDay()
    {
        if (_currentBubble != null)
        {
            Destroy(_currentBubble.gameObject);
            _currentBubble = null;
        }

        curStoryQuest = null;
        _currentSpeaker = null;
        storyQuestProgress.Value = 0;
        storyQuestState.Value = QuestState.Ready;
    }

    private bool StartStoryQuest(StoryQuestPoolSO questSO)
    {
        Transform bubbleTarget = ResolveBubbleTarget(questSO.id);

        curStoryQuest = new StoryQuest();
        curStoryQuest.Init(questSO);
        curStoryQuest.SetReady();
        curStoryQuest.StartQuest();

        storyQuestProgress.Value = curStoryQuest.curCount;

        _currentBubble = QuestManager.Instance.ShowClickableSpeechBubble(
            bubbleTarget,
            StoryBubbleMessage,
            StartCurrentStoryDialogue);

        storyQuestState.Value = curStoryQuest.state;
        return true;
    }

    // 말풍선 버튼 띄워줄 객체 결정
    private Transform ResolveBubbleTarget(int questId)
    {
        _currentSpeaker = null;

        if (questId == FirstHireQuestId)
        {
            _currentSpeaker = _EmployeeManager.Instance.lastHiredEmployee;
            return _currentSpeaker.transform;
        }

        _currentSpeaker = GameManager.Instance.GetRandomActiveEmployee();
        return _currentSpeaker.transform;
    }

    private void StartCurrentStoryDialogue()
    {
        if (curStoryQuest.state != QuestState.Playing) return;

        _currentBubble = null;

        var speakers = new Dictionary<string, Employee>
        {
            ["NPC1"] = _currentSpeaker
        };

        StoryDialoguePlayer.Instance.StartStoryDialogue(
            curStoryQuest.so.startDialogueId,
            speakers,
            CompleteCurrentStoryQuest);
    }

    private void CompleteCurrentStoryQuest()
    {
        if (curStoryQuest.state != QuestState.Playing) return;

        int completedQuestId = curStoryQuest.so.id;
        curStoryQuest.Complete();
        ApplyReward(curStoryQuest.Reward);

        storyQuestProgress.Value = curStoryQuest.curCount;
        storyQuestState.Value = curStoryQuest.state;

        curQuestId = completedQuestId + 1;
        if (completedQuestId == FirstHireQuestId)
            _EmployeeManager.Instance.lastHiredEmployee = null;

        DateTimeManager.Instance.CompleteDayWork();
    }

    // 스토리 퀘스트 조건 체크
    bool IsConditionSatisfied(StoryQuestPoolSO questSO)
    {
        if (Company.Instance.level < questSO.conditionCompanyLv) return false;
        if (Company.Instance.gold.Value < questSO.conditionGold) return false;
        if (Company.Instance.reputation < questSO.conditionReputation) return false;

        switch (questSO.id)
        {
            case FirstStoryQuestId: // 1001
                return Company.Instance.completedProjects.Count > 0;
            case FirstHireQuestId:  // 1002
                return _EmployeeManager.Instance.lastHiredEmployee != null;

            default:
                return false;
        }
    }

    private void ApplyReward(QuestReward reward)
    {
        switch (reward.type)
        {
            case QuestRewardType.Gold:
                Company.Instance.gold.Value += reward.amount;
                Company.Instance.curManagementStatus.otherIncome += reward.amount;
                Company.Instance.cumulativeManagementStatus.otherIncome += reward.amount;
                Company.Instance.curManagementStatus.Recalculate();
                Company.Instance.cumulativeManagementStatus.Recalculate();
                break;
        }
    }

    #region Save/Load
    public void ExportStoryQuestData(SaveData data)
    {
        data.storyQuestId = curQuestId <= 0 ? FirstStoryQuestId : curQuestId;
    }

    public void ImportStoryQuestData(SaveData data)
    {
        curQuestId = data.storyQuestId <= 0 ? FirstStoryQuestId : data.storyQuestId;
        ResetForNewDay();
    }
    #endregion
}
