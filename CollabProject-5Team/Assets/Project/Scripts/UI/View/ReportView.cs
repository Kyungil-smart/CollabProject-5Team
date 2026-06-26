using DG.Tweening;
using R3;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Report 담당 View.
    /// Panel_Cover, EmployeeStatusSlide, Panel_ReportEnd 직접 제어.
    /// Panel_EmployeeComment, Panel_ReportReview, Panel_ReportDetail, Panel_PersonalOpinion은
    /// 담당자 스크립트에 위임 — Show/Hide 제어만 담당.
    /// </summary>
    public sealed class ReportView : MonoBehaviour
    {
        [Header("Canvas_Report")]
        [SerializeField] private GameObject _canvasReport;

        [Header("Panel_Cover")]
        [SerializeField] private GameObject _panelCover;
        [SerializeField] private TextMeshProUGUI _coverDateRangeLabel;
        [SerializeField] private TextMeshProUGUI _coverCompanyNameLabel;
        [SerializeField] private Button _coverNextPageButton;

        [Header("EmployeeStatusSlide")]
        [SerializeField] private GameObject _employeeStatusSlide;
        [SerializeField] private RectTransform _employeeStatusSlideRect;
        [SerializeField] private Image _slideBackgroundImage;
        [SerializeField] private Sprite _slideBackgroundActive;
        [SerializeField] private Sprite _slideBackgroundInactive;
        [SerializeField] private Button _slideToggleButton;
        [SerializeField] private Image _slideToggleIcon;
        [SerializeField] private Transform _slidePreviewContent;
        [SerializeField] private Sprite _slideIconExpanded;   // 슬라이드 올라가 있을 때
        [SerializeField] private Sprite _slideIconCollapsed;  // 슬라이드 닫혀있을 때
        [SerializeField] private Sprite _slideIconDisabled;   // 비활성화 상태

        [Header("Panel_EmployeeComment")]
        [SerializeField] private GameObject _panelEmployeeComment;

        [Header("Panel_ReportReview_Planner")]
        [SerializeField] private GameObject _panelReportReviewPlanner;
        [SerializeField] private ReportCardView[] _plannerCards;    // 고정 3슬롯
        [SerializeField] private GameObject[] _plannerDividers; // 2개: [0]=1~2번 사이, [1]=2~3번 사이

        [Header("Panel_ReportReview_Artist")]
        [SerializeField] private GameObject _panelReportReviewArtist;
        [SerializeField] private ReportCardView[] _artistCards;
        [SerializeField] private GameObject[] _artistDividers;

        [Header("Panel_ReportReview_Programmer")]
        [SerializeField] private GameObject _panelReportReviewProgrammer;
        [SerializeField] private ReportCardView[] _programmerCards;
        [SerializeField] private GameObject[] _programmerDividers;

        [Header("Panel_ReportDetail — 담당자 위임")]
        [SerializeField] private GameObject _panelReportDetail;
        [SerializeField] private Image _profileIcon;
        [SerializeField] TMP_Text _detailTitleLable;
        [SerializeField] DepartmentTagView _departmentTagPrefab;
        [SerializeField] TMP_Text _detailEmployeeNameLable;
        [SerializeField] TMP_Text _detailContentLable;
        [SerializeField] Button _adoptBtn;
        [SerializeField] Button _cancelBtn;

        [Header("Panel_PersonalOpinion - 추후 작업")]
        [SerializeField] private GameObject _panelPersonalOpinion;

        [Header("Panel_ReportEnd")]
        [SerializeField] private GameObject _panelReportEnd;
        [SerializeField] private Button _reportEndConfirmButton;

        // [DoTween 수치 확정 후 조정]
        [Header("Slide Animation")]
        [SerializeField] private float _slideHiddenY = -200f;
        [SerializeField] private float _slideShownY = 0f;
        private const float SLIDE_DURATION = 0.3f;

        private bool _isSlideExpanded;
        private Tween _activeSlideTween;

        public Observable<Unit> OnCoverNextPageClicked => _coverNextPageButton.OnClickAsObservable();
        public Observable<Unit> OnReportEndConfirmClicked => _reportEndConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnAdoptClicked => _adoptBtn.OnClickAsObservable();
        public Observable<Unit> OnCancelClicked => _cancelBtn.OnClickAsObservable();

        // 담당자 패널 Show/Hide용 — Presenter에서 순서 제어
        public GameObject PanelEmployeeComment => _panelEmployeeComment;
        public GameObject PanelReportDetail => _panelReportDetail;
        public GameObject PanelPersonalOpinion => _panelPersonalOpinion;

        public Transform SlidePreviewContent => _slidePreviewContent;

        public ReportCardView[] GetCards(int roleIndex) => roleIndex switch
        {
            0 => _plannerCards,
            1 => _artistCards,
            2 => _programmerCards,
            _ => null,
        };

        private void Awake()
        {
            _panelEmployeeComment.SetActive(false);
            _panelReportReviewPlanner.SetActive(false);
            _panelReportReviewArtist.SetActive(false);
            _panelReportReviewProgrammer.SetActive(false);
            _panelReportDetail.SetActive(false);
            _panelPersonalOpinion.SetActive(false);
            _panelReportEnd.SetActive(false);

            _employeeStatusSlide.SetActive(false);
            _isSlideExpanded = false;
            _employeeStatusSlideRect.anchoredPosition =
                new Vector2(_employeeStatusSlideRect.anchoredPosition.x, _slideHiddenY);

            _slideToggleButton.OnClickAsObservable()
                .Subscribe(_ => ToggleSlide())
                .AddTo(this);
        }

        public void Show()
        {
            _canvasReport.SetActive(true);
            ShowPanel(ReportPanel.Cover);
        }

        public void Hide()
        {
            // [DoTween Bottom Sheet 아웃 연출 추가 예정]
            //_canvasReport.SetActive(false); // 현재 이 오브젝트가 비활성화 되면 두번(3주차)이후 보고서가 뜨지 않는 버그가 있음.
        }

        public void ShowPanel(ReportPanel panel)
        {
            _panelCover.SetActive(panel == ReportPanel.Cover);
            _panelEmployeeComment.SetActive(panel == ReportPanel.EmployeeComment);
            _panelReportReviewPlanner.SetActive(panel == ReportPanel.ReportReviewPlanner);
            _panelReportReviewArtist.SetActive(panel == ReportPanel.ReportReviewArtist);
            _panelReportReviewProgrammer.SetActive(panel == ReportPanel.ReportReviewProgrammer);
            _panelReportDetail.SetActive(panel == ReportPanel.ReportDetail);
            _panelPersonalOpinion.SetActive(panel == ReportPanel.PersonalOpinion);
            _panelReportEnd.SetActive(panel == ReportPanel.ReportEnd);
        }

        /// <summary>
        /// 슬라이드 상호작용 가능 여부 — 배경 스프라이트 및 토글 아이콘 상태 반영.
        /// </summary>
        public void SetSlideInteractable(bool interactable)
        {
            Debug.Log($"[ReportView] SetSlideInteractable: {interactable}, Button: {_slideToggleButton != null}");

            if (_slideBackgroundImage != null)
                _slideBackgroundImage.sprite = interactable ? _slideBackgroundActive : _slideBackgroundInactive;

            _slideToggleButton.interactable = interactable;
            Debug.Log($"[ReportView] Button.interactable after set: {_slideToggleButton.interactable}");

            if (_slideToggleIcon != null && _slideIconDisabled != null && _slideIconCollapsed != null)
                _slideToggleIcon.sprite = interactable ? _slideIconCollapsed : _slideIconDisabled;

            // 비활성화 시 슬라이드 닫힘 상태로 초기화
            if (!interactable && _isSlideExpanded)
            {
                _isSlideExpanded = false;
                _activeSlideTween?.Kill();
                _employeeStatusSlideRect.anchoredPosition =
                    new Vector2(_employeeStatusSlideRect.anchoredPosition.x, _slideHiddenY);
            }
        }

        /// <summary>
        /// 보고서 진입 여부에 따라 슬라이드 패널 노출 제어.
        /// </summary>
        public void SetSlideVisible(bool visible)
        {
            _employeeStatusSlide.SetActive(visible);
        }

        /// <summary>
        /// 직군별 카드/구분선 바인딩. 보고서 수 초과 슬롯은 비활성.
        /// </summary>
        public void BindReviewCards(int roleIndex, List<Report> reports, Action<Report> onCardClicked)
        {
            var cards = GetCards(roleIndex);
            var dividers = GetDividers(roleIndex);

            for (int i = 0; i < cards.Length; i++)
            {
                bool active = i < reports.Count;
                cards[i].gameObject.SetActive(active);

                if (active)
                {
                    cards[i].Bind(reports[i]);
                    int captured = i;
                    cards[i].OnCardClicked
                        .Subscribe(r => onCardClicked(r))
                        .AddTo(cards[captured]);
                }
            }

            // 카드 사이 구분선 — 앞 카드와 뒷 카드 모두 활성일 때만 표시
            for (int i = 0; i < dividers.Length; i++)
                dividers[i].SetActive(i + 1 < reports.Count);
        }

        public void SetDetailInfo(Report report)
        {
            _detailTitleLable.text = report.so.title;
            _detailEmployeeNameLable.text = report.owner.so.Name;
            _detailContentLable.text = report.so.content;

            _profileIcon.sprite = report.owner.so.iconNormal;

            _departmentTagPrefab.Bind(report.role);
        }

        public void SetCoverInfo(string dateRange, string companyName)
        {
            _coverDateRangeLabel.text = dateRange;
            _coverCompanyNameLabel.text = companyName;
        }

        private GameObject[] GetDividers(int roleIndex) => roleIndex switch
        {
            0 => _plannerDividers,
            1 => _artistDividers,
            2 => _programmerDividers,
            _ => null,
        };

        private void ToggleSlide()
        {
            Debug.Log($"[ReportView] ToggleSlide called, Button.interactable: {_slideToggleButton.interactable}");

            _isSlideExpanded = !_isSlideExpanded;

            _activeSlideTween?.Kill();

            float targetY = _isSlideExpanded ? _slideShownY : _slideHiddenY;

            _activeSlideTween = _employeeStatusSlideRect
                .DOAnchorPosY(targetY, SLIDE_DURATION)
                .SetEase(Ease.OutQuart);

            if (_slideToggleIcon != null && _slideIconExpanded != null && _slideIconCollapsed != null)
                _slideToggleIcon.sprite = _isSlideExpanded ? _slideIconExpanded : _slideIconCollapsed;
        }

        private void OnDestroy()
        {
            _activeSlideTween?.Kill();
        }
    }

    public enum ReportPanel
    {
        Cover,
        EmployeeComment,
        ReportReviewPlanner,
        ReportReviewArtist,
        ReportReviewProgrammer,
        ReportDetail,
        PersonalOpinion,
        ReportEnd
    }
}