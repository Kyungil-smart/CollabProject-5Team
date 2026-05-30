# Dialogue 기반 구조 작업 내역

- 작성자 : 이승열
- 날짜 : 2026.05.25

---

## 적용 스크립트

- `WIP/LSY/Scripts/DialogueData.cs`
- `WIP/LSY/Scripts/DialogueEvents.cs`
- `WIP/LSY/Scripts/DialogueManager.cs`

---

## 현재 상태

**DialogueData.cs**
- `EmployeeDialogueState` 열거형 완료
- `StatDelta` 구조체 완료
- `DialogueStartPayload` 구조체 완료

**DialogueEvents.cs**
- R3 기반 이벤트 채널 완료
  - `OnStatChangeRequested`, `OnVacationPromised`, `OnDialogueReady`, `OnDialogueEnded`

**DialogueManager.cs**
- 싱글톤 완료
- `StartDialogue(int employeeId, EmployeeDialogueState state)` 완료
- `SubmitChoice(int selectedIndex)` 완료

---

## 외부 연결 필요

- `DialogueEvents.OnStatChangeRequested` 구독 → Employee 스탯(의욕/피로도/충성도) 반영
- `DialogueEvents.OnVacationPromised` 구독 → 이번 주 보고서 채택 불가 처리

---

- `DialogueEvents.OnDialogueReady` 구독 → 대화 팝업 표시
- 선택지 결정 후 `DialogueManager.Instance.SubmitChoice(index)` 호출

---

- `DialogueManager.Instance.StartDialogue(employeeId, state)` 호출
