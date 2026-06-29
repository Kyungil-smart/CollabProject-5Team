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
        HighLightSqureTouchSomewhere
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

            // Canvas 체크
            Canvas targetCanvas = _currentActiveObject.GetComponent<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = _currentActiveObject.AddComponent<Canvas>();
                _isCanvasAddedByManager = true;
            }

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder    = 1001;

            var showMode = _tutorialSteps[_curIndex].showMode;
            if (showMode == ShowMode.ButtonActivated || showMode == ShowMode.HighLightSqureTouchSomewhere)
            {
                if (_currentActiveObject.GetComponent<GraphicRaycaster>() == null)
                {
                    _currentActiveObject.AddComponent<GraphicRaycaster>();
                    _isRaycasterAddedByManager = true;
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
}