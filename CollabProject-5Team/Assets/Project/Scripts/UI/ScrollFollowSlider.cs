using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// ScrollRect와 Slider를 양방향으로 동기화.
    /// Slider 드래그 시 ScrollRect 위치 반영, ScrollRect 스크롤 시 Slider 위치 반영.
    /// Content 높이가 Viewport 이하일 경우 Slider GO 비활성.
    /// </summary>
    public sealed class ScrollFollowSlider : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Slider _slider;

        private RectTransform _content;
        private RectTransform _viewport;

        private bool _isSyncingFromScroll;
        private bool _isSyncingFromSlider;

        private void Awake()
        {
            _slider.interactable = true;

            _content = _scrollRect.content;
            _viewport = _scrollRect.viewport;

            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnDestroy()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        /// <summary>
        /// Content 동적 생성 완료 후 호출 — Slider 활성 여부 갱신.
        /// </summary>
        public void RefreshSliderVisibility()
        {
            bool needsScroll = _content.rect.height > _viewport.rect.height;
            _slider.gameObject.SetActive(needsScroll);

            if (needsScroll)
            {
                _isSyncingFromScroll = true;
                _slider.value = _scrollRect.verticalNormalizedPosition;
                _isSyncingFromScroll = false;
            }
        }

        private void OnScrollValueChanged(Vector2 value)
        {
            if (!_slider.gameObject.activeSelf) return;
            if (_isSyncingFromSlider) return;

            _isSyncingFromScroll = true;
            _slider.value = value.y;
            _isSyncingFromScroll = false;
        }

        private void OnSliderValueChanged(float value)
        {
            if (_isSyncingFromScroll) return;

            _isSyncingFromSlider = true;
            _scrollRect.verticalNormalizedPosition = value;
            _isSyncingFromSlider = false;
        }
    }
}