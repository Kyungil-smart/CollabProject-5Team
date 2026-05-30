using UnityEngine;

namespace Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private const int EndNodeId = 20000;

        private bool _isDialogueRunning;
        private int _currentEmployeeId;
        private string _currentNpcName;
        private EmployeeDialogueState _currentState;

        private DialoguePoolEntrySO _currentPoolEntry;
        private int _currentNodeId;
        private int _chosenBranch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartDialogueById(int employeeId, string npcName)
        {
            Employee emp = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                .Find(e => e.so.id == employeeId);

            if (emp == null)
            {
                Debug.LogWarning($"[DialogueManager] employeeId {employeeId} 직원을 찾을 수 없습니다.");
                return;
            }

            EmployeeDialogueState state = GetDialogueState(emp.MutableData.fatigue, emp.MutableData.desire);

            StartDialogue(employeeId, npcName, state);
        }

        public void StartDialogue(int employeeId, string npcName, EmployeeDialogueState state)
        {
            if (_isDialogueRunning) return;

            DialoguePoolEntrySO poolEntry = DialogueDataManager.Instance.GetPoolEntry(employeeId, state);
            if (poolEntry == null)
            {
                DialogueEvents.NotifyDialogueEnded(employeeId);
                return;
            }

            _isDialogueRunning = true;
            _currentEmployeeId = employeeId;
            _currentNpcName = npcName;
            _currentState = state;
            _currentPoolEntry = poolEntry;
            _chosenBranch = 0;

            ShowNode(poolEntry.talkId);
        }

        public void SubmitChoice(int selectedIndex)
        {
            if (!_isDialogueRunning) return;

            DialogueNodeSO node = DialogueDataManager.Instance.GetNode(_currentNodeId);
            if (node == null) return;

            if (_chosenBranch == 0)
                _chosenBranch = selectedIndex + 1;

            int nextId = selectedIndex == 0 ? node.nextId01 : node.nextId02;
            AdvanceTo(nextId);
        }

        public void AdvanceDialogue()
        {
            if (!_isDialogueRunning) return;

            DialogueNodeSO node = DialogueDataManager.Instance.GetNode(_currentNodeId);
            if (node == null) return;

            AdvanceTo(node.nextId);
        }

        static EmployeeDialogueState GetDialogueState(int fatigue, int desire)
        {
            bool highFatigue   = fatigue > 50;
            bool lowMotivation = desire  < 50;

            if (!highFatigue && !lowMotivation) return EmployeeDialogueState.Normal;
            if ( highFatigue &&  lowMotivation) return EmployeeDialogueState.Critical;
            return EmployeeDialogueState.Caution;
        }

        void ShowNode(int nodeId)
        {
            if (nodeId == EndNodeId || nodeId == 0)
            {
                EndDialogue();
                return;
            }

            DialogueNodeSO node = DialogueDataManager.Instance.GetNode(nodeId);
            if (node == null) { EndDialogue(); return; }

            _currentNodeId = nodeId;

            DialogueEvents.NotifyDialogueReady(new DialogueStartPayload
            {
                employeeId = _currentEmployeeId,
                state = _currentState,
                text = node.text,
                isChoice = node.isChoice,
                choice01 = node.choice01,
                choice02 = node.choice02,
            });
        }

        void AdvanceTo(int nextId)
        {
            if (nextId == EndNodeId || nextId == 0)
                EndDialogue();
            else
                ShowNode(nextId);
        }

        void EndDialogue()
        {
            if (_currentPoolEntry != null && _chosenBranch != 0)
            {
                string effect = _chosenBranch == 1
                    ? _currentPoolEntry.branch01Effect
                    : _currentPoolEntry.branch02Effect;

                DialogueEffectParser.Apply(effect, _currentEmployeeId);
            }

            DateTimeManager.Instance.CompleteSpecialDialogue(_currentNpcName);

            DialogueEvents.NotifyDialogueEnded(_currentEmployeeId);

            _isDialogueRunning = false;
            _currentPoolEntry = null;
        }
    }
}