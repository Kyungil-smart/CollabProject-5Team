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
        }

        public void StartDialogue(int employeeId, EmployeeDialogueState state)
        {
            if (_isDialogueRunning) return;

            _isDialogueRunning = true;
            _currentEmployeeId = employeeId;
            _currentState      = state;

            DialogueEvents.NotifyDialogueReady(new DialogueStartPayload
            {
                employeeId = employeeId,
                state      = state,
            });
        }

        public void SubmitChoice(int selectedIndex)
        {
            if (!_isDialogueRunning) return;

            bool isCrisis = _currentState != EmployeeDialogueState.Normal;

            if (isCrisis)
            {
                DialogueEvents.RequestStatChange(new StatDelta { employeeId = _currentEmployeeId });
                if (selectedIndex == 0) DialogueEvents.RequestBonusPay(_currentEmployeeId);
                if (selectedIndex == 1) DialogueEvents.NotifyVacationPromised(_currentEmployeeId);
            }
            else
            {
                DialogueEvents.RequestStatChange(new StatDelta { employeeId = _currentEmployeeId });
            }

            _isDialogueRunning = false;
        }
    }
}