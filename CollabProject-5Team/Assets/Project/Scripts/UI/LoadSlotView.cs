using DG.Tweening;
using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 세이브 로드 슬롯 단일 항목 View.
    /// 데이터 바인딩 및 선택 이벤트 발행 담당.
    /// </summary>
    public sealed class LoadSlotView : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private Button      _slotButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Save Info")]
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _dateLabel;
        [SerializeField] private TextMeshProUGUI _sizeLabel;

        [Header("State Objects")]
        [SerializeField] private GameObject _emptyOverlay;
        [SerializeField] private Image      _slotIcon;

        [Header("Slot Index")]
        [SerializeField] private int _slotIndex;

        public event Action<int> OnSelected;

        private void Awake()
        {
            _canvasGroup.alpha = 0f;

            _slotButton.OnClickAsObservable()
                .Subscribe(_ => HandleClick())
                .AddTo(this);
        }

        public void Bind(SaveSlotData data)
        {
            bool isEmpty = data == null;

            if (_emptyOverlay != null)
                _emptyOverlay.SetActive(isEmpty);

            _slotButton.interactable = !isEmpty;

            if (!isEmpty)
            {
                _titleLabel.text = data.SlotName;
                _dateLabel.text  = data.SaveDate;
                _sizeLabel.text  = data.FileSize;
            }
            else
            {
                _titleLabel.text = "빈 슬롯";
                _dateLabel.text  = string.Empty;
                _sizeLabel.text  = string.Empty;
            }
        }

        public void PlayEntrance(float delaySeconds)
        {
            DOTween.Kill(_canvasGroup);
            _canvasGroup.alpha = 0f;
            _canvasGroup.DOFade(1f, 0.25f).SetDelay(delaySeconds).SetEase(Ease.OutQuad).SetTarget(_canvasGroup);
        }

        private void HandleClick()
        {
            transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, vibrato: 5).SetTarget(transform);
            OnSelected?.Invoke(_slotIndex);
        }
    }

    [Serializable]
    public sealed class SaveSlotData
    {
        public string SlotName;
        public string SaveDate;
        public string FileSize;
    }
}