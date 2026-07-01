using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dialogue
{
    public abstract class DialogueBaseView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] protected TextMeshProUGUI _dialogueText;

        [Header("타이핑 속도")]
        [SerializeField] private float _charInterval = 0.03f;

        private Tween _typingTween;
        private bool _isChoiceMode;

        public bool IsTyping { get; private set; }

        public System.Action OnTypingComplete;
        public System.Action OnNextAction;

        public void SetChoiceMode(bool isChoice)
        {
            _isChoiceMode = isChoice;
        }

        private void OnNextClicked()
        {
            if (_isChoiceMode) return;

            if (IsTyping)
            {
                SkipTyping();
                return;
            }

            if (OnNextAction != null)
                OnNextAction.Invoke();
            else
                Advance();
        }

        private void Advance()
        {
            if (StoryDialoguePlayer.Instance != null && StoryDialoguePlayer.Instance.IsDialogueRunning)
                StoryDialoguePlayer.Instance.AdvanceDialogue();
            else
                DialogueManager.Instance.AdvanceDialogue();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnNextClicked();
        }

        protected void StartTyping(string text)
        {
            if (_typingTween != null) _typingTween.Kill();

            if (string.IsNullOrEmpty(text))
            {
                _dialogueText.text = string.Empty;
                IsTyping = false;
                OnTypingComplete?.Invoke();
                return;
            }

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
            _isChoiceMode = false;
        }
    }
}
