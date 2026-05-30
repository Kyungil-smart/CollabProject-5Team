# Dialogue 로직 레이어 작업 내역

- 작성자 : 이승열
- 날짜 : 2026.05.26

---

## 적용 스크립트

- `WIP/LSY/Scripts/NeglectPenaltyHandler.cs`
- `WIP/LSY/Scripts/DialogueManager.cs`

---

## 현재 상태

**NeglectPenaltyHandler.cs**
- 금요일 밤 방치 패널티 발행 완료
  - 충성도 -5 즉시 적용
  - 피로도 +10 다음 주 적용 (금요일 밤 → 월요일 전환 시 `ApplyPendingFatigue()` 호출)

---

## 외부 연결 필요

- 금요일 밤 `NeglectPenaltyHandler.ApplyNeglectPenalty(neglectedIds)` 호출
- `DialogueManager.Instance.StartDialogueById(employeeId, npcName)` 호출
