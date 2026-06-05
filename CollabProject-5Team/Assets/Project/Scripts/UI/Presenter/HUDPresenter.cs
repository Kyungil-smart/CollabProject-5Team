using R3;
using System;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_HUD Presenter.
    /// DateTimeManager, Company의 ReactiveProperty를 구독해 HUDView에 반영.
    /// 버튼 이벤트를 받아 게임 로직으로 전달.
    /// </summary>
    public sealed class HUDPresenter : MonoBehaviour
    {
        [SerializeField] private HUDView   _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private SettingsPresenter _settingsPresenter;

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            var dtm = DateTimeManager.Instance;

            _view.OnWorkStartClicked
                .Subscribe(_ => OnWorkStartClicked())
                .AddTo(this);

            _view.OnHRClicked
                .Subscribe(_ => OnHRClicked())
                .AddTo(this);

            _view.OnProjectClicked
                .Subscribe(_ => OnProjectClicked())
                .AddTo(this);

            _view.OnCompanyClicked
                .Subscribe(_ => OnCompanyClicked())
                .AddTo(this);

            _view.OnSaveClicked
                .Subscribe(_ => OnSaveClicked())
                .AddTo(this);

            _view.OnNightQuitClicked
                .Subscribe(_ => OnNightQuitClicked())
                .AddTo(this);
            
            _view.OnSettingsClicked
                .Subscribe(_ => _settingsPresenter.Show())
                .AddTo(this);
        }


        private void RefreshMoneyLabel()
        {
            _view.SetMoneyLabel(Company.Instance.gold);
        }

        private void RefreshReputationLabel()
        {
            _view.SetReputationLabel(Company.Instance.reputation);
        }

        /// <summary>
        /// Company.gold / reputation 변경 시점에 외부에서 호출.
        /// ReactiveProperty 전환 전까지 사용.
        /// </summary>
        public void RefreshHUD()
        {
            RefreshMoneyLabel();
            RefreshReputationLabel();
        }

        public void SetNightQuitInteractable(bool interactable)
        {
            _view.SetNightQuitInteractable(interactable);
        }


        private void OnWorkStartClicked()
        {
            DateTimeManager.Instance.CompleteDayWork();
            // WorkStartBubble은 업무 시작 후 비활성화 — View에서 직접 처리하거나 Presenter에서 호출
            // [TODO: WorkStartBubble 비활성화 메서드 HUDView에 추가 후 연결]
            // ★~퀘스트 구현 전에 임시로 그냥 임무 완료되게 처리중~☆
        }

        private void OnHRClicked()
        {
            // [TODO: HRPresenter 연결 후 HRView.Show() 호출]
        }

        private void OnProjectClicked()
        {
            // [TODO: ProjectPresenter 연결 후 ProjectView.Show() 호출]
        }

        private void OnCompanyClicked()
        {
            // [TODO: CompanyPresenter 연결 후 CompanyView.Show() 호출]
        }

        private void OnSaveClicked()
        {
            _alertView.ShowConfirmPopup("저장하시겠습니까?", () =>
            {
                // [TODO: SaveSystem 연결]
            });
        }

        private void OnNightQuitClicked()
        {
            DateTimeManager.Instance.OnClickEndDayButton();
            _view.SwitchToDay();
        }
    }
}