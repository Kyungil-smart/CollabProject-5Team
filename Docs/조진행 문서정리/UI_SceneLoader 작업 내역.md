# UI_SceneLoader 작업 내역
작성자 : 조진행  
날짜 : 2026.05.27

## 적용 스크립트
- CollabProject-5Team\Assets\Project\Scripts\Util\SceneLoader.cs
- CollabProject-5Team\Assets\Project\Prefabs\SceneLoader.prefab

## 작업 내용
- DontDestroyOnLoad 싱글톤 기반 SceneLoader 작성
- UniTask 비동기 씬 전환 (Single / Additive / Unload)
- 전환 중 중복 호출 방지 (IsLoading 플래그)
- SceneName enum으로 씬 이름 하드코딩 방지

## 현재 상태
- SceneLoader 스크립트 작성 및 프리팹화 완료

## 특이사항
- 타이틀 씬에 SceneLoader 프리팹 배치 필요
- 씬 추가 시 SceneName enum 및 ToSceneString switch에 항목 추가 필요