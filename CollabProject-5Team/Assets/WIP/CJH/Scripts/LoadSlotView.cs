// using DG.Tweening; // TODO: DoTween 패키지 설치 후 주석 해제
// using R3;          // TODO: R3 패키지 설치 후 주석 해제

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
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

            // [R3 설치 후 아래 코드로 교체]
            // _slotButton.OnClickAsObservable().Subscribe(_ => HandleClick()).AddTo(this);

            _slotButton.onClick.AddListener(HandleClick);

            Debug.LogWarning("[LoadSlotView] DoTween / R3 미설치 상태 - 임시 코드 사용 중");
        }

        private void OnDestroy()
        {
            // [DoTween 설치 후 주석 해제]
            // DOTween.Kill(_canvasGroup);
            // DOTween.Kill(transform);
        }

        public void Bind(SaveSlotData data)
        {
            bool isEmpty = data == null;
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
            // [DoTween 설치 후 아래 코드로 교체]
            // DOTween.Kill(_canvasGroup);
            // _canvasGroup.alpha = 0f;
            // _canvasGroup.DOFade(1f, 0.25f).SetDelay(delaySeconds)
            //     .SetEase(Ease.OutQuad).SetTarget(_canvasGroup);

            _canvasGroup.alpha = 1f;
        }

        private void HandleClick()
        {
            // [DoTween 설치 후 아래 코드로 교체]
            // transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, vibrato: 5).SetTarget(transform);

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