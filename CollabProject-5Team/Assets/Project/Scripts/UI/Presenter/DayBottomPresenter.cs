using R3;
using UnityEngine;
using GameDevTycoon.UI;
using Cysharp.Threading.Tasks;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_DayBottom Presenter.
    /// 진척도 표시 갱신, 저장/퇴근 버튼 처리.
    /// 진행 중인 프로젝트 1개 기준으로 직접 바인딩.
    /// </summary>
    public sealed class DayBottomPresenter : MonoBehaviour
    {
        [SerializeField] private DayBottomView _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private SavePresenter _savePresenter;

        private void Start()
        {
            BindData();
            BindButtons();

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
        }

        private void BindButtons()
        {
            _view.OnSaveClicked
                .Subscribe(_ => _savePresenter.Show())
                .AddTo(this);

            _view.OnDayQuitClicked
                .Subscribe(_ =>
                {
                    DateTimeManager.Instance.OnClickEndDayButton().Forget();
                    _view.SetDayQuitInteractable(false);
                }).AddTo(this);
        }

        /// <summary>
        /// 낮 시작 또는 진척도 갱신 시점에 외부에서 호출.
        /// </summary>
        public void RefreshProgressItems()
        {
            var company = Company.Instance;
            bool hasProjects = company.activeProjectCount.Value > 0;

            _view.SetProjectInfoVisible(hasProjects);
            if (!hasProjects) return;

            _view.SetProjectProgress(company.curProject.userNamed.Value, company.curProject.ProgressDayBar);
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
            _view.SetDayQuitInteractable(false);
            RefreshProgressItems();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}