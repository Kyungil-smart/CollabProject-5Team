using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI
{
    /// <summary>
    /// Canvas_Alert 담당 View.
    /// ConfirmPopup, FireConfirmPopup, AlertPopup, NoticePopup, SpyPopup 표시 제어.
    /// 팝업 간 배타적 활성화는 Show 메서드 호출 측에서 보장.
    /// 타이틀 씬에서는 ConfirmPopup만 연결해서 사용 가능.
    /// </summary>
    public sealed class AlertView : MonoBehaviour
    {
        [Header("ConfirmPopup")]
        [SerializeField] private GameObject _confirmPopup;
        [SerializeField] private TextMeshProUGUI _confirmMessageLabel;
        [SerializeField] private Button _confirmPopupConfirmButton;
        [SerializeField] private Button _confirmPopupCancelButton;

        [Header("FireConfirmPopup")]
        [SerializeField] private GameObject _fireConfirmPopup;
        [SerializeField] private TextMeshProUGUI _fireCommentLabel;
        [SerializeField] private Image _fireEmployeeIcon;
        [SerializeField] private TextMeshProUGUI _fireMessageLabel;
        [SerializeField] private Button _fireConfirmButton;
        [SerializeField] private Button _fireCancelButton;

        [Header("AlertPopup")]
        [SerializeField] private GameObject _alertPopup;
        [SerializeField] private TextMeshProUGUI _alertMessageLabel;
        [SerializeField] private Button _alertConfirmButton;

        [Header("NoticePopup")]
        [SerializeField] private GameObject _noticePopup;
        [SerializeField] private TextMeshProUGUI _noticeCommentLabel;
        [SerializeField] private TextMeshProUGUI _noticeEmployeeNameLabel;
        [SerializeField] private Image _noticeEmployeeIcon;

        [Header("SpyPopup")]
        [SerializeField] private GameObject _spyPopup;
        [SerializeField] private Button _spyConfirmButton;
        [SerializeField] private Button _spyCancelButton;

        private const float NOTICE_DURATION = 3f;
        private const float POPUP_FADE_DURATION = 0.15f;

        private IDisposable _confirmPopupConfirmSubscription;
        private IDisposable _confirmPopupCancelSubscription;
        private IDisposable _fireConfirmSubscription;
        private IDisposable _fireCancelSubscription;
        private IDisposable _alertConfirmSubscription;
        private IDisposable _spySubscription;
        private IDisposable _spyCancelSubscription;

        private void Awake()
        {
            if (_confirmPopup != null) _confirmPopup.SetActive(false);
            if (_fireConfirmPopup != null) _fireConfirmPopup.SetActive(false);
            if (_alertPopup != null) _alertPopup.SetActive(false);
            if (_noticePopup != null) _noticePopup.SetActive(false);
            if (_spyPopup != null) _spyPopup.SetActive(false);
        }

        /// <summary>
        /// 확인/취소 팝업. onConfirm/onCancel 콜백은 버튼 클릭 1회 후 자동 해제.
        /// </summary>
        public void ShowConfirmPopup(string message, Action onConfirm, Action onCancel = null)
        {
            if (_confirmPopup == null) return;

            ClearConfirmPopupSubscriptions();
            _confirmMessageLabel.text = message;
            _confirmPopup.SetActive(true);

            _confirmPopupConfirmSubscription = _confirmPopupConfirmButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearConfirmPopupSubscriptions();
                    _confirmPopup.SetActive(false);
                    onConfirm?.Invoke();
                });

            _confirmPopupCancelSubscription = _confirmPopupCancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearConfirmPopupSubscriptions();
                    _confirmPopup.SetActive(false);
                    onCancel?.Invoke();
                });
        }

        /// <summary>
        /// 해고 확인 팝업. 직원 코멘트와 퇴직금 메시지를 함께 표시.
        /// </summary>
        public void ShowFireConfirmPopup(string employeeComment, Sprite employeeSprite,
            string message, Action onConfirm, Action onCancel = null)
        {
            if (_fireConfirmPopup == null) return;

            ClearFireConfirmPopupSubscriptions();
            _fireCommentLabel.text = employeeComment;
            _fireEmployeeIcon.sprite = employeeSprite;
            _fireMessageLabel.text = message;
            _fireConfirmPopup.SetActive(true);

            _fireConfirmSubscription = _fireConfirmButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearFireConfirmPopupSubscriptions();
                    _fireConfirmPopup.SetActive(false);
                    onConfirm?.Invoke();
                });

            _fireCancelSubscription = _fireCancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearFireConfirmPopupSubscriptions();
                    _fireConfirmPopup.SetActive(false);
                    onCancel?.Invoke();
                });
        }

        /// <summary>
        /// 단순 알림 팝업 (보유자금 부족 등). 확인 버튼으로 닫힘.
        /// </summary>
        public void ShowAlertPopup(string message)
        {
            if (_alertPopup == null) return;

            ClearAlertPopupSubscription();
            _alertMessageLabel.text = message;
            _alertPopup.SetActive(true);

            _alertConfirmSubscription = _alertConfirmButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearAlertPopupSubscription();
                    _alertPopup.SetActive(false);
                });
        }

        /// <summary>
        /// 버튼 없는 자동 닫힘 알림 (해고 후 마지막 코멘트). NOTICE_DURATION 후 자동 비활성화.
        /// </summary>
        public void ShowNoticePopup(string comment, string employeeName, Sprite employeeSprite)
        {
            if (_noticePopup == null) return;

            _noticeCommentLabel.text = comment;
            _noticeEmployeeNameLabel.text = employeeName;
            _noticeEmployeeIcon.sprite = employeeSprite;

            _noticePopup.SetActive(true);
            WaitAndHideNoticeAsync().Forget();
        }

        /// <summary>
        /// 최종 스파이 지목 확인 팝업. 고정 텍스트 형태이므로 매개변수 없이 이벤트를 바인딩합니다.
        /// </summary>
        public void ShowSpyConfirmPopup(Action onConfirm, Action onCancel = null)
        {
            if (_spyPopup == null) return;

            ClearSpyPopupSubscriptions();
            _spyPopup.SetActive(true);

            _spySubscription = _spyConfirmButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearSpyPopupSubscriptions();
                    _spyPopup.SetActive(false);
                    onConfirm?.Invoke();
                });

            _spyCancelSubscription = _spyCancelButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClearSpyPopupSubscriptions();
                    _spyPopup.SetActive(false);
                    onCancel?.Invoke();
                });
        }

        public void HideAll()
        {
            ClearConfirmPopupSubscriptions();
            ClearFireConfirmPopupSubscriptions();
            ClearAlertPopupSubscription();
            ClearSpyPopupSubscriptions();

            if (_confirmPopup != null) _confirmPopup.SetActive(false);
            if (_fireConfirmPopup != null) _fireConfirmPopup.SetActive(false);
            if (_alertPopup != null) _alertPopup.SetActive(false);
            if (_noticePopup != null) _noticePopup.SetActive(false);
            if (_spyPopup != null) _spyPopup.SetActive(false);
        }

        private void OnDestroy()
        {
            ClearConfirmPopupSubscriptions();
            ClearFireConfirmPopupSubscriptions();
            ClearAlertPopupSubscription();
            ClearSpyPopupSubscriptions();
        }

        private async UniTaskVoid WaitAndHideNoticeAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(NOTICE_DURATION),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            if (_noticePopup != null)
                _noticePopup.SetActive(false);
        }

        private void ClearConfirmPopupSubscriptions()
        {
            _confirmPopupConfirmSubscription?.Dispose();
            _confirmPopupCancelSubscription?.Dispose();
            _confirmPopupConfirmSubscription = null;
            _confirmPopupCancelSubscription = null;
        }

        private void ClearFireConfirmPopupSubscriptions()
        {
            _fireConfirmSubscription?.Dispose();
            _fireCancelSubscription?.Dispose();
            _fireConfirmSubscription = null;
            _fireCancelSubscription = null;
        }

        private void ClearAlertPopupSubscription()
        {
            _alertConfirmSubscription?.Dispose();
            _alertConfirmSubscription = null;
        }

        private void ClearSpyPopupSubscriptions()
        {
            _spySubscription?.Dispose();
            _spyCancelSubscription?.Dispose();
            _spySubscription = null;
            _spyCancelSubscription = null;
        }
    }
}