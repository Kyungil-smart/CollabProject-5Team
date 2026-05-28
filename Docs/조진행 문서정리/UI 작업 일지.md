# UI 작업 일지
작성자 : 조진행  
날짜 : 2026.05.28

---

## 완료 작업

### 와이어프레임 작업
- Canvas_HUD 전체
  - TopBar (TimeGroup, MoneyGroup, ReputationGroup, SettingsButton)
  - DayUI (WorkStartButton, DayQuitButton)
  - NightUI BottomBar (HRButton, ProjectButton, CompanyButton, SaveButton, NightQuitButton)

- Canvas_Popup HRPopup
  - TabBar (직원관리 / 직원고용 / 직원해고 / 직원교육)
  - Tab_EmployeeManage
    - Panel_List (HeaderGroup, SortDropdown, EmployeeGrid)
    - Panel_Detail (EmployeeImage, InfoGroup, StatBars, ProjectHistory, EducationButton, FireButton)
  - Tab_Hire
    - Panel_HireMain (HireImage, RecruitButton, ApplicantButton)
    - Panel_Recruit (BackButton, JobSliders, CostGroup, ConfirmButton)

---

## 특이사항

- Slider Fill Image Type을 `Tiled`로 설정해야 끝부분이 깔끔하게 처리됨
- Slider Handle은 `Handle Rect`를 None으로 설정해 제거
- 직원 모집 슬라이더 눈금/숫자 표시는 리소스 확정 후 작업 예정
- 프로토타입 이미지가 와이어프레임이 아닌 리소스 포함 버전이라 구조 파악에 시간 소요
- VRAM 부족으로 피그마/유니티 동시 작업 불가 → 내일부터 태블릿에 피그마 띄워놓고 작업 예정

---

## 미완료 (내일 이어서)
`설계도는 언제든지 변경될 수 있음`

- Tab_Hire Panel_ApplicantList
- Tab_Fire
- Tab_Education
- ProjectPopup
- CompanyPopup
- Canvas_Report
- Canvas_Alert
- Canvas_Loading

---

## 보류

- 직원 카드 프리팹 (EmployeeCardView)
- 프로젝트 이력 태그 프리팹 (ProjectHistoryTag)
- 특성 태그 프리팹 (TraitTagView) - 기획 확정 후
- Safe Area 적용 - APK 빌드 후 실기기 테스트 시점
- DoTween 연출 - 리소스 확정 후
- 탭 Sprite Swap - 리소스 확정 후