using R3;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial
{
    public enum TutorialTriggerType
    {
        None,
        GameStarted,
        PanelOpened,
        DialogueNodeReached,
    }

    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        public static readonly Subject<bool> OnTutorialHighlightStateChanged = new();

        [Header("UI References")]
        [SerializeField] private GameObject _dimOverlay;
        [SerializeField] private RectTransform _fingerPointer;

        [Header("Text Only Tutorial")]
        [SerializeField] private GameObject _textPanel;
        [SerializeField] private TextMeshProUGUI _tutorialText;

        [Header("Tutorial Sequence")]
        [SerializeField] private List<TutorialStepSO> _tutorialSteps = new();

        private int _currentStepIndex = 0;
        private HashSet<int> _completedSteps = new();

        private Canvas _tempCanvas;
        private GraphicRaycaster _tempRaycaster;
        private Button _currentButton;

        private const string TUTORIAL_SAVE_KEY = "Tutorial_Completed_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadCompletedSteps();

            _dimOverlay.SetActive(false);
            _fingerPointer.gameObject.SetActive(false);
            if (_textPanel != null) _textPanel.SetActive(false);

            Button overlayButton = _dimOverlay.GetComponent<Button>();
            if (overlayButton == null) overlayButton = _dimOverlay.AddComponent<Button>();
            overlayButton.onClick.AddListener(OnPlayerTapped);
        }

        void Start()
        {
            Invoke(nameof(StartInitialTutorial), 0.3f);
        }

        private void LoadCompletedSteps()
        {
            _completedSteps.Clear();
            for (int i = 0; i < _tutorialSteps.Count; i++)
            {
                if (PlayerPrefs.GetInt(TUTORIAL_SAVE_KEY + i, 0) == 1)
                    _completedSteps.Add(i);
            }
        }

        private void SaveStepCompleted(int stepIndex)
        {
            if (!_completedSteps.Contains(stepIndex))
            {
                _completedSteps.Add(stepIndex);
                PlayerPrefs.SetInt(TUTORIAL_SAVE_KEY + stepIndex, 1);
                PlayerPrefs.Save();
            }
        }

        public void StartInitialTutorial()
        {
            if (_tutorialSteps.Count == 0) return;

            _currentStepIndex = 0;
            while (_currentStepIndex < _tutorialSteps.Count && _completedSteps.Contains(_currentStepIndex))
                _currentStepIndex++;

            if (_currentStepIndex >= _tutorialSteps.Count) return;

            CheckTrigger(TutorialTriggerType.GameStarted, 0);
        }

        public void TriggerTutorial(TutorialTriggerType type, int value = 0)
        {
            if (_currentStepIndex >= _tutorialSteps.Count) return;
            CheckTrigger(type, value);
        }

        private void CheckTrigger(TutorialTriggerType type, int value)
        {
            if (_currentStepIndex >= _tutorialSteps.Count) return;

            TutorialStepSO current = _tutorialSteps[_currentStepIndex];

            if (current.triggerType != type || current.triggerValue != value)
                return;

            if (current.isTextOnly)
                ExecuteTextOnlyTutorial(current);
            else
                ExecuteHighlight(current.targetButton);
        }

        private void ExecuteTextOnlyTutorial(TutorialStepSO step)
        {
            OnTutorialHighlightStateChanged.OnNext(true);

            _textPanel.SetActive(true);
            _tutorialText.text = step.tutorialText;

            Button textButton = _textPanel.GetComponent<Button>();
            if (textButton == null) textButton = _textPanel.AddComponent<Button>();
            textButton.onClick.RemoveAllListeners();
            textButton.onClick.AddListener(OnPlayerTapped);
        }

        private void ExecuteHighlight(Button button)
        {
            _currentButton = button;
            if (_currentButton == null) return;

            OnTutorialHighlightStateChanged.OnNext(true);

            _dimOverlay.SetActive(true);
            _tempCanvas = _currentButton.gameObject.AddComponent<Canvas>();
            _tempCanvas.overrideSorting = true;
            _tempCanvas.sortingOrder = 15;
            _tempRaycaster = _currentButton.gameObject.AddComponent<GraphicRaycaster>();

            _fingerPointer.gameObject.SetActive(true);
            _fingerPointer.position = _currentButton.transform.position + new Vector3(0, 50f, 0);

            _currentButton.onClick.RemoveAllListeners();
            _currentButton.onClick.AddListener(OnPlayerTapped);
        }

        private void OnPlayerTapped()
        {
            SaveStepCompleted(_currentStepIndex);

            ClearTutorialUI();
            _currentStepIndex++;

            while (_currentStepIndex < _tutorialSteps.Count && _completedSteps.Contains(_currentStepIndex))
                _currentStepIndex++;

            if (_currentStepIndex >= _tutorialSteps.Count) return;

            // 연속 Text Only 처리
            TutorialStepSO next = _tutorialSteps[_currentStepIndex];
            if (next.isTextOnly && next.triggerType == _tutorialSteps[_currentStepIndex - 1].triggerType)
            {
                ExecuteTextOnlyTutorial(next);
            }
        }

        private void ClearTutorialUI()
        {
            _dimOverlay.SetActive(false);
            _fingerPointer.gameObject.SetActive(false);
            if (_textPanel != null) _textPanel.SetActive(false);

            if (_currentButton != null)
            {
                Destroy(_tempCanvas);
                Destroy(_tempRaycaster);
                _currentButton.onClick.RemoveAllListeners();
                _currentButton = null;
            }

            OnTutorialHighlightStateChanged.OnNext(false);
        }
    }
}