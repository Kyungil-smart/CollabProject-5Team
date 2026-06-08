using R3;
using UnityEngine;
using GameDevTycoon.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_DayBottom Presenter.
    /// 진척도 표시 갱신, 저장/퇴근 버튼 처리.
    /// ProjectProgressItemView 동적 생성 및 바인딩 담당.
    /// </summary>
    public sealed class DayBottomPresenter : MonoBehaviour
    {
        [SerializeField] private DayBottomView _view;
        [SerializeField] private AlertView _alertView;

        [Header("ProjectProgressItemView 프리팹")]
        [SerializeField] private GameObject _progressItemPrefab;

        private void Start()
        {
            BindData();
            BindButtons();
            RefreshProgressItems();

            DateTimeManager.OnDay += Show;
            DateTimeManager.OnWorkCompleted += OnWorkCompleted;
            DateTimeManager.OnNightLoading += Hide;
        }

        private void OnDestroy()
        {
            DateTimeManager.OnDay -= Show;
            DateTimeManager.OnWorkCompleted -= OnWorkCompleted;
            DateTimeManager.OnNightLoading -= Hide;
        }

        private void BindData()
        {
            // 낮 진입 시 퇴근 버튼 비활성 — 업무 완료 후 활성화는 OnWorkCompleted()로 처리
            _view.SetDayQuitInteractable(false);

            // 진척도 높은 순 정렬 후 표시
            RefreshProgressItems();
        }

        private void BindButtons()
        {
            _view.OnSaveClicked
                .Subscribe(_ => OnSaveClicked())
                .AddTo(this);

            _view.OnDayQuitClicked
                .Subscribe(_ =>
                {
                    DateTimeManager.Instance.OnClickEndDayButton();
                    // _view.SetDayQuitInteractable(false); 임시로 버그 안고침
                }).AddTo(this);
        }

        /// <summary>
        /// 낮 시작 또는 진척도 갱신 시점에 외부에서 호출.
        /// </summary>
        public void RefreshProgressItems()
        {
            var projects = Company.Instance.projects;

            _view.SetProjectInfoVisible(projects.Count > 0);

            // 기존 아이템 제거
            foreach (Transform child in _view.ProjectListContent)
                Destroy(child.gameObject);

            if (projects.Count == 0) return;

            // 진척도 높은 순 정렬 (동률 시 day 적은 순 — 먼저 시작한 프로젝트)
            projects.Sort((a, b) =>
            {
                int cmp = b.ProgressDayBar.CompareTo(a.ProgressDayBar);
                return cmp != 0 ? cmp : a.day.CompareTo(b.day);
            });

            foreach (var project in projects)
            {
                var item = Instantiate(_progressItemPrefab, _view.ProjectListContent);
                // [TODO: ProjectProgressItemView IBindable 연결 후 Bind 호출]
                // item.GetComponent<IBindable<Project>>().Bind(project);
            }
        }

        /// <summary>
        /// 업무 완료 시 외부(퀘스트 시스템)에서 호출.
        /// </summary>
        public void OnWorkCompleted()
        {
            _view.SetDayQuitInteractable(true);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnSaveClicked()
        {
            _alertView.ShowConfirmPopup("저장하시겠습니까?", () =>
            {
                // [TODO: SaveSystem 연결]
                Debug.Log("[DayBottomPresenter] 저장 처리");
            });
        }
    }
}