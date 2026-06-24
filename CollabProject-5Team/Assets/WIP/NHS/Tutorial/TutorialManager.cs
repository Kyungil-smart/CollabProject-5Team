using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using R3;

namespace Tutorial
{
    // 튜토리얼이 감지할 트리거 타입 정의
    public enum TutorialTriggerType
    {
        None,
        DialogueNodeReached, // 특정 대화 노드에 도달했을 때
        PanelOpened,         // 특정 UI 창이 열렸을 때
        GameStarted          // 게임이 시작되었을 때 등...
    }

    [System.Serializable]
    public struct PureTutorialStep
    {
        public TutorialTriggerType triggerType;
        public int                 triggerValue; // DialogueNodeReached 라면 노드 ID, PanelOpened 라면 패널 ID 등
        public Button              targetButton;
    }

    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        // 대화창이나 다른 UI 시스템에 "가이드가 켜지고 꺼짐"을 방송할 R3 채널
        public static readonly Subject<bool> OnTutorialHighlightStateChanged = new();

        [Header("UI References")]
        [SerializeField] private GameObject    _dimOverlay;
        [SerializeField] private RectTransform _fingerPointer;

        [Header("Tutorial Sequence")]
        [SerializeField] private List<PureTutorialStep> _tutorialSteps = new();
        private int _currentStepIndex = 0;

        private Canvas           _tempCanvas;
        private GraphicRaycaster _tempRaycaster;
        private Button           _currentButton;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

                          _dimOverlay.SetActive(false);
            _fingerPointer.gameObject.SetActive(false);
        }

        void Start()
        {
            // 대화창에서 노드 타이핑이 끝났다는 방송 소리가 나면 귀를 기울입니다.
            Dialogue.DialogueEvents.OnNodeTypingCompleted
                .Subscribe(nodeId => CheckTrigger(TutorialTriggerType.DialogueNodeReached, nodeId))
                .AddTo(this);

            Dialogue.DialogueEvents.OnDialogueEnded
                .Subscribe(_ => ClearTutorialUI())
                .AddTo(this);
        }

        private void CheckTrigger(TutorialTriggerType type, int value)
        {
            if (_currentStepIndex >= _tutorialSteps.Count) return;

            PureTutorialStep currentStep = _tutorialSteps[_currentStepIndex];

            // 이번 스텝의 조건 종류나 세부 세팅 값이 다르면 발동하지 않고 무시
            if (currentStep.triggerType != type || currentStep.triggerValue != value) return;

            // 조건이 명확하게 맞아떨어지는 순간 스나이핑 가이드 시작
            ExecuteHighlight(currentStep.targetButton);
        }

        private void ExecuteHighlight(Button button)
        {
            _currentButton = button;
            if (_currentButton == null) return;

            // 가이드 시작했으니 대기 신호 전송
            OnTutorialHighlightStateChanged.OnNext(true);

            _dimOverlay.SetActive(true);
            _tempCanvas = _currentButton.gameObject.AddComponent<Canvas>();
            _tempCanvas.overrideSorting = true;
            _tempCanvas.sortingOrder = 15;
            _tempRaycaster = _currentButton.gameObject.AddComponent<GraphicRaycaster>();

            _fingerPointer.gameObject.SetActive(true);
            _fingerPointer.position = _currentButton.transform.position + new Vector3(0, 50f, 0);

            _currentButton.onClick.RemoveAllListeners();
            _currentButton.onClick.AddListener(() =>
            {
                ClearTutorialUI();
                _currentStepIndex++;

                // 인게임 진행을 재개시킴
                OnTutorialHighlightStateChanged.OnNext(false);
            });
        }

        private void ClearTutorialUI()
        {
            _dimOverlay.SetActive(false);
            _fingerPointer.gameObject.SetActive(false);

            if (_currentButton != null)
            {
                Destroy(_tempCanvas);
                Destroy(_tempRaycaster);
                _currentButton.onClick.RemoveAllListeners();
                _currentButton = null;
            }
        }
    }
}