using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

public class StoryQuestManager : MonoBehaviour
{
    public static StoryQuestManager Instance { get; private set; }

    public delegate void SpySelectDelegate(List<Employee> employees, Action<Employee, bool> onSelected);
    public event SpySelectDelegate OnSpySelect;

    const int FirstStoryQuestId = 1001;
    const int FirstHireQuestId = 1002;
    const int SpyQuestStartId = 1003;
    const int LargeProjectSpyQuestId = 1004;
    const int SelectSpyQuestId = 1042; // 스파이 퀘스트중 선형적 진행이 끝나고 스파이 결정 선택지가 나오는 퀘스트
    const int CorrectSpyResultQuestId = 1043;
    const int WrongSpyResultQuestId = 1044;
    const int CorrectSpyEpilogueQuestId = 1045;
    const int WrongSpyEpilogueQuestId = 1046;
    const int EndingQuestId = 1047;

    [SerializeField] Sprite StoryBookBubbleSprite;

    public ReactiveProperty<QuestState> storyQuestState = new(QuestState.Ready);
    public ReactiveProperty<int> storyQuestProgress = new(0);

    public StoryQuest curStoryQuest;
    public int curSpyQuestID;
    public List<int> completedStoryQuestIds = new();
    public bool isCorrectSpySelected;
    public Employee selectedSpyEmployee;

    Employee _currentSpeaker;  // NPC1
    Employee _currentSpeaker2; // NPC2
    SpeechBubble _currentBubble;

    const string FlowerTag = "Flower";
    const string DeskTag = "Desk";
    const string StoryBubbleMessage = "<b>...</b>";

