using R3;
using UnityEngine;

namespace Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private const int EndNodeId = 20000;

        private bool _isDialogueRunning;
        private int  _currentEmployeeId;
        private EmployeeDialogueState _currentState;
        private DialoguePoolEntrySO _currentPoolEntry;
        private int  _currentNodeId;
        private int  _chosenBranch;

        [Header("대화 View (씬에 미리 배치)")]
        [SerializeField] private PlayerDialogueView   _playerView;
        [SerializeField] private EmployeeDialogueView _employeeView;

        [Header("선택지 View (씬에 미리 배치, 최대 2개)")]
        [SerializeField] private ChoiceItemView _choiceItem01;
        [SerializeField] private ChoiceItemView _choiceItem02;

        private DialogueBaseView _currentView;

        private NPCController _currentNpcController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            DialogueEvents.OnDialogueReady
                .Subscribe(payload => BindViews(payload))
                .AddTo(this);

            DialogueEvents.OnDialogueEnded
                .Subscribe(_ => HideAll())
                .AddTo(this);

        }
        private void Start() => HideAll();

        public void StartDialogueById(Employee emp)
        {
            EmployeeDialogueState state = GetDialogueState(emp.MutableData.fatigue, emp.MutableData.desire);
            StartDialogue(emp.so.id, state);
        }

        public void StartDialogue(int employeeId, EmployeeDialogueState state)
        {
            if (_isDialogueRunning) return;

            Employee emp = _EmployeeManager.Instance.haveEmployees.haveEmployeeList.Find(e => e.so.id == employeeId);
            if (emp != null)
            {
                _currentNpcController = emp.GetComponent<NPCController>();
                _currentNpcController?.StartConversation();

                CameraManager.Instance.IsUIOpen.Value = true;
                CameraManager.Instance.FocusOnTarget(emp.transform.position);
            }

            DialoguePoolEntrySO poolEntry = DialogueDataManager.Instance.GetPoolEntry(employeeId, state);
            if (poolEntry == null)
            {
                DateTimeManager.Instance.CompleteSpecialDialogue(employeeId.ToString());
                DialogueEvents.NotifyDialogueEnded(employeeId);
                Debug.Log($"[DM] 풀항목없음으로 종료 — id={employeeId}, state={state}");
                return;
            }

            _isDialogueRunning = true;
            _currentEmployeeId = employeeId;
            _currentState      = state;
            _currentPoolEntry  = poolEntry;
            _chosenBranch      = 0;

            ShowNode(poolEntry.talkId);
        }

        public void SubmitChoice(int selectedIndex)
        {
            if (!_isDialogueRunning) return;

            DialogueNodeSO node = DialogueDataManager.Instance.GetNode(_currentNodeId);
            if (node == null) return;

            if (_chosenBranch == 0)
                _chosenBranch = selectedIndex + 1;

            HideChoices();
            if (_currentView != null) _currentView.SetChoiceMode(false);

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

        /// <summary>
        /// 이미 대화한 직원 클릭 시 간단 메시지 표시 (state==2)
        /// </summary>
        public void ShowBusyMessage(Employee emp, string message = "지금은 좀 바빠 보인다...")
        {
            if (_isDialogueRunning) return;

            _isDialogueRunning   = true;
            _currentEmployeeId   = emp.so.id;
            _currentNpcController = emp.GetComponent<NPCController>();

            CameraManager.Instance.IsUIOpen.Value = true;
            CameraManager.Instance.FocusOnTarget(emp.transform.position);

            _currentView = _playerView;
            _playerView.OnTypingComplete = null;
            _playerView.OnNextAction     = () =>
            {
                _isDialogueRunning = false;
                HideAll();
            };

            _playerView.Bind("", message);
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
                Debug.Log($"[DM] 노드ID종료 — nodeId={nodeId}");
                return;
            }

            DialogueNodeSO node = DialogueDataManager.Instance.GetNode(nodeId);
            if (node == null) { EndDialogue(); return; }

            _currentNodeId = nodeId;

            DialogueEvents.NotifyDialogueReady(new DialogueStartPayload
            {
                employeeId = _currentEmployeeId,
                state      = _currentState,
                desc       = node.desc,
                text       = node.text,
                isChoice   = node.isChoice,
                isUser     = node.isUser,
                choice01   = node.choice01,
                choice02   = node.choice02,
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

            DateTimeManager.Instance.CompleteSpecialDialogue(_currentEmployeeId.ToString());
            DialogueEvents.NotifyDialogueEnded(_currentEmployeeId);

            CameraManager.Instance.ResetCamera();
            CameraManager.Instance.IsUIOpen.Value = false;

            if (_currentNpcController != null)
            {
                _currentNpcController.EndConversation();
                _currentNpcController = null;
            }

            _isDialogueRunning = false;
            _currentPoolEntry  = null;
        }

        void BindViews(DialogueStartPayload payload)
        {
            HideChoices();
            if (_currentView != null)
            {
                _currentView.SetChoiceMode(false);
                _currentView.OnTypingComplete = null;
                _currentView.OnNextAction     = null;
                _currentView.gameObject.SetActive(false);
            }

            if (payload.isUser)
            {
                _currentView = _playerView;
                _playerView.Bind(payload.desc, payload.text);
            }
            else
            {
                _currentView = _employeeView;

                Employee emp = _EmployeeManager.Instance.haveEmployees.haveEmployeeList
                    .Find(e => e.so.id == payload.employeeId);

                Sprite portrait = null;
                if (emp != null)
                {
                    portrait = payload.state switch
                    {
                        EmployeeDialogueState.Normal   => emp.so.iconNormal,
                        EmployeeDialogueState.Caution  => emp.so.iconCaution,
                        EmployeeDialogueState.Critical => emp.so.iconCritical,
                        _                              => emp.so.iconNormal,
                    };
                }

                _employeeView.Bind(new EmployeeDialogueViewData
                {
                    desc     = payload.desc,
                    text     = payload.text,
                    portrait = portrait,
                });
            }

            if (payload.isChoice)
            {
                var capturedPayload = payload;
                _currentView.OnTypingComplete = () =>
                {
                    DialogueEvents.OnNodeTypingCompleted.OnNext(_currentNodeId);
                    EnterChoiceMode(capturedPayload);
                };
            }
            else
            {
                _currentView.OnTypingComplete = () =>
                {
                    DialogueEvents.OnNodeTypingCompleted.OnNext(_currentNodeId);
                };
            }
        }

        void ShowChoice(ChoiceItemView item, string text, int index)
        {
            if (string.IsNullOrEmpty(text)) { item.gameObject.SetActive(false); return; }

            item.gameObject.SetActive(true);
            item.Bind(new ChoiceItemViewData
            {
                text       = text,
                index      = index,
                onSelected = SubmitChoice,
            });
        }

        void EnterChoiceMode(DialogueStartPayload payload)
        {
            _currentView.SetChoiceMode(true);
            ShowChoice(_choiceItem01, payload.choice01, 0);
            ShowChoice(_choiceItem02, payload.choice02, 1);
        }

        void HideChoices()
        {
            if (_choiceItem01 != null) _choiceItem01.gameObject.SetActive(false);
            if (_choiceItem02 != null) _choiceItem02.gameObject.SetActive(false);
        }

        void HideAll()
        {
            CameraManager.Instance.ResetCamera();
            CameraManager.Instance.IsUIOpen.Value = false;

            if (_currentNpcController != null)
            {
                _currentNpcController.EndConversation();
                _currentNpcController = null;
            }
            
            if (_playerView   != null) _playerView.gameObject.SetActive(false);
            if (_employeeView != null) _employeeView.gameObject.SetActive(false);
            _currentView   = null;
            HideChoices();
            GameManager.Instance?.player?.CloseInteractionUI();
        }
    }
}
