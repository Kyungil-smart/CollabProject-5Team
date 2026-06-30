using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public enum ShowMode
    {
        TextOnly,
        ButtonActivated,
        HighlightSqureTouchAnywhere,
        HighLightSqureTouchSomewhere,
        PunchHole
    }

    // 싱글톤
    public  static TutorialManager  Instance => _instance;
    private static TutorialManager _instance;

    // UI 요소들
    [Header("UI")]
    [SerializeField] private GameObject      _guideBox;
    [SerializeField] private TextMeshProUGUI _tutorialText;
    [SerializeField] private GameObject      _tutorialPanel;

    [Header("Pointer Settings")]
    [SerializeField] private Image _tutorialPointer; // 인스펙터에서 할당
    [SerializeField] private Vector2 _pointerOffset = new Vector2(30, -30);

    // 튜토리얼 진행
    [SerializeField] private List<TutorialDataSO> _tutorialSteps = new List<TutorialDataSO>();
    private int _curIndex = -1;

    private bool _isWaitingPlayerInput = false;

    // 오브젝트 ID 등록
    private Dictionary<string, GameObject> _registeredObjects = new Dictionary<string, GameObject>();

    // 외부 신호를 받기 위한 Action
    public static System.Action OnSomewhereTutorialCompleted;

    // 현재 진행 중인 단계의 안전한 추적을 위한 상태 변수들
    private GameObject       _currentActiveObject;
    private bool          _isCanvasAddedByManager;
    private bool       _isRaycasterAddedByManager;

    /////////////////// - 라이프사이클 - ///////////////////
    private void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        yield return null;

        StartTutorial();
    }

    private void Update()
    {
        if(_isWaitingPlayerInput && Input.GetMouseButtonDown(0))
        {
            _isWaitingPlayerInput = false;

            if (_tutorialSteps[_curIndex].showMode == ShowMode.HighlightSqureTouchAnywhere)
            {
                CleanUpActiveObjectComponents();
            }

            ProceedTutorial();
        }
    }

    private void OnDestroy()
    {
        OnSomewhereTutorialCompleted -= OnSomewhereConditionMet;
        OnSomewhereTutorialCompleted = null;
    }

    /////////////////// - 실행 - ///////////////////
    private void StartTutorial()
    {
        ProceedTutorial();
    }

    public void RegisterObject(string id, GameObject tutorialObject)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (_registeredObjects.ContainsKey(id))
            _registeredObjects[id] = tutorialObject;

        else
            _registeredObjects.Add(id, tutorialObject);
    }

    private void ExecuteTutorial()
    {
        _tutorialPanel.SetActive(true);

        _tutorialPointer.gameObject.SetActive(false);

        if (string.IsNullOrEmpty(_tutorialSteps[_curIndex].tutorialText))
            _guideBox.SetActive(false);
        else
        {
            _guideBox.SetActive(true);
            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;
        }

        SetupActiveObjectContext();

        switch (_tutorialSteps[_curIndex].showMode)
        {
            case ShowMode.TextOnly:
                TutorialTextOnly();
                break;
            case ShowMode.ButtonActivated:
                TutorialButtonActivated();
                break;
            case ShowMode.HighlightSqureTouchAnywhere:
                TutorialHighlightSqureTouchAnywhere();
                break;
            case ShowMode.HighLightSqureTouchSomewhere:
                TutorialHighLightSqureTouchSomewhere();
                break;
            case ShowMode.PunchHole:
                TutorialPunchHole();
                break;
        }
    }

    private void ProceedTutorial()
    {
        _curIndex++;

        if(_curIndex >= _tutorialSteps.Count)
        {
            _tutorialPanel.SetActive(false);
            return;
        }

        ExecuteTutorial();
    }

    /////////////////// - 필요한 요소 추가, 삭제 - ///////////////////
    private void SetupActiveObjectContext()
    {
              _currentActiveObject = null;
           _isCanvasAddedByManager = false;
        _isRaycasterAddedByManager = false;

        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if (string.IsNullOrEmpty(targetId)) return;

        if (_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            _currentActiveObject = targetObj;

            var showMode = _tutorialSteps[_curIndex].showMode;

            if (showMode == ShowMode.PunchHole) return;

            // 1. 캔버스 설정
            Canvas targetCanvas = _currentActiveObject.GetComponent<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = _currentActiveObject.AddComponent<Canvas>();
                _isCanvasAddedByManager = true;
            }
            else
            {
                _isCanvasAddedByManager = false;
            }

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = 1001;

            // 2. GraphicRaycaster 설정
            if (showMode == ShowMode.ButtonActivated || showMode == ShowMode.HighLightSqureTouchSomewhere)
            {
                if (_currentActiveObject.GetComponent<GraphicRaycaster>() == null)
                {
                    _currentActiveObject.AddComponent<GraphicRaycaster>();
                    _isRaycasterAddedByManager = true;
                }
                else
                {
                    _isRaycasterAddedByManager = false;
                }
            }
        }
    }

    private void CleanUpActiveObjectComponents()
    {
        if (_currentActiveObject == null) return;

        if (_isRaycasterAddedByManager)
        {
            var raycaster = _currentActiveObject.GetComponent<GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);
        }

        Canvas targetCanvas = _currentActiveObject.GetComponent<Canvas>();
        if (targetCanvas != null)
        {
            if (_isCanvasAddedByManager)
            {
                Destroy(targetCanvas);
            }
            else
            {
                targetCanvas.overrideSorting = false;
                targetCanvas.sortingOrder    = 0;
            }
        }

        _currentActiveObject = null;
    }

    /////////////////// - PingerPointer - ///////////////////

    private void SetPointerPosition()
    {
        if (_tutorialPointer == null || _currentActiveObject == null) return;

        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();
        if (targetRect == null) return;

        // 1. Pointer를 TutorialPanel의 자식으로 확실히 넣기
        if (_tutorialPointer.transform.parent != _tutorialPanel.transform)
        {
            _tutorialPointer.transform.SetParent(_tutorialPanel.transform, false);
        }

        _tutorialPointer.transform.SetAsLastSibling();
        _tutorialPointer.gameObject.SetActive(true);

        // 2. UI 전용 정확한 위치 계산 (가장 안정적)
        RectTransform pointerRect = _tutorialPointer.rectTransform;

        // 타겟의 World Corners → Screen Point → Canvas Local Position
        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);

        // 우측 하단 기준 (corners[3])
        Vector3 targetWorldPos = worldCorners[3];

        // Screen Space → Canvas Local Position 변환
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetWorldPos);

        RectTransform canvasRect = _tutorialPanel.GetComponent<RectTransform>() ??
                                   _tutorialPointer.canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        // 최종 위치 = 타겟 우측 하단 + Offset
        pointerRect.anchoredPosition = localPoint + _pointerOffset;
    }

    /////////////////// - TextOnly - ///////////////////
    private void TutorialTextOnly()
    {
        // 터치 입력
        _isWaitingPlayerInput = true;
    }

    /////////////////// - ButtonActivated - ///////////////////
    private void TutorialButtonActivated()
    {
        if (_currentActiveObject == null) return;

        Button button = _currentActiveObject.GetComponent<Button>();
        if(button != null)
        {
            button.onClick.RemoveListener(OnTutorialButtonClicked);
            button.onClick.   AddListener(OnTutorialButtonClicked);
        }

        SetPointerPosition();
    }

    private void OnTutorialButtonClicked()
    {
        if(_currentActiveObject != null)
        {
            Button button = _currentActiveObject.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveListener(OnTutorialButtonClicked);
        }

        CleanUpActiveObjectComponents();

        ProceedTutorial();
    }

    /////////////////// - HighlightSqureTouchAnywhere - ///////////////////
    private void TutorialHighlightSqureTouchAnywhere()
    {
        _isWaitingPlayerInput = true;

        SetPointerPosition();
    }

    /////////////////// - HighLightSqureTouchSomewhere - ///////////////////
    private void TutorialHighLightSqureTouchSomewhere()
    {
        OnSomewhereTutorialCompleted -= OnSomewhereConditionMet;
        OnSomewhereTutorialCompleted += OnSomewhereConditionMet;

        SetPointerPosition();
    }

    private void OnSomewhereConditionMet()
    {
        OnSomewhereTutorialCompleted -= OnSomewhereConditionMet;

        CleanUpActiveObjectComponents();

        ProceedTutorial();
    }

    /////////////////// - TutorialPunchHole - ///////////////////
    private void TutorialPunchHole()
    {
        if (_currentActiveObject == null) return;

        // 1. 패널에 필터 스크립트 추가 (클릭 차단용)
        var filter = _tutorialPanel.GetComponent<PunchHoleFilter>()
                     ?? _tutorialPanel.AddComponent<PunchHoleFilter>();

        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();
        filter.SetHole(targetRect);

        // 2. 튜토리얼 패널 이미지의 머티리얼에 구멍 좌표 전달 (시각적 구멍 뚫기)
        var panelImage = _tutorialPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = true;

            // 타겟 오브젝트의 월드 기준 네 모서리 가져오기
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            // corners[0] : 좌하단, corners[2] : 우상단

            // 쉐이더 내부의 _HoleRect 변수에 (MinX, MinY, MaxX, MaxY) 주입
            Vector4 holeVector = new Vector4(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            panelImage.material.SetVector("_HoleRect", holeVector);
        }

        // 3. 버튼인 경우 다음 단계를 위한 클릭 이벤트 바인딩
        Button button = _currentActiveObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnPunchHoleButtonClicked);
            button.onClick.AddListener(OnPunchHoleButtonClicked);
        }

        SetPointerPosition();
    }

    private void OnPunchHoleButtonClicked()
    {
        if (_currentActiveObject != null)
        {
            Button button = _currentActiveObject.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveListener(OnPunchHoleButtonClicked);
        }

        // 초기화: 다음 단계를 위해 패널의 구멍 영역을 초기화 (안 뚫린 상태로)
        var panelImage = _tutorialPanel.GetComponent<Image>();
        if (panelImage != null && panelImage.material != null)
        {
            panelImage.material.SetVector("_HoleRect", Vector4.zero);
        }

        CleanUpActiveObjectComponents();
        ProceedTutorial();
    }
}