# Dialogue 로직 레이어 작업 내역

- 작성자 : 이승열
- 날짜 : 2026.05.26

---

## 적용 스크립트

- `WIP/LSY/Scripts/EmployeeStateChecker.cs`
- `WIP/LSY/Scripts/DialogueChoiceHandler.cs`
- `WIP/LSY/Scripts/DayTalkTracker.cs`
- `WIP/LSY/Scripts/NeglectPenaltyHandler.cs`
- `WIP/LSY/Scripts/DialogueManager.cs`

---

## 현재 상태

**EmployeeStateChecker.cs**
- 피로도·의욕 수치 →  판정 완료
  - Normal : 피로도 ≤ 50 AND 의욕 ≥ 50
  - Danger : 피로도 > 50 AND 의욕 < 50
  - Warning : 그 외 (둘 중 하나만 위험)
- Warning/Danger 여부 반환 완료
- 보고서 가중치 반환 완료 (+5 / 0 / -10)

**DialogueChoiceHandler.cs**
- 위기 대화(Warning/Danger) 선택지 처리 완료
  - 의욕 +30, 충성도 +10, 보너스 지급 이벤트 발행
  - 피로도 -20, 충성도 +10, 휴가 약속 이벤트 발행
  - 의욕 -15, 피로도 +20
- 일상 대화(Normal) 결과 처리 완료
  - 정답 : 충성도 +3 / 오답 : 충성도 -5

**DayTalkTracker.cs**
- 싱글톤 완료
- 하루 최대 대화 횟수 : 2회
- 직원당 하루 1회 제한
- 주간 대화 기록
- 낮 시작 시 일일 기록 초기화
- 새 주 시작 시 전체 기록 초기화
- 이번 주 미대화 직원 목록 반환

**NeglectPenaltyHandler.cs**
- 금요일 밤 방치 패널티 발행 완료
  - 충성도 -5, 피로도 +10 즉시 적용
  - TODO: 낮밤 시스템 완성 후 피로도 +10을 다음 주 적용으로 교체

---

## 외부 연결 필요

- `DialogueEvents.OnStatChangeRequested` 구독 → 의욕/피로도/충성도 반영

- 낮 시작 시 `DayTalkTracker.Instance.ResetDaily()` 호출
- 새 주 시작 시 `DayTalkTracker.Instance.ResetWeekly()` 호출
- 금요일 밤 `NeglectPenaltyHandler.ApplyNeglectPenalty(allEmployeeIds)` 호출

- 직원 클릭 시 `EmployeeStateChecker.GetState(fatigue, motivation)` 으로 상태 판정
- `DialogueManager.Instance.StartDialogue(employeeId, state)` 호출
