using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    // 튜토리얼 진행
    [SerializeField] private List<TutorialDataSO> _tutorialSteps = new List<TutorialDataSO>();
    private int _curIndex = -1;

    private bool _isWaitingPlayerInput = false;

    // 오브젝트 ID 등록
    private Dictionary<string, GameObject> _registeredObjects = new Dictionary<string, GameObject>();

    // 외부 신호를 받기 위한 Action
    public static System.Action OnSomewhereTutorialCompleted;

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
                CleanUpHighlightSqure();
            }

            ProceedTutorial();
        }
    }

    private void OnDestroy()
    {
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

        if (string.IsNullOrEmpty(_tutorialSteps[_curIndex].tutorialText))
            _guideBox.SetActive(false);
        else
        {
            _guideBox.SetActive(true);
            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;
        }

        // 텍스트만 출력
        if (_tutorialSteps[_curIndex].showMode == ShowMode.TextOnly)
        {
            TutorialTextOnly();
        }

        // 버튼 눌러야 넘어가짐
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.ButtonActivated)
        {
            TutorialButtonActivated();
        }

        // 특정부분 강조 아무데나 터치해도 넘어감
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.HighlightSqureTouchAnywhere)
        {
            TutorialHighlightSqureTouchAnywhere();
        }

        // 특정 부분 강조 그 부분 터치해야 넘어감
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.HighLightSqureTouchSomewhere)
        {
            TutorialHighLightSqureTouchSomewhere();
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

    /////////////////// - TextOnly - ///////////////////

    private void TutorialTextOnly()
    {
        // 터치 입력
        _isWaitingPlayerInput = true;
    }

    /////////////////// - ButtonActivated - ///////////////////

    private void TutorialButtonActivated()
    {
        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if (_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            Canvas targetCanvas = targetObj.GetComponent<Canvas>();
            if (targetCanvas == null) targetCanvas = targetObj.AddComponent<Canvas>();

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder    = 1001;

            if (targetObj.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                targetObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            UnityEngine.UI.Button button = targetObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(OnTutorialButtonClicked);
                button.onClick.   AddListener(OnTutorialButtonClicked);
            }
        }
        else
            return;
    }

    private void OnTutorialButtonClicked()
    {
        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if(_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            var raycaster = targetObj.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);

            Canvas targetCanvas = targetObj.GetComponent<Canvas>();
            if (targetCanvas != null) Destroy(targetCanvas);

            UnityEngine.UI.Button btn = targetObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) 
                btn.onClick.RemoveListener(OnTutorialButtonClicked);
        }

        ProceedTutorial();
    }

    /////////////////// - HighlightSqureTouchAnywhere - ///////////////////

    private void TutorialHighlightSqureTouchAnywhere()
    {
        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if(_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            Canvas targetCanvas =     targetObj.GetComponent<Canvas>();
            if (targetCanvas == null) targetObj.AddComponent<Canvas>();

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder    = 1001;
        }

        _isWaitingPlayerInput = true;
    }

    private void CleanUpHighlightSqure()
    {
        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if(_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            Canvas targetCanvas = targetObj.GetComponent<Canvas>();
            if (targetCanvas != null) Destroy(targetCanvas);
        }
    }

    /////////////////// - HighLightSqureTouchSomewhere - ///////////////////
    private async void TutorialHighLightSqureTouchSomewhere()
    {
        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if (_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            Canvas targetCanvas = targetObj.GetComponent<Canvas>();
            if (targetCanvas == null) targetCanvas = targetObj.AddComponent<Canvas>();

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder    = 1001;

            if (targetObj.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                targetObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        OnSomewhereTutorialCompleted -= OnSomewhereConditionMet;
        OnSomewhereTutorialCompleted += OnSomewhereConditionMet;
    }

    private void OnSomewhereConditionMet()
    {
        OnSomewhereTutorialCompleted -= OnSomewhereConditionMet;

        CleanUpHighlightSqureSomewhere();

        ProceedTutorial();
    }

    private void CleanUpHighlightSqureSomewhere()
    {
        string targetId = _tutorialSteps[_curIndex].tutorialObjectId;

        if (_registeredObjects.TryGetValue(targetId, out GameObject targetObj))
        {
            var raycaster = targetObj.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);

            Canvas targetCanvas = targetObj.GetComponent<Canvas>();
            if (targetCanvas != null) Destroy(targetCanvas);
        }
    }
}