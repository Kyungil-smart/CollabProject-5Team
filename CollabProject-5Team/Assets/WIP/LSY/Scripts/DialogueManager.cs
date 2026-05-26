using UnityEngine;

namespace Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private bool _isDialogueRunning;
        private int _currentEmployeeId;
        private EmployeeDialogueState _currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartDialogue(int employeeId, EmployeeDialogueState state)
        {
            if (_isDialogueRunning) return;
            if (DayTalkTracker.Instance.IsAtDailyLimit()) return;
            if (DayTalkTracker.Instance.HasTalkedToday(employeeId)) return;

            _isDialogueRunning = true;
            _currentEmployeeId = employeeId;
            _currentState = state;

            DialogueEvents.NotifyDialogueReady(new DialogueStartPayload
            {
                employeeId = employeeId,
                state = state,
            });
        }

        public void SubmitChoice(int selectedIndex)
        {
            if (!_isDialogueRunning) return;

            if (EmployeeStateChecker.IsInCrisis(_currentState))
            {
                DialogueChoiceHandler.ApplyChoice(_currentEmployeeId, (DialogueChoice)selectedIndex);
            }
            else
            {
                // TODO: DialogueDataSO 완성 후 교체
                DialogueChoiceHandler.ApplyNormalResult(_currentEmployeeId, isCorrect: false);
            }

            DayTalkTracker.Instance.MarkTalked(_currentEmployeeId);
            _isDialogueRunning = false;
        }
    }
}