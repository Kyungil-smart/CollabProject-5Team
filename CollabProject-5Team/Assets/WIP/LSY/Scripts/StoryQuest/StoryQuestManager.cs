using System.Collections.Generic;
using R3;
using UnityEngine;

public class StoryQuestManager : MonoBehaviour
{
    public static StoryQuestManager Instance { get; private set; }

    private const int FirstStoryQuestId = 1001;
    private const int FirstHireQuestId = 1002;
    private const int LastImplementedStoryQuestId = 1002; // 마지막 구현된 스토리 ID
    private const string StoryBubbleMessage = "<b>...</b>";

    public ReactiveProperty<QuestState> storyQuestState = new(QuestState.Ready);
    public ReactiveProperty<int> storyQuestProgress = new(0);

    public StoryQuest curStoryQuest;
    public int curQuestId = FirstStoryQuestId;

    private int _lastHiredEmployeeId;
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
        if (curQuestId > LastImplementedStoryQuestId)
            return false;

        StoryQuestPoolSO questSO = StoryQuestDataManager.Instance.GetPoolEntry(curQuestId);
        if (questSO == null) return false;
        if (!IsConditionSatisfied(questSO)) return false;

        return StartStoryQuest(questSO);
    }

    public void NotifyEmployeeHired(Employee employee)
    {
        if (employee == null) return;
        _lastHiredEmployeeId = employee.so.id;
    }

    public void ResetForNewDay()
    {
        if (_currentBubble != null)
        {
            Destroy(_currentBubble.gameObject);
            _currentBubble = null;
        }

        DateTimeManager.Instance.isStoryQuest = false;

        curStoryQuest = null;
        _currentSpeaker = null;
        storyQuestProgress.Value = 0;
        storyQuestState.Value = QuestState.Ready;
    }

    private bool StartStoryQuest(StoryQuestPoolSO questSO)
    {
        Employee speaker = ResolveSpeaker(questSO.id);
        if (speaker == null)
        {
            Debug.LogWarning($"[StoryQuestManager] {questSO.id}번 스토리 퀘스트를 띄울 직원을 찾지 못했습니다.");
            return false;
        }

        curStoryQuest = new StoryQuest();
        curStoryQuest.Init(questSO);
        curStoryQuest.SetReady();
        curStoryQuest.StartQuest();

        _currentSpeaker = speaker;
        storyQuestProgress.Value = curStoryQuest.curCount;

        _currentBubble = QuestManager.Instance.ShowClickableSpeechBubble(
            speaker.transform,
            StoryBubbleMessage,
            StartCurrentStoryDialogue);

        if (_currentBubble == null)
        {
            curStoryQuest = null;
            _currentSpeaker = null;
            storyQuestProgress.Value = 0;
            return false;
        }

        DateTimeManager.Instance.isStoryQuest = true;

        storyQuestState.Value = curStoryQuest.state;
        return true;
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
            _lastHiredEmployeeId = 0;

        DateTimeManager.Instance.isStoryQuest = false;
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
                return Company.Instance.activeProjectCount.Value > 0;
            case FirstHireQuestId:  // 1002
                return HasHiredEmployeeBeyondDefaults();

            default:
                return false;
        }
    }

    private Employee ResolveSpeaker(int questId)
    {
        if (questId == FirstHireQuestId)
        {
            Employee hired = GameManager.Instance.GetActiveEmployee(_lastHiredEmployeeId);
            if (hired != null) return hired;

            hired = GetFirstActiveNonDefaultEmployee();
            if (hired != null) return hired;

            return null;
        }

        return GameManager.Instance.GetRandomActiveEmployee();
    }

    private bool HasHiredEmployeeBeyondDefaults()
    {
        if (_lastHiredEmployeeId != 0) return true;

        foreach (Employee employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            if (!IsDefaultEmployee(employee))
                return true;
        }

        return false;
    }

    private Employee GetFirstActiveNonDefaultEmployee()
    {
        if (_EmployeeManager.Instance == null || GameManager.Instance == null) return null;

        foreach (Employee employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            if (employee == null || IsDefaultEmployee(employee)) continue;

            Employee activeEmployee = GameManager.Instance.GetActiveEmployee(employee.so.id);
            if (activeEmployee != null)
                return activeEmployee;
        }

        return null;
    }

    private bool IsDefaultEmployee(Employee employee)
    {
        foreach (Employee defaultEmployee in _EmployeeManager.Instance.defaultEmployees)
        {
            if (defaultEmployee.so.id == employee.so.id)
                return true;
        }

        return false;
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
