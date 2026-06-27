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
        HighlightSqure,
        WaitPlayerAction
    }

    // 싱글톤
    public  static TutorialManager  Instance => _instance;
    private static TutorialManager _instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _tutorialText;
    [SerializeField] private GameObject      _tutorialPanel;

    // 튜토리얼 진행
    [SerializeField] private List<TutorialDataSO> _tutorialSteps = new List<TutorialDataSO>();
    private int _curIndex = -1;

    private bool _isWaitingPlayerInput = false;

    // 오브젝트 ID 등록
    private Dictionary<string, GameObject> _registeredObjects = new Dictionary<string, GameObject>();

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
            ProceedTutorial();
        }
    }

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

        // 텍스트만 출력
        if (_tutorialSteps[_curIndex].showMode == ShowMode.TextOnly)
        {
            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;

            TutorialTextOnly();
        }

        // 버튼 눌러야 넘어가짐
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.ButtonActivated)
        {
            _tutorialText.text = _tutorialSteps[_curIndex].tutorialText;

            TutorialButtonActivated();
        }

        // 특정부분 강조 그 부분 터치하면 넘어감
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.HighlightSqure)
        {
        }

        // 플레이어의 행동 기다림 , 특정 행동만 가능하게
        else if (_tutorialSteps[_curIndex].showMode == ShowMode.WaitPlayerAction)
        {
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
            targetCanvas.sortingOrder = 1001;

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

    /////////////////// - HighlightSqure - ///////////////////


}