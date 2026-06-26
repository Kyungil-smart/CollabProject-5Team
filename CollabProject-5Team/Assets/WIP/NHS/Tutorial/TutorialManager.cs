using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum TutorialType
    {
        TextOnly,       
        ButtonTrigger,  
        DialogueWait    
    }

    [System.Serializable]
    public struct TutorialStep
    {
        public TutorialType type;
        public string targetButtonId;
        public string targetActionName;
        [TextArea(4, 5)]
        public string tutorialText;
    }

    [Header("UI 연결 요소")]
    [SerializeField] private GameObject _dimOverlay;
    [SerializeField] private TextMeshProUGUI _tutorialTextUI;
    [SerializeField] private GameObject _tutorialWindowObj;

    [Header("튜토리얼 시퀀스")]
    [SerializeField] private List<TutorialStep> _steps = new();
    private int _currentStepIndex = 0;

    private bool _isTutorialStarted = false;
    private Dictionary<string, GameObject> _activeDynamicButtons = new();

    public static Action<string> OnTutorialActionCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_dimOverlay != null) _dimOverlay.SetActive(false);
        if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(false);

        OnTutorialActionCompleted += CheckInGameAction;
    }

    private void Start()
    {
        StartFirstTutorial();
    }

    private void OnDestroy()
    {
        OnTutorialActionCompleted -= CheckInGameAction;
    }

    public void StartFirstTutorial()
    {
        if (_isTutorialStarted || _steps.Count == 0) return;
        _isTutorialStarted = true;
        _currentStepIndex = 0;
        ExecuteStep(_steps[0]);
    }

    public void RegisterDynamicButton(string buttonId, GameObject buttonObj)
    {
        if (string.IsNullOrEmpty(buttonId) || buttonObj == null) return;

        _activeDynamicButtons[buttonId] = buttonObj;

        if (!_isTutorialStarted || _currentStepIndex >= _steps.Count) return;
        TutorialStep currentStep = _steps[_currentStepIndex];

        if (currentStep.type == TutorialType.ButtonTrigger && currentStep.targetButtonId == buttonId)
        {
            LockAndHighlightButton(buttonObj, OnTargetButtonClicked);
        }
    }

    private void CheckInGameAction(string actionName)
    {
        if (!_isTutorialStarted || _currentStepIndex >= _steps.Count) return;

        TutorialStep currentStep = _steps[_currentStepIndex];
        if (!string.IsNullOrEmpty(currentStep.targetActionName) && currentStep.targetActionName == actionName)
        {
            AdvanceStep();
        }
    }

    private void ExecuteStep(TutorialStep step)
    {
        if (step.type != TutorialType.DialogueWait)
        {
            if (_dimOverlay != null) _dimOverlay.SetActive(true);
            if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(true);
            if (_tutorialTextUI != null) _tutorialTextUI.text = step.tutorialText;
        }

        if (step.type == TutorialType.TextOnly)
        {
            SetupDimAsNextButton();
        }
        else if (step.type == TutorialType.ButtonTrigger)
        {
            RemoveDimButtonListener();

            if (_activeDynamicButtons.TryGetValue(step.targetButtonId, out GameObject btnObj))
            {
                LockAndHighlightButton(btnObj, OnTargetButtonClicked);
            }
        }
        else if (step.type == TutorialType.DialogueWait)
        {
            if (_dimOverlay != null) _dimOverlay.SetActive(false);
            if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(false);

            Debug.Log("[Tutorial] 중간 NPC 대화 단계 진입. 대화 종료 신호를 대기합니다.");
        }
    }

    private void LockAndHighlightButton(GameObject buttonObj, UnityEngine.Events.UnityAction action)
    {
        if (buttonObj == null) return;

        Canvas targetCanvas = buttonObj.GetComponent<Canvas>();
        if (targetCanvas == null) targetCanvas = buttonObj.AddComponent<Canvas>();

        if (targetCanvas != null)
        {
            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = 999;
        }

        if (buttonObj.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            buttonObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        UnityEngine.UI.Button btn = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }
    }

    private void OnTargetButtonClicked() => AdvanceStep();

    private void ResetButtonComponent(GameObject targetObj)
    {
        if (targetObj == null) return;
        var raycaster = targetObj.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;

        var canvas = targetObj.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }

    public void AdvanceStep()
    {
        if (_currentStepIndex < _steps.Count && _steps[_currentStepIndex].type == TutorialType.ButtonTrigger)
        {
            string targetId = _steps[_currentStepIndex].targetButtonId;
            if (_activeDynamicButtons.TryGetValue(targetId, out GameObject btnObj) && btnObj != null)
            {
                ResetButtonComponent(btnObj);
                var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.onClick.RemoveListener(OnTargetButtonClicked);
            }
        }

        _currentStepIndex++;

        if (_currentStepIndex >= _steps.Count)
        {
            Debug.Log("[Tutorial] 모든 튜토리얼 시퀀스 최종 종료.");
            _isTutorialStarted = false;
            if (_dimOverlay != null) _dimOverlay.SetActive(false);
            if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(false);
            return;
        }

        ExecuteStep(_steps[_currentStepIndex]);
    }

    #region Dim Logic
    private void SetupDimAsNextButton()
    {
        if (_dimOverlay == null) return;
        UnityEngine.UI.Button dimBtn = _dimOverlay.GetComponent<UnityEngine.UI.Button>() ?? _dimOverlay.AddComponent<UnityEngine.UI.Button>();
        dimBtn.onClick.RemoveListener(OnTextOnlyClicked);
        dimBtn.onClick.AddListener(OnTextOnlyClicked);
    }

    private void RemoveDimButtonListener()
    {
        if (_dimOverlay == null) return;
        UnityEngine.UI.Button dimBtn = _dimOverlay.GetComponent<UnityEngine.UI.Button>();
        if (dimBtn != null) dimBtn.onClick.RemoveListener(OnTextOnlyClicked);
    }

    private void OnTextOnlyClicked() => AdvanceStep();
    #endregion
}