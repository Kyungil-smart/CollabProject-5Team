using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Cysharp.Threading.Tasks;
using System; // 💡 Action 사용을 위해 추가

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum TutorialType
    {
        TextOnly,
        ButtonTrigger,
        MultiButtonTrigger,
        ActionTrigger // 💡 [통합] 인게임 특정 행동(예: 퀘스트 완료)을 감지하는 타입 추가
    }

    [System.Serializable]
    public struct TutorialStep
    {
        public TutorialType type;
        public string targetButtonId;   // 단일 버튼용 ID
        public List<string> targetButtonIds;  // 다중 버튼용 고유 ID 리스트
        public string targetActionName; // 💡 [통합] 감지할 인게임 행동 이름 (예: "DailyQuestComplete")
        [TextArea(2, 5)]
        public string tutorialText;
    }

    // 💡 [통합 핵심] QuestManager 등 외부에서 "행동 완료했다!"라고 신호를 보낼 수 있는 전역 이벤트
    public static Action<string> OnTutorialActionCompleted;

    [Header("UI 연결 요소")]
    [SerializeField] private GameObject _dimOverlay;
    [SerializeField] private RectTransform _fingerPointer;
    [SerializeField] private TextMeshProUGUI _tutorialTextUI;
    [SerializeField] private GameObject _tutorialWindowObj;

    [Header("튜토리얼 시퀀스")]
    [SerializeField] private List<TutorialStep> _steps = new();
    private int _currentStepIndex = 0;

    // 런타임에 생성/활성화된 동적 버튼들을 담아두는 저장소
    private Dictionary<string, GameObject> _activeDynamicButtons = new();

    // 현재 멀티 미션에서 아직 안 눌린 버튼 오브젝트들을 추적하는 셋
    private HashSet<GameObject> _remainingButtons = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_dimOverlay != null) _dimOverlay.SetActive(false);
        if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(false);
        if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(false);

        // 💡 [통합] 행동 감지 리스너 구독 연결
        OnTutorialActionCompleted += CheckInGameAction;
    }

    private void OnDestroy()
    {
        // 💡 메모리 누수 방지 리스너 해제
        OnTutorialActionCompleted -= CheckInGameAction;
    }

    private void Start()
    {
        Invoke(nameof(StartFirstTutorial), 0.3f);
    }

    private void StartFirstTutorial()
    {
        if (_steps.Count == 0) return;
        _currentStepIndex = 0;
        ExecuteStep(_steps[0]);
    }

    // 📢 외부(동적 버튼)에서 "나 생성되었어!"라고 등록할 때 호출하는 함수
    public void RegisterDynamicButton(string buttonId, GameObject buttonObj)
    {
        if (string.IsNullOrEmpty(buttonId) || buttonObj == null) return;

        _activeDynamicButtons[buttonId] = buttonObj;
        Debug.Log($"[Tutorial] 동적 버튼 등록됨: {buttonId}");

        if (_currentStepIndex >= _steps.Count) return;
        TutorialStep currentStep = _steps[_currentStepIndex];

        if (currentStep.type == TutorialType.MultiButtonTrigger && currentStep.targetButtonIds.Contains(buttonId))
        {
            if (!_remainingButtons.Contains(buttonObj))
            {
                _remainingButtons.Add(buttonObj);
                LockAndHighlightButton(buttonObj, () => OnMultiButtonClicked(buttonObj));
            }
        }
        else if (currentStep.type == TutorialType.ButtonTrigger && currentStep.targetButtonId == buttonId)
        {
            LockAndHighlightButton(buttonObj, OnTargetButtonClicked);
        }
    }

    // 💡 [통합] QuestManager 등에서 신호를 보냈을 때 감시 및 전진하는 함수
    private void CheckInGameAction(string actionName)
    {
        if (_currentStepIndex >= _steps.Count) return;

        TutorialStep currentStep = _steps[_currentStepIndex];

        // 현재 스텝이 ActionTrigger이고, 퀘스트 매니저가 보낸 행동 이름이 인스펙터에 적힌 이름과 같다면 통과!
        if (currentStep.type == TutorialType.ActionTrigger && currentStep.targetActionName == actionName)
        {
            Debug.Log($"[Tutorial] 목표 인게임 행동 감지 완료: {actionName} -> 다음 단계로.");
            AdvanceStep();
        }
    }

    private void ExecuteStep(TutorialStep step)
    {
        if (_dimOverlay != null) _dimOverlay.SetActive(true);
        if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(true);
        if (_tutorialTextUI != null) _tutorialTextUI.text = step.tutorialText;

        if (step.type == TutorialType.TextOnly)
        {
            if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(false);
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
        else if (step.type == TutorialType.MultiButtonTrigger)
        {
            RemoveDimButtonListener();
            if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(false);

            _remainingButtons.Clear();

            if (step.targetButtonIds != null)
            {
                foreach (var id in step.targetButtonIds)
                {
                    if (_activeDynamicButtons.TryGetValue(id, out GameObject btnObj) && btnObj != null)
                    {
                        _remainingButtons.Add(btnObj);
                        LockAndHighlightButton(btnObj, () => OnMultiButtonClicked(btnObj));
                    }
                }
            }
        }
        else if (step.type == TutorialType.ActionTrigger)
        {
            // 🔒 [통합] 행동 감지 미션일 때: 화면 UI 조작은 다 막아버리고 대기 상태 진입
            RemoveDimButtonListener();
            if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(false);

            Debug.Log($"[Tutorial] 인게임 행동 미션 대기 시작 -> 목표: [{step.targetActionName}]");
        }
    }

    private void LockAndHighlightButton(GameObject buttonObj, UnityEngine.Events.UnityAction action)
    {
        if (buttonObj == null) return;

        // 1. 🔥 [수정] Canvas가 없으면 붙이고, 반드시 GetComponent로 안전하게 인스턴스를 새로 가져옵니다.
        if (buttonObj.GetComponent<Canvas>() == null)
        {
            buttonObj.AddComponent<Canvas>();
        }

        Canvas targetCanvas = buttonObj.GetComponent<Canvas>();

        // 2. 확보된 Canvas가 확실히 존재할 때만 정렬 순서 조작 (타이밍 에러 방지)
        if (targetCanvas != null)
        {
            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = 999;
        }

        // 3. GraphicRaycaster도 안전하게 체크 후 부착
        if (buttonObj.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            buttonObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 4. 손가락 배치 (단일 버튼 트리거일 때만)
        if (_steps[_currentStepIndex].type == TutorialType.ButtonTrigger && _fingerPointer != null)
        {
            _fingerPointer.gameObject.SetActive(true);
            _fingerPointer.position = buttonObj.transform.position + new Vector3(0, 50f, 0);
        }

        // 5. 버튼 컴포넌트 이벤트 연결
        UnityEngine.UI.Button btn = buttonObj.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }
    }

    private void OnMultiButtonClicked(GameObject clickedButton) => HandleMultiButtonClickAsync(clickedButton).Forget();
    private async UniTaskVoid HandleMultiButtonClickAsync(GameObject clickedButton)
    {
        await UniTask.Yield(PlayerLoopTiming.Update);

        if (_remainingButtons.Contains(clickedButton))
        {
            Debug.Log($"[Tutorial] 동적 멀티 버튼 클릭됨: {clickedButton.name}");
            ResetButtonComponent(clickedButton);
            _remainingButtons.Remove(clickedButton);

            if (_remainingButtons.Count == 0)
            {
                AdvanceStep();
            }
        }
    }

    private void ResetButtonComponent(GameObject targetObj)
    {
        if (targetObj == null) return;
        var raycaster = targetObj.GetComponent<UnityEngine.UI.GraphicRaycaster>(); if (raycaster != null) Destroy(raycaster);
        var canvas = targetObj.GetComponent<Canvas>(); if (canvas != null) Destroy(canvas);
    }

    private void OnTargetButtonClicked() => HandleTargetButtonClickedAsync().Forget();
    private async UniTaskVoid HandleTargetButtonClickedAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
        AdvanceStep();
    }

    public void AdvanceStep()
    {
        if (_currentStepIndex < _steps.Count && _steps[_currentStepIndex].type == TutorialType.ButtonTrigger)
        {
            string targetId = _steps[_currentStepIndex].targetButtonId;
            if (_activeDynamicButtons.TryGetValue(targetId, out GameObject btnObj))
            {
                ResetButtonComponent(btnObj);
                if (btnObj != null)
                {
                    var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null) btn.onClick.RemoveListener(OnTargetButtonClicked);
                }
            }
        }

        _currentStepIndex++;
        if (_currentStepIndex >= _steps.Count)
        {
            Debug.Log("[Tutorial] 모든 튜토리얼 시퀀스 완전 종료.");
            if (_dimOverlay != null) _dimOverlay.SetActive(false);
            if (_fingerPointer != null) _fingerPointer.gameObject.SetActive(false);
            if (_tutorialWindowObj != null) _tutorialWindowObj.SetActive(false);
            return;
        }

        ExecuteStep(_steps[_currentStepIndex]);
    }

    #region Dim Setup
    private void SetupDimAsNextButton()
    {
        if (_dimOverlay == null) return;
        UnityEngine.UI.Button dimBtn = _dimOverlay.GetComponent<UnityEngine.UI.Button>() ?? _dimOverlay.AddComponent<UnityEngine.UI.Button>();
        dimBtn.onClick.RemoveListener(OnTextOnlyClicked); dimBtn.onClick.AddListener(OnTextOnlyClicked);
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