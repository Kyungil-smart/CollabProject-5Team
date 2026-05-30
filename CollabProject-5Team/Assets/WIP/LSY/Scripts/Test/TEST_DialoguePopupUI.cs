using R3;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogue
{
    /// <summary> 대화 시스템 테스트용 간단 팝업 UI </summary>
    public class TEST_DialoguePopupUI : MonoBehaviour
    {
        [Header("대사 텍스트")]
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Header("선택지 버튼 (isChoice=true, 타이핑 완료 후 표시)")]
        [SerializeField] private Button          _choiceBtn01;
        [SerializeField] private Button          _choiceBtn02;
        [SerializeField] private TextMeshProUGUI _choiceText01;
        [SerializeField] private TextMeshProUGUI _choiceText02;

        [Header("PlayerMove 연결")]
        [SerializeField] private PlayerMove _playerMove;

        [Header("타이핑 속도 (초/글자)")]
        [SerializeField] private float _charInterval = 0.03f;

        private bool  _isTyping;
        private bool  _isChoiceNode;
        private Tween _typingTween;
        private bool  _skipThisFrame;

        private void Awake()
        {
            DialogueEvents.OnDialogueReady
                .Subscribe(payload => OnDialogueReady(payload))
                .AddTo(this);

            DialogueEvents.OnDialogueEnded
                .Subscribe(_ => ClosePopup())
                .AddTo(this);

            _choiceBtn01.onClick.AddListener(() => OnChoiceClicked(0));
            _choiceBtn02.onClick.AddListener(() => OnChoiceClicked(1));

            HideChoiceButtons();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;

            if (_isChoiceNode) return;

            if (_skipThisFrame) { _skipThisFrame = false; return; }

            bool touched = Input.GetMouseButtonDown(0);
            if (!touched && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                touched = true;

            if (!touched) return;

            if (_isTyping)
                SkipTyping();
            else
                DialogueManager.Instance.AdvanceDialogue();
        }

        void OnDialogueReady(DialogueStartPayload payload)
        {
            gameObject.SetActive(true);
            _isChoiceNode = payload.isChoice;

            HideChoiceButtons();

            if (_isChoiceNode)
            {
                _choiceText01.text = payload.choice01;
                _choiceText02.text = payload.choice02;
            }

            StartTyping(payload.text);
        }

        void StartTyping(string text)
        {
            _isTyping = true;
            if (_typingTween != null) _typingTween.Kill();

            _typingTween = _dialogueText.DOTypewriter(text, text.Length * _charInterval)
                .OnComplete(() =>
                {
                    _isTyping = false;
                    OnTypingComplete();
                });
        }

        void SkipTyping()
        {
            if (_typingTween != null)
            {
                _typingTween.Kill();
                _typingTween = null;
            }
            _dialogueText.ShowAll();
            _isTyping = false;
            OnTypingComplete();
        }

        void OnTypingComplete()
        {
            if (_isChoiceNode)
            {
                _choiceBtn01.gameObject.SetActive(true);
                _choiceBtn02.gameObject.SetActive(true);
            }
        }

        void OnChoiceClicked(int index)
        {
            _skipThisFrame = true;
            HideChoiceButtons();
            DialogueManager.Instance.SubmitChoice(index);
        }

        void HideChoiceButtons()
        {
            _choiceBtn01.gameObject.SetActive(false);
            _choiceBtn02.gameObject.SetActive(false);
        }

        void ClosePopup()
        {
            if (_typingTween != null)
            {
                _typingTween.Kill();
                _typingTween = null;
            }
            _dialogueText.ShowAll();
            gameObject.SetActive(false);
            _playerMove?.CloseInteractionUI();
        }

    }
}