    #region Init
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // 테스트 코드 (삭제예정)~
        OnSpySelect += LogSpySelect;
    }
    void LogSpySelect(List<Employee> employees, Action<Employee, bool> onSelected)
    {
        Debug.Log("잡았다 요놈");
    }
    // ~---
    public void ResetForNewDay()
    {
        if (_currentBubble != null)
        {
            Destroy(_currentBubble.gameObject);
            _currentBubble = null;
        }

        curStoryQuest = null;
        _currentSpeaker = null;
        _currentSpeaker2 = null;
        storyQuestProgress.Value = 0;
        storyQuestState.Value = QuestState.Ready;
    }
    #endregion

    #region Start Story Quest
    public bool TryStartStoryQuestForToday()
    {
        StoryQuestPoolSO questSO = SelectStartableQuest();
        return questSO != null && StartStoryQuest(questSO);
    }

    StoryQuestPoolSO SelectStartableQuest()
    {
        if (curSpyQuestID > 0)
            return SelectCurrentSpyQuest(); // 스파이 퀘스트 진행중이면 여기

        StoryQuestPoolSO normalQuest = SelectNormalStoryQuest(); // 일반 퀘스트 조건체크
        if (normalQuest != null) return normalQuest;

        return SelectSpyStartQuest(); // 스파이시작 퀘스트 조건체크
    }

    StoryQuestPoolSO SelectCurrentSpyQuest()
    {
        if (curSpyQuestID > EndingQuestId) return null;

        StoryQuestPoolSO spyQuest = StoryQuestDataManager.Instance.GetPoolEntry(curSpyQuestID);
        return IsSpyQuestConditionSatisfied(spyQuest) ? spyQuest : null;
    }

    StoryQuestPoolSO SelectNormalStoryQuest()
    {
        foreach (StoryQuestPoolSO questSO in StoryQuestDataManager.Instance.GetAllPoolEntries())
        {
            if (questSO.isSpyQuest) continue;
            if (completedStoryQuestIds.Contains(questSO.id)) continue;
            if (!IsNormalStoryConditionSatisfied(questSO)) continue;

            return questSO;
        }
        return null;
    }

    StoryQuestPoolSO SelectSpyStartQuest()
    {
        if (completedStoryQuestIds.Contains(SpyQuestStartId)) return null;

        StoryQuestPoolSO spyStartQuest = StoryQuestDataManager.Instance.GetPoolEntry(SpyQuestStartId);
        return IsSpyQuestConditionSatisfied(spyStartQuest) ? spyStartQuest : null;
    }

    bool StartStoryQuest(StoryQuestPoolSO questSO)
    {
        Transform bubbleTarget = ResolveBubbleTarget(questSO.id);
        if (bubbleTarget == null) return false;

        if (questSO.isSpyQuest || questSO.id == SpyQuestStartId)
            curSpyQuestID = questSO.id;

        if (questSO.id == LargeProjectSpyQuestId)
            _EmployeeManager.Instance.canLeaveSelf = false;

        curStoryQuest = new StoryQuest();
        curStoryQuest.Init(questSO);
        curStoryQuest.state = QuestState.Playing;
        storyQuestState.Value = QuestState.Playing;

        _currentBubble = IsStoryBookBubbleQuest(questSO.id)
            ? QuestManager.Instance.ShowClickableSpeechBubble(bubbleTarget, StoryBookBubbleSprite, StartCurrentStoryDialogue)
            : QuestManager.Instance.ShowClickableSpeechBubble(bubbleTarget, StoryBubbleMessage, StartCurrentStoryDialogue);

        return true;
    }
    #endregion

    #region Condition Check
    bool IsNormalStoryConditionSatisfied(StoryQuestPoolSO questSO)
    {
        return questSO.id switch
        {
            FirstStoryQuestId => Company.Instance.completedProjects.Count > 0,
            FirstHireQuestId => _EmployeeManager.Instance.lastHiredEmployee != null,
            _ => false
        };
    }

    bool IsSpyQuestConditionSatisfied(StoryQuestPoolSO questSO)
    {
        return questSO.id switch
        {
            SpyQuestStartId => Company.Instance.level == 3,
            LargeProjectSpyQuestId => Company.Instance.activeProjectCount.Value > 0 &&
                                      Company.Instance.curProject.Scale == ProjectSize.Large,
            EndingQuestId => Company.Instance.reputation >= questSO.conditionReputation,
            _ => true
        };
    }
    #endregion

    #region Dialogue
    // 말풍선 버튼 띄워줄 객체 결정
    private Transform ResolveBubbleTarget(int questId)
    {
        _currentSpeaker = null;
        _currentSpeaker2 = null;

        if (questId == FirstHireQuestId)
        {
            _currentSpeaker = _EmployeeManager.Instance.lastHiredEmployee;
            _currentSpeaker2 = GameManager.Instance.GetRandomActiveEmployee();
            return _currentSpeaker.transform;
        }

        if (questId == SpyQuestStartId)
        {
            _currentSpeaker = GameManager.Instance.GetRandomActiveEmployee();
            _currentSpeaker2 = GameManager.Instance.GetRandomActiveEmployee();

            return GameObject.FindWithTag(FlowerTag).transform;
        }

        if (questId == SelectSpyQuestId)
        {
            _currentSpeaker = GameManager.Instance.GetRandomActiveEmployee();
            _currentSpeaker2 = GameManager.Instance.GetRandomActiveEmployee();

            return GameObject.FindWithTag(DeskTag).transform;
        }

        _currentSpeaker = GameManager.Instance.GetRandomActiveEmployee();
        _currentSpeaker2 = GameManager.Instance.GetRandomActiveEmployee();
        return _currentSpeaker.transform;
    }
    // 스토리북 띄워야하는 퀘스트인지 확인
    bool IsStoryBookBubbleQuest(int questId)
    {
        return questId == SpyQuestStartId || questId == SelectSpyQuestId;
    }

    private void StartCurrentStoryDialogue()
    {
        _currentBubble = null;

        var speakers = new Dictionary<string, Employee>
        {
            ["NPC1"] = _currentSpeaker,
            ["NPC2"] = _currentSpeaker2,
            ["UCSPY"] = selectedSpyEmployee,
            ["SPY"] = GetSpyEmployee(),
        };

        StoryDialoguePlayer.Instance.StartStoryDialogue(
            curStoryQuest.so.startDialogueId,
            speakers,
            CompleteCurrentStoryQuest);
    }
    #endregion

    #region SPY GetSet
    /// <summary>
    /// [원리 설명] 외부(Presenter 등)에서 특정 직원을 스파이로 의심하여 판정을 요청할 때 사용하는 검증 인터페이스입니다.
    /// 실제 데이터 원본(GetSpyEmployee)을 외부에 노출(Public)하지 않고, 참/거짓 결과만 안전하게 반환하여 데이터 오염을 방지합니다.
    /// </summary>
    public bool CheckIsSpy(Employee targetEmployee)
    {
        if (targetEmployee == null) return false;

        return targetEmployee == GetSpyEmployee();
    }

    Employee GetSpyEmployee()
    {
        if (Company.Instance.activeProjectCount.Value > 0)
        {
            foreach (Employee employee in Company.Instance.curProject.GetAllEmployees())
            {
                if (employee.isSpy)
                    return employee;
            }
        }

        foreach (Employee employee in _EmployeeManager.Instance.haveEmployees.haveEmployeeList)
        {
            if (employee.isSpy)
                return employee;
        }

        return null;
    }
    #endregion

    #region Complete Quest
    private void CompleteCurrentStoryQuest()
    {
        if (curStoryQuest.state != QuestState.Playing) return;

        int completedQuestId = curStoryQuest.so.id;
        bool isSpyQuest = curStoryQuest.so.isSpyQuest;
        curStoryQuest.Complete();
        ApplyReward(curStoryQuest.Reward);

        storyQuestProgress.Value = curStoryQuest.curCount;
        storyQuestState.Value = curStoryQuest.state;

        if (!completedStoryQuestIds.Contains(completedQuestId))
            completedStoryQuestIds.Add(completedQuestId);

        if (completedQuestId == SelectSpyQuestId)
        {
            OnSpySelect?.Invoke(Company.Instance.curProject.GetAllEmployees(), CompleteSpySelection);
            return;
        }

        if (isSpyQuest)
            curSpyQuestID = GetNextSpyQuestId(completedQuestId);

        if (completedQuestId == EndingQuestId)
        {
            // TODO: 마지막 스토리 퀘스트 완료 후 일 종료가 아니라 엔딩 씬으로 전환
            return;
        }

        DateTimeManager.Instance.CompleteDayWork();
    }

    void CompleteSpySelection(Employee selectedEmployee, bool isCorrect)
    {
        selectedSpyEmployee = selectedEmployee;
        isCorrectSpySelected = isCorrect;
        curSpyQuestID = isCorrect ? CorrectSpyResultQuestId : WrongSpyResultQuestId;
        DateTimeManager.Instance.CompleteDayWork();
    }

    int GetNextSpyQuestId(int completedQuestId)
    {
        return completedQuestId switch
        {
            CorrectSpyResultQuestId => CorrectSpyEpilogueQuestId,
            WrongSpyResultQuestId => WrongSpyEpilogueQuestId,
            CorrectSpyEpilogueQuestId => EndingQuestId,
            WrongSpyEpilogueQuestId => EndingQuestId,
            EndingQuestId => -1, // 마지막 퀘스트라 다음 퀘스트 id는 -1로 설정
            _ => completedQuestId + 1
        };
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
    #endregion

    #region Save/Load
    public void ExportStoryQuestData(SaveData data)
    {
        data.completedStoryQuestIds = new List<int>(completedStoryQuestIds);
        data.curSpyQuestID = curSpyQuestID;
        data.selectedSpyEmployeeId = selectedSpyEmployee != null ? selectedSpyEmployee.so.id : 0;
    }

    public void ImportStoryQuestData(SaveData data)
    {
        completedStoryQuestIds.Clear();

        if (data.completedStoryQuestIds != null)
            completedStoryQuestIds.AddRange(data.completedStoryQuestIds);

        curSpyQuestID = data.curSpyQuestID;
        selectedSpyEmployee = _EmployeeManager.Instance.haveEmployees.haveEmployeeList.Find(e => e.so.id == data.selectedSpyEmployeeId);

        ResetForNewDay();
    }
    #endregion
}
