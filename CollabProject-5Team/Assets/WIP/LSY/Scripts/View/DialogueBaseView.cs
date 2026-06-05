using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogue
{
    /// <summary>
    /// 대화 View 기반 클래스 - 타이핑 효과 + 다음 버튼 공통 처리
    /// </summary>
    public abstract class DialogueBaseView : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI _dialogueText;

        [Header("다음 버튼")]
        [SerializeField] private Button _nextButton;

        [Header("타이핑 속도")]
        [SerializeField] private float _charInterval = 0.03f;

        private Tween _typingTween;

        public bool IsTyping { get; private set; }

        public System.Action OnTypingComplete;

        public System.Action OnNextAction;

        public void SetNextButtonVisible(bool visible)
        {
            if (_nextButton != null)
                _nextButton.gameObject.SetActive(visible);
        }

        private void Awake()
        {
            if (_nextButton != null)
                _nextButton.onClick.AddListener(OnNextClicked);
        }

        private void OnNextClicked()
        {
            if (IsTyping)
                SkipTyping();
            else if (OnNextAction != null)
                OnNextAction.Invoke();
            else
                DialogueManager.Instance.AdvanceDialogue();
        }

        protected void StartTyping(string text)
        {
            if (_typingTween != null) _typingTween.Kill();

            _dialogueText.text = text;
            IsTyping = true;

            _typingTween = _dialogueText.DOTypewriter(text, text.Length * _charInterval)
                .OnComplete(() =>
                {
                    IsTyping = false;
                    OnTypingComplete?.Invoke();
                });
        }

        public void SkipTyping()
        {
            if (_typingTween != null)
            {
                _typingTween.Kill();
                _typingTween = null;
            }
            _dialogueText.ShowAll();
            IsTyping = false;
            OnTypingComplete?.Invoke();
        }

        protected virtual void OnDisable()
        {
            if (_typingTween != null)
            {
                _typingTween.Kill();
                _typingTween = null;
            }
            IsTyping = false;
        }
    }
}