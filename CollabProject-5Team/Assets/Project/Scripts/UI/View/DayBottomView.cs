using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_DayBottom 담당 View.
    /// ProjectProgressFrame 토글 연출, 진척도 표시, 저장/퇴근 버튼 이벤트 발행.
    /// ProjectProgressItemView 동적 생성은 Presenter에서 담당.
    /// </summary>
    public sealed class DayBottomView : MonoBehaviour
    {
        [Header("ProjectProgressFrame")]
        [SerializeField] private RectTransform _progressFrameRect;
        [SerializeField] private Button        _projectProgressButton;
        [SerializeField] private GameObject    _expandedArea;

        [Header("EmptyGroup / ProjectInfoGroup")]
        [SerializeField] private GameObject    _emptyGroup;
        [SerializeField] private GameObject    _projectInfoGroup;

        [Header("ExpandedArea")]
        [SerializeField] private Transform     _projectListContent;

        [Header("Buttons")]
        [SerializeField] private Button        _saveButton;
        [SerializeField] private Button        _dayQuitButton;

        // 기획 확정 후 수치 조정
        [Header("Frame Height")]
        [SerializeField] private float _collapsedHeight = 120f;
        [SerializeField] private float _expandedHeight  = 400f;

        private const float TWEEN_DURATION = 0.3f;

        private bool   _isExpanded;
        private Tween  _activeTween;

        public Observable<Unit> OnSaveClicked    => _saveButton.OnClickAsObservable();
        public Observable<Unit> OnDayQuitClicked => _dayQuitButton.OnClickAsObservable();

        public Transform ProjectListContent => _projectListContent;

        private void Awake()
        {
            _dayQuitButton.interactable = false;

            _expandedArea.SetActive(false);
            _emptyGroup.SetActive(true);
            _projectInfoGroup.SetActive(false);

            _projectProgressButton.OnClickAsObservable()
                .Subscribe(_ => ToggleProgressFrame())
                .AddTo(this);
        }

        private void ToggleProgressFrame()
        {
            _isExpanded = !_isExpanded;

            _activeTween?.Kill();

            float targetHeight = _isExpanded ? _expandedHeight : _collapsedHeight;

            _activeTween = _progressFrameRect
                .DOSizeDelta(new Vector2(_progressFrameRect.sizeDelta.x, targetHeight), TWEEN_DURATION)
                .SetEase(Ease.OutQuart)
                .OnStart(() =>
                {
                    if (_isExpanded) _expandedArea.SetActive(true);
                })
                .OnComplete(() =>
                {
                    if (!_isExpanded) _expandedArea.SetActive(false);
                });
        }

        /// <summary>
        /// 진행 중인 프로젝트 유무에 따라 EmptyGroup / ProjectInfoGroup 전환.
        /// </summary>
        public void SetProjectInfoVisible(bool hasProject)
        {
            _emptyGroup.SetActive(!hasProject);
            _projectInfoGroup.SetActive(hasProject);
        }

        public void SetDayQuitInteractable(bool interactable)
        {
            _dayQuitButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
        }
    }
}