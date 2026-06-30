using System.Collections.Generic;
using R3;
using UnityEngine;

public class StoryQuestManager : MonoBehaviour
{
    public static StoryQuestManager Instance { get; private set; }

    const int FirstStoryQuestId = 1001;
    const int FirstHireQuestId  = 1002;
    const int SpyQuestStartId = 1003;
    const int SelectSpyQuestId = 1041; // 스파이 퀘스트중 선형적 진행이 끝나고 스파이 결정 선택지가 나오는 퀘스트
    const string StoryBubbleMessage = "<b>...</b>";

    public ReactiveProperty<QuestState> storyQuestState = new(QuestState.Ready);
    public ReactiveProperty<int> storyQuestProgress = new(0);

    public StoryQuest curStoryQuest;
    public int curSpyQuestID;
    public List<int> completedStoryQuestIds = new();

    private Employee _currentSpeaker;
    private SpeechBubble _currentBubble;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

    public bool TryStartStoryQuestForToday()
    {
        StoryQuestPoolSO questSO = SelectStartableQuest();
        return questSO != null && StartStoryQuest(questSO);
    }

    StoryQuestPoolSO SelectStartableQuest()
    {
        if (curSpyQuestID != 0) // 스파이 퀘스트 분기 점검
        {
            if (curSpyQuestID > SelectSpyQuestId) return null;

            StoryQuestPoolSO spyQuest = StoryQuestDataManager.Instance.GetPoolEntry(curSpyQuestID);
            return IsConditionSatisfied(spyQuest) ? spyQuest : null;
        }

        foreach (StoryQuestPoolSO questSO in StoryQuestDataManager.Instance.GetAllPoolEntries())
        {
            if (questSO.isSpyQuest) continue;
            if (completedStoryQuestIds.Contains(questSO.id)) continue;
            if (!IsConditionSatisfied(questSO)) continue;

            return questSO;
        }
        return null;
    }

    bool StartStoryQuest(StoryQuestPoolSO questSO)
    {
        Transform bubbleTarget = ResolveBubbleTarget(questSO.id);
        if (bubbleTarget == null) return false;

        if (questSO.isSpyQuest)
            curSpyQuestID = questSO.id;

        curStoryQuest = new StoryQuest();
        curStoryQuest.Init(questSO);
        curStoryQuest.state = QuestState.Playing;
        storyQuestState.Value = QuestState.Playing;

        _currentBubble = QuestManager.Instance.ShowClickableSpeechBubble(
            bubbleTarget,
            StoryBubbleMessage,
            StartCurrentStoryDialogue);

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

    // 일반 스토리 퀘스트 조건 체크
    bool IsConditionSatisfied(StoryQuestPoolSO questSO)
    {
        return questSO.id switch
        {
            FirstStoryQuestId => Company.Instance.completedProjects.Count > 0,
            FirstHireQuestId => _EmployeeManager.Instance.lastHiredEmployee != null,
            SpyQuestStartId => Company.Instance.level < questSO.conditionCompanyLv,
            _ => false
        };
    }


    private void StartCurrentStoryDialogue()
    {
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
        bool completedSpyQuest = curStoryQuest.so.isSpyQuest;
        curStoryQuest.Complete();
        ApplyReward(curStoryQuest.Reward);

        storyQuestProgress.Value = curStoryQuest.curCount;
        storyQuestState.Value = curStoryQuest.state;

        if (!completedStoryQuestIds.Contains(completedQuestId))
            completedStoryQuestIds.Add(completedQuestId);

        if (completedSpyQuest)
            curSpyQuestID = completedQuestId < SelectSpyQuestId ? completedQuestId + 1 : 0; // 스파이 선택 퀘스트 차별

        DateTimeManager.Instance.CompleteDayWork();
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
        data.completedStoryQuestIds = new List<int>(completedStoryQuestIds);
        data.curSpyQuestID = curSpyQuestID;
    }

    public void ImportStoryQuestData(SaveData data)
    {
        completedStoryQuestIds.Clear();

        if (data.completedStoryQuestIds != null)
            completedStoryQuestIds.AddRange(data.completedStoryQuestIds);

        curSpyQuestID = data.curSpyQuestID;

        ResetForNewDay();
    }
    #endregion
}
