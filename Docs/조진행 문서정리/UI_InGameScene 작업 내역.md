# UI_InGameScene 작업 내역
작성자 : 조진행  
날짜 : 2026.05.26

## 적용 스크립트
- WIP\Scripts\InGameUI.cs

## 현재 상태
- 낮/밤 UI 전환 구현 완료
- 보고서 패널 Show/Hide 구현 완료
- 버튼 이벤트 발행 구조 완료
- 날짜 표시 외부 API 구조 완료

## 미설치 패키지 (주석 처리 상태)
- DoTween : SetActive 직접 전환으로 임시 대체 (낮/밤 전환 및 팝업 연출)

## 외부 연결 필요 (프로그래머)
- InGameUI.OnWorkStartClicked → 업무 시작 처리
- InGameUI.OnDayQuitClicked → 다음날 or 금요일 밤 전환 처리
- InGameUI.OnNightQuitClicked → 다음주 월요일 전환 처리
- InGameUI.OnReportNextClicked → 보고서 완료 처리
- InGameUI.SetTime() → 날짜 데이터 전달
- InGameUI.SetDayQuitButtonActive() → 낮 퇴근 버튼 활성화 타이밍 전달
- InGameUI.SetNightQuitButtonActive() → 밤 퇴근 버튼 활성화 타이밍 전달
- InGameUI.SetReportText() → 보고서 텍스트 데이터 전달

## 미결 사항
- SceneLoader 구조 미확정 (싱글톤 / VContainer / 어댑터 방식 팀 합의 필요)
- TitleScene ↔ InGameScene 씬 전환 연결 미완료
- DayHUD.cs / NightHUD.cs / DayNightTransition.cs 는 InGameUI.cs 로 통합, 미사용