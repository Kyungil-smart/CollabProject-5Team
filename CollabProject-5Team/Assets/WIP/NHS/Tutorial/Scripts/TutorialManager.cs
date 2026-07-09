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
        PunchHole,
    }

    public enum HoleShape
    {
        Square,
        Circle
    }

    // 싱글톤
    public static TutorialManager  Instance => _instance;
    private static TutorialManager _instance;

    // UI 요소들
    [Header("UI")]
    [SerializeField] private GameObject      _guideBox;
    [SerializeField] private TextMeshProUGUI _tutorialText;
    [SerializeField] private GameObject      _tutorialPanel;
    [SerializeField] private Transform       _position1;
    [SerializeField] private Transform       _position2;

    [Header("Pointer Settings")]
    [SerializeField] private Image _tutorialPointer;
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

    // Shader
    [SerializeField] private Material _templateMaterial;
    private Material _runtimeMaterial;

    [Header("BGM")]
    [SerializeField] private AudioClip _bgm;

    [Header("Index 23")]
    [SerializeField] private TMP_InputField _myInputField;

    /////////////////// - 라이프사이클 - ///////////////////
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(_instance.gameObject);
        }
        _instance = this;

        if (_tutorialPanel == null) return;

        Image panelImage = _tutorialPanel.GetComponent<Image>();
        if (panelImage == null) return;

        Material sourceMat = _templateMaterial != null ? _templateMaterial : panelImage.material;

        if (sourceMat != null)
        {
            _runtimeMaterial = new Material(sourceMat);
            panelImage.material = _runtimeMaterial;
        }
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
                bool isNextSame = false;
                if (_curIndex + 1 < _tutorialSteps.Count)
                {
                    string currentId = _tutorialSteps[_curIndex].tutorialObjectId;
                    string nextId = _tutorialSteps[_curIndex + 1].tutorialObjectId;

                    if (!string.IsNullOrEmpty(currentId) && currentId == nextId)
                    {
                        isNextSame = true;
                    }
                }

                if (!isNextSame)
                {
                    CleanUpActiveObjectComponents();
                }
            }

            ProceedTutorial();
        }
    }

    private void LateUpdate()
    {
        if (_tutorialSteps != null && _curIndex  >= 0 && _curIndex < _tutorialSteps.Count &&
            _tutorialSteps[_curIndex].showMode == ShowMode.PunchHole)
        {
            UpdatePunchHole();
            SetPointerPosition();
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
        AudioManager.Instance?.PlayBGM(_bgm);
        ProceedTutorial();
    }

    public void RegisterObject(string id, GameObject tutorialObject)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (_registeredObjects.ContainsKey(id))
        {
            _registeredObjects[id] = tutorialObject;
        }
        else
        {
            _registeredObjects.Add(id, tutorialObject);
        }
    }

    private void ExecuteTutorial()
    {
        if (_tutorialSteps == null || _curIndex < 0 || _curIndex >= _tutorialSteps.Count) return;

        if (_tutorialSteps[_curIndex] == null) return;

        _tutorialPanel.             SetActive(true);
        _tutorialPointer.gameObject.SetActive(false);

        if (string.IsNullOrWhiteSpace(_tutorialSteps[_curIndex].tutorialText))
            _guideBox.SetActive(false);
        else
        {
            _guideBox.SetActive(true);

            if (_tutorialSteps[_curIndex].textPosition)
                _guideBox.transform.position = _position2.position;
            else
                _guideBox.transform.position = _position1.position;

            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;
        }

        SetupActiveObjectContext();

        if (_currentActiveObject != null && _myInputField != null)
        {
            if (_currentActiveObject == _myInputField.gameObject || _currentActiveObject.GetComponentInChildren<TMP_InputField>() != null)
            {
                _myInputField.text = "임시 프로젝트";

                _myInputField.ForceLabelUpdate();
            }
        }

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

            if (SceneFlowManager.Instance != null)
                SceneFlowManager.Instance.CompleteCurrentFlow();

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

            RectTransform checkRect = _currentActiveObject.GetComponent<RectTransform>();
            if(checkRect != null)
            {
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
                if (showMode == ShowMode.ButtonActivated ||
                    showMode == ShowMode.HighLightSqureTouchSomewhere ||
                    showMode == ShowMode.PunchHole)
                {
                    if (_currentActiveObject.GetComponent<GraphicRaycaster>() == null)
                    {
                        _currentActiveObject.AddComponent<GraphicRaycaster>();
                        _isRaycasterAddedByManager = true;
                    }
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

        var tutorialComp = _currentActiveObject.GetComponent<TutorialObject>();
        Vector3 worldPos = (tutorialComp != null) ? tutorialComp.GetWorldPosition() : _currentActiveObject.transform.position;

        _tutorialPointer.transform.SetAsLastSibling();
        _tutorialPointer.gameObject.SetActive(true);

        RectTransform pointerRect = _tutorialPointer.rectTransform;
        Vector2 screenPoint = Vector2.zero;

        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            Vector3[] worldCorners = new Vector3[4];
            targetRect.GetWorldCorners(worldCorners);
            Vector3 targetWorldPos = worldCorners[3];
            screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetWorldPos);
        }
        else
        {
            worldPos = _currentActiveObject.transform.position;
            screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        }

        RectTransform canvasRect = _tutorialPanel.GetComponent<RectTransform>() ??
                                   _tutorialPointer.canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

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

        bool isCircle = (_tutorialSteps[_curIndex].holeShape == HoleShape.Circle);
        _runtimeMaterial.SetFloat("_HoleShape", isCircle ? 1.0f : 0.0f);

        var filter = _tutorialPanel.GetComponent<PunchHoleFilter>() ?? _tutorialPanel.AddComponent<PunchHoleFilter>();

        Vector4 holeVector = CalculateHoleVector(out bool isTargetUI);

        if (isTargetUI) filter.SetTarget(_currentActiveObject.GetComponent<RectTransform>(), _tutorialSteps[_curIndex].holeShape);
        else filter.SetCustomScreenRect(new Vector4(holeVector.x, holeVector.y, holeVector.z, holeVector.w)); // 실제 로직에 맞게 조정 필요

        _runtimeMaterial.SetVector("_HoleRect", holeVector);
        _tutorialPanel.GetComponent<Image>().raycastTarget = true;
        SetPointerPosition();
    }

    private Vector4 CalculateHoleVector(out bool isTargetUI)
    {
        RectTransform panelRect = _tutorialPanel.GetComponent<RectTransform>();
        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();
        isTargetUI = (targetRect != null);

        if (isTargetUI)
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector3 bl = panelRect.InverseTransformPoint(corners[0]);
            Vector3 tr = panelRect.InverseTransformPoint(corners[2]);

            return new Vector4(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y),
                               Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
        }
        else
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentActiveObject.transform.position);
            Canvas canvas = _tutorialPanel.GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPos, uiCamera, out Vector2 localCenter);

            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float localSize = 100f / scaleFactor;

            return new Vector4(localCenter.x - localSize, localCenter.y - localSize,
                               localCenter.x + localSize, localCenter.y + localSize);
        }
    }

    private void UpdatePunchHole()
    {
        if (_currentActiveObject == null) return;

        // 1. 좌표 계산
        Vector4 holeVector = CalculateHoleVector(out bool isTargetUI);

        // 2. 셰이더 적용
        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.SetVector("_HoleRect", holeVector);
        }

        // 3. 필터 업데이트
        var filter = _tutorialPanel.GetComponent<PunchHoleFilter>();
        if (filter == null) return;

        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();

        if (isTargetUI)
        {
            filter.SetTarget(targetRect, _tutorialSteps[_curIndex].holeShape);
        }
        else
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentActiveObject.transform.position);
            filter.SetCustomScreenRect(new Vector4(screenPos.x - 100, screenPos.y - 100, screenPos.x + 100, screenPos.y + 100));
        }
    }

    // 구멍 안의 버튼이 클릭되었을 때 실행될 콜백
    private void CleanUpPunchHole()
    {
        var filter = _tutorialPanel.GetComponent<PunchHoleFilter>();
        if (filter != null) filter.ClearTarget();

        var panelImage = _tutorialPanel.GetComponent<Image>();
        if (panelImage != null && panelImage.material != null)
        {
            panelImage.material.SetVector("_HoleRect", Vector4.zero);
        }
    }

    public void CompletePunchHoleStep()
    {
        if (_curIndex >= 0 && _curIndex < _tutorialSteps.Count &&
            _tutorialSteps[_curIndex].showMode == ShowMode.PunchHole)
        {
            CleanUpPunchHole();
            CleanUpActiveObjectComponents();
            ProceedTutorial();
        }
    }

    /////////////////////////////

    public bool IsWaitingDialogue = false;

    public void StartWaitingForDialogue()
    {
        IsWaitingDialogue = true;
        _tutorialPanel.SetActive(false); 
    }

    public void FinishDialogueAndProceed()
    {
        if (IsWaitingDialogue)
        {
            IsWaitingDialogue = false;
            _tutorialPanel.SetActive(true);

            if (_tutorialSteps[_curIndex].showMode == ShowMode.PunchHole)
                CleanUpPunchHole();

            CleanUpActiveObjectComponents();

            ProceedTutorial();
        }
    }
}