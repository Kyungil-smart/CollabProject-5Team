using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// ScrollRect 스크롤 위치를 Slider에 단방향 반영.
    /// Slider는 표시 전용(interactable = false).
    /// Content 높이가 Viewport 이하일 경우 Slider GO 비활성.
    /// </summary>
    public sealed class ScrollFollowSlider : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Slider _slider;

        private RectTransform _content;
        private RectTransform _viewport;

        private void Awake()
        {
            _slider.interactable = false;

            _content = _scrollRect.content;
            _viewport = _scrollRect.viewport;

            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void OnDestroy()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        /// <summary>
        /// Content 동적 생성 완료 후 호출 — Slider 활성 여부 갱신.
        /// </summary>
        public void RefreshSliderVisibility()
        {
            bool needsScroll = _content.rect.height > _viewport.rect.height;
            _slider.gameObject.SetActive(needsScroll);

            if (needsScroll)
                _slider.value = _scrollRect.verticalNormalizedPosition;
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            if (!_slider.gameObject.activeSelf) return;
            _slider.value = value.y;
        }
    }
}