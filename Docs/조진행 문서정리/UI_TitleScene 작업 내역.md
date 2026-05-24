# UI_TitleScene 작업 내역
작성자 : 조진행  
날짜 : 2026.05.23

## 적용 스크립트
- WIP\Scripts\TitleSceneUI.cs
- WIP\Scripts\LoadSlotView.cs

## 현재 상태
- 버튼 연결 및 패널 Show/Hide 구현 완료
- 세이브 슬롯 데이터 바인딩 구조 완료

## 미설치 패키지 (주석 처리 상태)
- DoTween : SetActive 직접 전환으로 임시 대체
- R3 : onClick.AddListener 임시 사용
- UniTask : Coroutine 임시 사용

## 외부 연결 필요 (프로그래머)
- TitleSceneUI.OnSlotSelected → 씬 전환
- TitleSceneUI.SetSlotData() → 세이브 데이터