# Dialogue 타팀코드 수정 내역

- 작성자 : 이승열
- 날짜 : 2026.05.29

대화 시스템 연결을 위해 담당 파일에 추가/수정된 내용 기록

---

## 1. `DateTimeManager.cs`

### 추가된 필드

```csharp
// 이번 주에 대화한 직원 ID 목록 (방치 패널티 판정용)
private HashSet<int> _talkedEmployeesThisWeek = new HashSet<int>();
```

### 추가된 메서드

```csharp
// NPCInteract state==1 진입 시 호출 — 이번 주 대화 직원으로 등록
public void MarkTalkedThisWeek(int employeeId)
{
    _talkedEmployeesThisWeek.Add(employeeId);
}

// 이번 주 미대화 직원 ID 목록 반환
private List<int> GetNeglectedEmployeeIds(List<int> allEmployeeIds)
{
    List<int> neglected = new List<int>();
    foreach (int id in allEmployeeIds)
    {
        if (!_talkedEmployeesThisWeek.Contains(id))
            neglected.Add(id);
    }
    return neglected;
}
```

### `OnClickEndDayButton()` 수정

```csharp
else if (currentDay.Value == DayOfWeek.Friday && currentTime.Value == TimeOfDay.Night)
{
    var allIds = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
        .ConvertAll(e => e.so.id);
    var neglectedIds = GetNeglectedEmployeeIds(allIds);

    Dialogue.NeglectPenaltyHandler.ApplyNeglectPenalty(neglectedIds); // 충성도 -5

    currentWeek.Value++;
    currentDay.Value = DayOfWeek.Monday;
    currentTime.Value = TimeOfDay.Day;

    Dialogue.NeglectPenaltyHandler.ApplyPendingFatigue(); // 피로도 +10 다음 주 적용
    _talkedEmployeesThisWeek.Clear();                     // 주간 기록 초기화

    ResetDayStatus();
}
```

---

## 2. `_EmployeeManager.cs`

### `Start()`에 구독 추가

```csharp
private void Start()
{
    // 대화 결과로 인한 의욕 / 피로도 / 충성도 변경 구독
    Dialogue.DialogueEvents.OnStatChangeRequested
        .Subscribe(delta =>
        {
            Employee emp = _haveEmployees.haveEmployeeList
                .Find(e => e.so.id == delta.employeeId);
            if (emp == null) return;

            var data = emp.MutableData;
            data.desire  = Mathf.Clamp(data.desire  + delta.desireDelta,  0, 100);
            data.fatigue = Mathf.Clamp(data.fatigue + delta.fatigueDelta, 0, 100);
            data.loyalty = Mathf.Clamp(data.loyalty + delta.loyaltyDelta, 0, 100);
            emp.MutableData = data;
        })
        .AddTo(this);
}
```

---

## 3. `NPCInteract.cs`

### 필드 변경

```csharp
// 변경 전: int 직접 입력
[SerializeField] private int employeeId;

// 변경 후: SO 참조 (구글 시트 ID 변경 시 SO만 업데이트하면 자동 반영)
[SerializeField] private EmployeeImmutableData employeeSO;
```

### state==1 분기 변경

```csharp
// 변경 전 (임시 플레이스홀더)
else if (state == 1)
{
    dialogueText.text = $"{npcName}: Quest (Special Dialogue)";
    if (questCompleteButton != null)
    {
        questCompleteButton.gameObject.SetActive(true);
        questCompleteButton.onClick.RemoveAllListeners();
        questCompleteButton.onClick.AddListener(() => OnClickQuestComplete());
    }
}

// 변경 후 (DialogueManager 위임)
else if (state == 1)
{
    // 업무 완료 후 첫 대화 → DialogueManager에 위임
    DateTimeManager.Instance.MarkTalkedThisWeek(employeeSO.id); // 주간 기록
    Dialogue.DialogueManager.Instance.StartDialogueById(employeeSO.id, npcName);
    return; // interactionUI.SetActive(true) 건너뜀
}
```
