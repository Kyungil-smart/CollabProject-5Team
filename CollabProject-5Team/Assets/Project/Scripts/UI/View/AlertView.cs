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
    /// ConfirmPopup, FireConfirmPopup, AlertPopup, NoticePopup, SynergyPopup 표시 제어.
    /// 팝업 간 배타적 활성화는 Show 메서드 호출 측에서 보장.
    /// 타이틀 씬에서는 ConfirmPopup만 연결해서 사용 가능.
    /// </summary>
    public sealed class AlertView : MonoBehaviour
    {
        [Header("ConfirmPopup")]
        [SerializeField] private GameObject      _confirmPopup;
        [SerializeField] private TextMeshProUGUI _confirmMessageLabel;
        [SerializeField] private Button          _confirmPopupConfirmButton;
        [SerializeField] private Button          _confirmPopupCancelButton;

        [Header("FireConfirmPopup")]
        [SerializeField] private GameObject      _fireConfirmPopup;
        [SerializeField] private TextMeshProUGUI _fireCommentLabel;
        [SerializeField] private Image           _fireEmployeeIcon;
        [SerializeField] private TextMeshProUGUI _fireMessageLabel;
        [SerializeField] private Button          _fireConfirmButton;
        [SerializeField] private Button          _fireCancelButton;

        [Header("AlertPopup")]
        [SerializeField] private GameObject      _alertPopup;
        [SerializeField] private TextMeshProUGUI _alertMessageLabel;
        [SerializeField] private Button          _alertConfirmButton;

        [Header("NoticePopup")]
        [SerializeField] private GameObject      _noticePopup;
        [SerializeField] private TextMeshProUGUI _noticeCommentLabel;
        [SerializeField] private TextMeshProUGUI _noticeEmployeeNameLabel;
        [SerializeField] private Image           _noticeEmployeeIcon;

        [Header("SynergyPopup")]
        [SerializeField] private GameObject      _synergyPopup;
        [SerializeField] private Transform       _synergyScrollContent;
        [SerializeField] private Button          _synergyConfirmButton;

        private const float NOTICE_DURATION     = 3f;
        private const float POPUP_FADE_DURATION = 0.15f;

        private void Awake()
        {
            if (_confirmPopup     != null) _confirmPopup.SetActive(false);
            if (_fireConfirmPopup != null) _fireConfirmPopup.SetActive(false);
            if (_alertPopup       != null) _alertPopup.SetActive(false);
            if (_noticePopup      != null) _noticePopup.SetActive(false);
            if (_synergyPopup     != null) _synergyPopup.SetActive(false);
        }

        /// <summary>
        /// 확인/취소 팝업. onConfirm/onCancel 콜백은 버튼 클릭 1회 후 자동 해제.
        /// </summary>
        public void ShowConfirmPopup(string message, Action onConfirm, Action onCancel = null)
        {
            if (_confirmPopup == null) return;

            _confirmMessageLabel.text = message;
            _confirmPopup.SetActive(true);

            // 구독을 Take(1)로 1회 호출 후 자동 해제
            _confirmPopupConfirmButton.OnClickAsObservable()
                .Take(1)
                .Subscribe(_ =>
                {
                    _confirmPopup.SetActive(false);
                    onConfirm?.Invoke();
                })
                .AddTo(_confirmPopup);

            _confirmPopupCancelButton.OnClickAsObservable()
                .Take(1)
                .Subscribe(_ =>
                {
                    _confirmPopup.SetActive(false);
                    onCancel?.Invoke();
                })
                .AddTo(_confirmPopup);
        }

        /// <summary>
        /// 해고 확인 팝업. 직원 코멘트와 퇴직금 메시지를 함께 표시.
        /// </summary>
        public void ShowFireConfirmPopup(string employeeComment, Sprite employeeSprite,
            string message, Action onConfirm, Action onCancel = null)
        {
            if (_fireConfirmPopup == null) return;

            _fireCommentLabel.text   = employeeComment;
            _fireEmployeeIcon.sprite = employeeSprite;
            _fireMessageLabel.text   = message;
            _fireConfirmPopup.SetActive(true);

            _fireConfirmButton.OnClickAsObservable()
                .Take(1)
                .Subscribe(_ =>
                {
                    _fireConfirmPopup.SetActive(false);
                    onConfirm?.Invoke();
                })
                .AddTo(_fireConfirmPopup);

            _fireCancelButton.OnClickAsObservable()
                .Take(1)
                .Subscribe(_ =>
                {
                    _fireConfirmPopup.SetActive(false);
                    onCancel?.Invoke();
                })
                .AddTo(_fireConfirmPopup);
        }

        /// <summary>
        /// 단순 알림 팝업 (보유자금 부족 등). 확인 버튼으로 닫힘.
        /// </summary>
        public void ShowAlertPopup(string message)
        {
            if (_alertPopup == null) return;

            _alertMessageLabel.text = message;
            _alertPopup.SetActive(true);

            _alertConfirmButton.OnClickAsObservable()
                .Take(1)
                .Subscribe(_ => _alertPopup.SetActive(false))
                .AddTo(_alertPopup);
        }

        /// <summary>
        /// 버튼 없는 자동 닫힘 알림 (해고 후 마지막 코멘트). NOTICE_DURATION 후 자동 비활성화.
        /// </summary>
        public void ShowNoticePopup(string comment, string employeeName, Sprite employeeSprite)
        {
            if (_noticePopup == null) return;

            _noticeCommentLabel.text      = comment;
            _noticeEmployeeNameLabel.text = employeeName;
            _noticeEmployeeIcon.sprite    = employeeSprite;

            _noticePopup.SetActive(true);
            WaitAndHideNoticeAsync().Forget();
        }

        private async UniTaskVoid WaitAndHideNoticeAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(NOTICE_DURATION),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            if (_noticePopup != null)
                _noticePopup.SetActive(false);
        }

        /// <summary>
        /// 시너지 팝업. SynergyScrollContent는 외부에서 동적 생성 후 전달.
        /// onConfirm 콜백으로 닫힘 시점을 전달.
        /// </summary>
        public void ShowSynergyPopup(Action onConfirm = null)
        {
            if (_synergyPopup == null) return;

            _synergyPopup.SetActive(true);

            _synergyConfirmButton.OnClickAsObservable()
                .Take(1)
                .Subscribe(_ =>
                {
                    _synergyPopup.SetActive(false);
                    onConfirm?.Invoke();
                })
                .AddTo(_synergyPopup);
        }

        public Transform GetSynergyScrollContent() => _synergyScrollContent;

        public void HideAll()
        {
            if (_confirmPopup     != null) _confirmPopup.SetActive(false);
            if (_fireConfirmPopup != null) _fireConfirmPopup.SetActive(false);
            if (_alertPopup       != null) _alertPopup.SetActive(false);
            if (_noticePopup      != null) _noticePopup.SetActive(false);
            if (_synergyPopup     != null) _synergyPopup.SetActive(false);
        }
    }
}