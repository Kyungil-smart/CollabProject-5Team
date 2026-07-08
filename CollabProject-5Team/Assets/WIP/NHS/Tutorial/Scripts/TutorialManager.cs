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

    // 싱글톤
    public  static TutorialManager  Instance => _instance;
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

        var filter = _tutorialPanel.GetComponent<PunchHoleFilter>()
                     ?? _tutorialPanel.AddComponent<PunchHoleFilter>();

        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();
        RectTransform panelRect = _tutorialPanel.GetComponent<RectTransform>();
        Vector4 holeVector = Vector4.zero;

        if (targetRect != null)
        {
            // (1) 대상이 UI 요소일 때
            filter.SetTarget(targetRect);

            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);

            Vector3 bl = panelRect.InverseTransformPoint(corners[0]); // Bottom Left
            Vector3 tr = panelRect.InverseTransformPoint(corners[2]); // Top Right

            // 회전이나 스케일 반전을 대비해 안전하게 Min/Max 값 추출
            float minX = Mathf.Min(bl.x, tr.x);
            float maxX = Mathf.Max(bl.x, tr.x);
            float minY = Mathf.Min(bl.y, tr.y);
            float maxY = Mathf.Max(bl.y, tr.y);

            holeVector = new Vector4(minX, minY, maxX, maxY);
        }
        else
        {
            // (2) 대상이 3D 월드 오브젝트일 때
            Vector3 worldPos = _currentActiveObject.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            float screenObjectSize = 100;
            Vector4 pixelRect = new Vector4(
                screenPos.x - screenObjectSize,
                screenPos.y - screenObjectSize,
                screenPos.x + screenObjectSize,
                screenPos.y + screenObjectSize
            );
            filter.SetCustomScreenRect(pixelRect);

            // 셰이더용 로컬 좌표 변환
            Canvas canvas = _tutorialPanel.GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            // 스크린 좌표를 튜토리얼 패널의 로컬 좌표로 변환
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRect,
                new Vector2(screenPos.x, screenPos.y),
                uiCamera,
                out Vector2 localCenter
            );

            // 캔버스 스케일 팩터를 고려한 구멍 크기 조정
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float localSize = screenObjectSize / scaleFactor;

            holeVector = new Vector4(
                localCenter.x - localSize,
                localCenter.y - localSize,
                localCenter.x + localSize,
                localCenter.y + localSize
            );
        }

        // 셰이더 데이터 주입
        var panelImage = _tutorialPanel.GetComponent<Image>();
        if (panelImage != null && _runtimeMaterial != null)
        {
            panelImage.raycastTarget = true;

            // 명시적으로 복제 가공된 런타임 머티리얼에 위치 좌표 주입
            _runtimeMaterial.SetVector("_HoleRect", holeVector);

            panelImage.SetMaterialDirty(); // 강제 UI 그래픽 갱신
        }

        SetPointerPosition();
    }

    private void UpdatePunchHole()
    {
        if (_currentActiveObject == null) return;

        var filter = _tutorialPanel.GetComponent<PunchHoleFilter>();
        if (filter == null) return;

        RectTransform targetRect = _currentActiveObject.GetComponent<RectTransform>();
        RectTransform panelRect = _tutorialPanel.GetComponent<RectTransform>();
        Vector4 holeVector = Vector4.zero;

        if (targetRect != null)
        {
            // UI 타겟: WorldCorners를 이용해 현재 위치 계산
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector3 bl = panelRect.InverseTransformPoint(corners[0]);
            Vector3 tr = panelRect.InverseTransformPoint(corners[2]);

            holeVector = new Vector4(
                Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y),
                Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y)
            );
            filter.SetTarget(targetRect);
        }
        else
        {
            // 3D 타겟: ScreenPoint 이용
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_currentActiveObject.transform.position);

            Canvas canvas = _tutorialPanel.GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, screenPos, uiCamera, out Vector2 localCenter);

            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            float localSize = 100f / scaleFactor; // 기존 screenObjectSize 기준

            holeVector = new Vector4(
                localCenter.x - localSize, localCenter.y - localSize,
                localCenter.x + localSize, localCenter.y + localSize
            );
            filter.SetCustomScreenRect(new Vector4(screenPos.x - 100, screenPos.y - 100, screenPos.x + 100, screenPos.y + 100));
        }

        // 셰이더 전달
        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.SetVector("_HoleRect", holeVector);
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