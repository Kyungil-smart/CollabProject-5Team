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

    [System.Serializable]
    public struct TutorialGuide
    {
        [Header("트리거 조건")]
        public TutorialTriggerType triggerType;
        public int triggerValue;

        [Header("강조할 버튼")]
        public Button targetButton;

        [Header("대사만 표시")]
        public bool isTextOnly;

        [TextArea(3, 6)]
        public string tutorialText;
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
        [SerializeField] private List<TutorialGuide> _tutorialGuides = new();

        private int _currentStepIndex = 0;

        private Canvas _tempCanvas;
        private GraphicRaycaster _tempRaycaster;
        private Button _currentButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

        public void StartInitialTutorial()
        {
            if (_tutorialGuides.Count == 0)
            {
                Debug.LogWarning("[Tutorial] 튜토리얼 스텝이 없습니다.");
                return;
            }

            _currentStepIndex = 0;
            Debug.Log($"[Tutorial] 시작! 총 {_tutorialGuides.Count}개");
            CheckTrigger(TutorialTriggerType.GameStarted, 0);
        }

        public void TriggerTutorial(TutorialTriggerType type, int value = 0)
        {
            CheckTrigger(type, value);
        }

        private void CheckTrigger(TutorialTriggerType type, int value)
        {
            if (_currentStepIndex >= _tutorialGuides.Count) return;

            TutorialGuide current = _tutorialGuides[_currentStepIndex];

            if (current.triggerType != type || current.triggerValue != value)
                return;

            if (current.isTextOnly)
                ExecuteTextOnlyTutorial(current);
            else
                ExecuteHighlight(current.targetButton);
        }

        private void ExecuteTextOnlyTutorial(TutorialGuide step)
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
            if (_currentButton == null)
            {
                Debug.LogWarning("[Tutorial] targetButton이 None입니다!");
                OnPlayerTapped();
                return;
            }

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
            ClearTutorialUI();
            _currentStepIndex++;

            if (_currentStepIndex >= _tutorialGuides.Count) return;

            TutorialGuide next = _tutorialGuides[_currentStepIndex];
            if (next.isTextOnly && next.triggerType == _tutorialGuides[_currentStepIndex - 1].triggerType)
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