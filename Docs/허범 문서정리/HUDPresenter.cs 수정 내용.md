1. `[SerializeField] private DeskInteract _desk;` 변수 추가
   - `OnWorkStartClicked()`함수 내부 조건 변경 (기존 내용은 주석처리)
  ```csharp
  // 기존 업무 완료가 되던 로직을 이동 로직으로 수정
  if (_desk != null)
  {
      _desk.OnClickWorkButton();
  }
  ```

2. `SwitchToNight()`함수 내부 추가내용
```csharp
CloseAllBottomPopups();
_settingsPresenter.Hide();
```

# 하이어라키 Canvas_Hud의 inspector창에서 Hud Presenter(script)쪽 Desk에 Hotel_PC_Monitor1 프리팹 참조해야함 