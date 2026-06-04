using R3;
using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// 대화 시스템 Presenter.
    /// 이벤트 구독 → View 바인딩.
    /// </summary>
    public class DialoguePresenter : MonoBehaviour
    {
        [Header("대화 View (씬에 미리 배치)")]
        [SerializeField] private PlayerDialogueView   _playerView;
        [SerializeField] private EmployeeDialogueView _employeeView;

        [Header("선택지 View (씬에 미리 배치, 최대 2개)")]
        [SerializeField] private ChoiceItemView _choiceItem01;
        [SerializeField] private ChoiceItemView _choiceItem02;

        private DialogueBaseView _currentView;
        private bool _isChoiceMode;

        private void Awake()
        {
            DialogueEvents.OnDialogueReady
                .Subscribe(payload => OnDialogueReady(payload))
                .AddTo(this);

            DialogueEvents.OnDialogueEnded
                .Subscribe(_ => HideAll())
                .AddTo(this);

            HideAll();
        }

        void OnDialogueReady(DialogueStartPayload payload)
        {
            HideChoices();
            _isChoiceMode = false;
            if (_currentView != null) _currentView.OnTypingComplete = null;

            if (payload.isUser)
            {
                _employeeView.gameObject.SetActive(false);
                _currentView = _playerView;
                _playerView.Bind(payload.desc, payload.text);
            }
            else
            {
                _playerView.gameObject.SetActive(false);
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
                _isChoiceMode = true;
                _currentView.OnTypingComplete = () =>
                {
                    _currentView.SetNextButtonVisible(false);
                    ShowChoice(_choiceItem01, payload.choice01, 0);
                    ShowChoice(_choiceItem02, payload.choice02, 1);
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
                onSelected = OnChoiceSelected,
            });
        }

        void OnChoiceSelected(int index)
        {
            _isChoiceMode = false;
            HideChoices();
            if (_currentView != null) _currentView.SetNextButtonVisible(true);
            DialogueManager.Instance.SubmitChoice(index);
        }

        void HideChoices()
        {
            if (_choiceItem01 != null) _choiceItem01.gameObject.SetActive(false);
            if (_choiceItem02 != null) _choiceItem02.gameObject.SetActive(false);
        }

        void HideAll()
        {
            if (_playerView   != null) _playerView.SetNextButtonVisible(true);
            if (_employeeView != null) _employeeView.SetNextButtonVisible(true);
            if (_playerView   != null) _playerView.gameObject.SetActive(false);
            if (_employeeView != null) _employeeView.gameObject.SetActive(false);
            _currentView = null;
            _isChoiceMode = false;
            HideChoices();
            GameManager.Instance?.player?.CloseInteractionUI();
        }
    }
}