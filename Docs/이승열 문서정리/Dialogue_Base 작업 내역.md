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
- `DialogueChoice` 열거형 완료
- `StatDelta` 구조체 완료
- `MotivationWeight` 구조체 완료
- `DialogueStartPayload` 구조체 완료
  - TODO: 대화 텍스트, 선택지 데이터 미포함 (추후 추가 예정)

**DialogueEvents.cs**
- R3 기반 이벤트 채널 5개 완료
  - `OnStatChangeRequested`, `OnNightWeightReady`, `OnVacationPromised`, `OnBonusPayRequested`, `OnDialogueReady`

**DialogueManager.cs**
- 싱글톤 완료
- `StartDialogue(int employeeId, EmployeeDialogueState state)` 완료
- `SubmitChoice(int selectedIndex)` 완료

---

## 외부 연결 필요

- `DialogueEvents.OnStatChangeRequested` 구독 → Employee 스탯(의욕/피로도/충성도) 반영
- `DialogueEvents.OnBonusPayRequested` 구독 → 보너스 지급 시 자금 차감 처리
- `DialogueEvents.OnVacationPromised` 구독 → 이번 주 보고서 채택 불가 처리

---

- `DialogueEvents.OnDialogueReady` 구독 → 대화 팝업 표시
- 선택지 결정 후 `DialogueManager.Instance.SubmitChoice(index)` 호출

---

- `DialogueManager.Instance.StartDialogue(employeeId, state)` 호출
