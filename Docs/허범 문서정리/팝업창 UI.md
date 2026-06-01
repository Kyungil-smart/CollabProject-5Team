## UI 기획 Figma와 조진행님의 UI설계도에 따라 '프로젝트 관리', '회사 관리' 팝업창 UI작업 후 프리팹화

```
Canvas_Popup (Screen Space - Overlay, Sort Order : 10)
│
├── ProjectPopup                    [GO]
│   ├── PopupFrame                  [IMG]     # 팝업 외곽 프레임 (9-Slice)
│   ├── TitleLabel                  [TMP]     # "프로젝트관리"
│   ├── TabBar                      [IMG]     # 탭 배경 프레임 (Horizontal Layout Group)
│   │   ├── NewProjectTabButton     [BTN]
│   │   │   └── Label               [TMP]    # "신규프로젝트"
│   │   └── InProgressTabButton     [BTN]
│   │       └── Label               [TMP]    # "진행프로젝트"
│   │
│   ├── Tab_NewProject              [GO]
│   │   ├── Panel_SlotSelect        [GO]      # 슬롯 선택 (최초 진입 화면)
│   │   │   ├── ContentFrame        [IMG]     # 배경 프레임 (9-Slice)
│   │   │   └── SlotScroll          [SCROLL]  # 기본 3개 표시, 초과 시 세로 스크롤
│   │   │                                     # Content에 ProjectSlotItemView 동적 생성
│   │   ├── Panel_ProjectSetup      [GO]      # 1/2 페이지: 이름·규모 설정
│   │   │   ├── ContentFrame        [IMG]     # 배경 프레임 (9-Slice)
│   │   │   ├── NameGroup           [GO]
│   │   │   │   ├── NameIcon        [IMG]
│   │   │   │   └── ProjectNameInput [INPUT]  # 한/영 최대 6자
│   │   │   ├── ScaleGroup          [GO]
│   │   │   │   ├── ScaleTitleIcon  [IMG]
│   │   │   │   └── ScaleList       [SCROLL]  # 소형/중형/대형
│   │   │   │                                 # Content에 ScaleCardView 동적 생성
│   │   │   ├── PageLabel           [TMP]     # "1 / 2"
│   │   │   ├── BackButton          [BTN]
│   │   │   └── NextButton          [BTN]     # 이름·규모 미설정 시 interactable = false
│   │   │       └── Label           [TMP]
│   │   └── Panel_StaffAssign       [GO]      # 2/2 페이지: 투입인원 설정
│   │       ├── ContentFrame        [IMG]     # 배경 프레임 (9-Slice)
│   │       ├── DeskScroll          [SCROLL]  # 책상 슬롯, Content에 DeskSlotView 동적 생성
│   │       ├── ResetButton         [BTN]     # 배치 전체 초기화
│   │       │   └── Label           [TMP]     # "재배치"
│   │       ├── SortDropdown        [DROPDOWN] # 직군 정렬 필터
│   │       ├── StaffGrid           [SCROLL]  # 최대 10명 표시
│   │       │                                 # Content에 StaffCardView 동적 생성
│   │       │                                 # 교육·배치 중인 직원 interactable = false
│   │       ├── SynergyLabel        [TMP]     # "시너지 OO%" (내용 추후 확정)
│   │       ├── PageLabel           [TMP]     # "2 / 2"
│   │       ├── BackButton          [BTN]
│   │       └── ConfirmButton       [BTN]     # 최소인원(직군별 1명) 미충족 시 interactable = false
│   │           └── Label           [TMP]     # "결정"
│   │
│   └── Tab_InProgress              [GO]
│       ├── ContentFrame            [IMG]     # 배경 프레임 (9-Slice)
│       ├── InProgressList          [SCROLL]  # 진척도 낮은 순 / 같으면 이름순
│       │                                     # Content에 ProjectListItemView 동적 생성
│       ├── EmptyLabel              [TMP]     # "현재 진행중인 프로젝트가 없습니다"
│       └── Panel_ProjectDetail     [GO]      # 목록 아이템 클릭 시 SetActive
│           ├── ContentFrame        [IMG]     # 배경 프레임 (9-Slice)
│           ├── BackButton          [BTN]
│           ├── ProjectNameLabel    [TMP]
│           ├── ProjectInfoGroup    [GO]
│           │   ├── ScaleLabel      [TMP]
│           │   ├── GenreLabel      [TMP]
│           │   └── ThemeLabel      [TMP]
│           ├── ProgressBarGroup    [GO]      # 진척도 (1%, 2.5픽셀)
│           │   ├── ProgressBarBG   [IMG]
│           │   ├── ProgressBarFill [IMG]
│           │   └── ProgressLabel   [TMP]     # 상승 빨강 / 하락 파랑
│           ├── StatGroup           [GO]
│           │   ├── CompletionLabel [TMP]     # 완성도, 상승 빨강 / 하락 파랑
│           │   ├── StabilityLabel  [TMP]
│           │   └── AppealLabel     [TMP]
│           ├── RevenueGraph        [GO]      # 순이익 그래프 (4주 기록)
│           │   └── GraphContent    [IMG]     # 커스텀 Graphic 컴포넌트로 라인 드로우
│           ├── DevStopButton       [BTN]     # 개발중 상태에서만 활성화
│           │   └── Label           [TMP]     # "개발중단"
│           └── ServiceStopButton   [BTN]     # 서비스중 상태에서만 활성화
│               └── Label           [TMP]     # "서비스종료"
│
└── CompanyPopup                    [GO]
    ├── PopupFrame                  [IMG]     # 팝업 외곽 프레임 (9-Slice)
    ├── TitleLabel                  [TMP]     # "회사관리"
    ├── TabBar                      [IMG]     # 탭 배경 프레임 (Horizontal Layout Group)
    │   ├── ExpansionTabButton      [BTN]
    │   │   └── Label               [TMP]    # "회사증축"
    │   └── RankingTabButton        [BTN]
    │       └── Label               [TMP]    # "랭킹"
    ├── Tab_Expansion               [GO]
    │   ├── ContentFrame            [IMG]    # 배경 프레임 (9-Slice)
    │   ├── ExpansionList           [SCROLL]
    │   └── ConfirmButton           [BTN]
    │       └── Label               [TMP]   # "결정"
    └── Tab_Ranking                 [GO]
        ├── ContentFrame            [IMG]   # 배경 프레임 (9-Slice)
        └── RankingList             [SCROLL]
```