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

        [Header("강조할 UI 오브젝트 (패널/창 전체)")]
        public GameObject targetObject;

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

        // 원래 상태 복구를 위한 변수들
        private GameObject _currentTargetObject;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Button _targetButtonInObject;

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
            if (overlayButton != null)
            {
                overlayButton.onClick.RemoveAllListeners();
            }
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
                ExecuteHighlight(current.targetObject); // 💡 targetObject 전달
        }

        private void ExecuteTextOnlyTutorial(TutorialGuide step)
        {
            OnTutorialHighlightStateChanged.OnNext(true);

            _dimOverlay.SetActive(true);
            _textPanel.SetActive(true);
            _tutorialText.text = step.tutorialText;

            Button textButton = _textPanel.GetComponent<Button>();
            if (textButton == null) textButton = _textPanel.AddComponent<Button>();
            textButton.onClick.RemoveAllListeners();
            textButton.onClick.AddListener(OnPlayerTapped);

            Button overlayButton = _dimOverlay.GetComponent<Button>();
            if (overlayButton == null) overlayButton = _dimOverlay.AddComponent<Button>();
            overlayButton.onClick.RemoveAllListeners();
            overlayButton.onClick.AddListener(OnPlayerTapped);
        }

        private void ExecuteHighlight(GameObject targetObj)
        {
            _currentTargetObject = targetObj;
            if (_currentTargetObject == null)
            {
                Debug.LogWarning("[Tutorial] targetObject가 없어서 다음으로 강제 진행합니다.");
                OnPlayerTapped();
                return;
            }

            OnTutorialHighlightStateChanged.OnNext(true);

            // 1. 딤 패널 활성화 (다른 곳 터치 완벽 차단)
            _dimOverlay.SetActive(true);
            Button overlayButton = _dimOverlay.GetComponent<Button>();
            if (overlayButton != null) overlayButton.onClick.RemoveAllListeners();

            // 2. 지정된 UI 오브젝트(창 전체)를 딤 패널 앞으로 탈출시키기
            _originalParent = _currentTargetObject.transform.parent;
            _originalSiblingIndex = _currentTargetObject.transform.GetSiblingIndex();

            _currentTargetObject.transform.SetParent(_dimOverlay.transform.parent, true);
            _currentTargetObject.transform.SetAsLastSibling();

            // 3. 오브젝트 내부(자식들 포함)에서 실제로 클릭되어야 하는 'Button' 컴포넌트를 탐색
            _targetButtonInObject = _currentTargetObject.GetComponentInChildren<Button>();

            if (_targetButtonInObject != null)
            {
                // 4. 오직 이 내부 버튼을 눌렀을 때만 튜토리얼이 다음으로 넘어가도록 리스너 추가
                _targetButtonInObject.onClick.AddListener(OnPlayerTapped);

                // 5. 손가락 포인터는 해당 버튼 위치 위에 띄워줌
                _fingerPointer.gameObject.SetActive(true);
                _fingerPointer.transform.SetAsLastSibling();
                _fingerPointer.position = _targetButtonInObject.transform.position + new Vector3(50, 50f, 0);

                Debug.Log($"[Tutorial] '{_currentTargetObject.name}' 패널을 위로 올림. 내부 버튼 '{_targetButtonInObject.name}' 클릭 시 다음으로 진행.");
            }
            else
            {
                // 패널 내부에 버튼이 하나도 없다면, 패널 자체를 눌러서 넘어가도록 백업 처리
                Debug.LogWarning($"[Tutorial] {_currentTargetObject.name} 내부에 Button 컴포넌트가 없습니다. 패널 터치 시 넘어가도록 대체합니다.");
                Button panelButton = _currentTargetObject.GetComponent<Button>();
                if (panelButton == null) panelButton = _currentTargetObject.AddComponent<Button>();
                _targetButtonInObject = panelButton;
                _targetButtonInObject.onClick.AddListener(OnPlayerTapped);
            }
        }

        private void OnPlayerTapped()
        {
            Debug.Log($"[Tutorial] 올바른 타겟 버튼 탭 감지! 다음 인덱스로: {_currentStepIndex + 1}");

            ClearTutorialUI();
            _currentStepIndex++;

            if (_currentStepIndex >= _tutorialGuides.Count)
            {
                Debug.Log("[Tutorial] 모든 튜토리얼 완료.");
                return;
            }

            TutorialGuide next = _tutorialGuides[_currentStepIndex];
            TutorialGuide prev = _tutorialGuides[_currentStepIndex - 1];

            if (next.triggerType == prev.triggerType && next.triggerValue == prev.triggerValue)
            {
                CheckTrigger(next.triggerType, next.triggerValue);
            }
        }

        private void ClearTutorialUI()
        {
            _dimOverlay.SetActive(false);
            _fingerPointer.gameObject.SetActive(false);
            if (_textPanel != null) _textPanel.SetActive(false);

            Button overlayButton = _dimOverlay.GetComponent<Button>();
            if (overlayButton != null) overlayButton.onClick.RemoveAllListeners();

            if (_currentTargetObject != null)
            {
                // 추가했던 튜토리얼 리스너 안전하게 제거
                if (_targetButtonInObject != null)
                {
                    _targetButtonInObject.onClick.RemoveListener(OnPlayerTapped);
                    _targetButtonInObject = null;
                }

                // UI 오브젝트(창 전체)를 원래 살던 부모 위치와 순서로 완벽 복구
                _currentTargetObject.transform.SetParent(_originalParent, true);
                _currentTargetObject.transform.SetSiblingIndex(_originalSiblingIndex);

                _currentTargetObject = null;
                _originalParent = null;
            }

            OnTutorialHighlightStateChanged.OnNext(false);
        }
    }
}