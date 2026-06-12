using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
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
        [SerializeField] private HUDView _view;
        [SerializeField] private AlertView _alertView;
        [SerializeField] private SettingsPresenter _settingsPresenter;
        [SerializeField] private SavePresenter _savePresenter;

        [Header("외부 연결")]
        [SerializeField] private HRPresenter _hrPresenter;
        [SerializeField] private ProjectPresenter _projectPresenter;
        //[SerializeField] private ~Presenter _(IBottomNightUI)Presenter; 추후 IBottomNightUI가 추가로 존재하면 연결

        [Header("업무 시작 시 이동할 데스크탑 프리팹")]
        [SerializeField] private DeskInteract _desk;

        private void Start()
        {
            BindButtons();
            DateTimeManager.OnReportEnd += SwitchToNight;
        }

        private void OnDestroy()
        {
            DateTimeManager.OnReportEnd -= SwitchToNight;
        }

        public void SwitchToNight()
        {
            CloseAllBottomPopups();  // HR, Project 닫기
            _settingsPresenter.Hide();  // 세팅도 같이 닫기
            _view.SwitchToNight();
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
                .Subscribe(_ => _savePresenter.Show())
                .AddTo(this);

            _view.OnNightQuitClicked
                .Subscribe(_ => OnNightQuitClicked())
                .AddTo(this);
            
            _view.OnSettingsClicked
                .Subscribe(_ => _settingsPresenter.Show())
                .AddTo(this);

            Company.Instance.activeProjectCount
                .Subscribe(count => _view.SetNightQuitInteractable(count))
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

        private void OnWorkStartClicked()
        {
            // 참조한 플레이어 책상의 데스크탑으로 이동하도록 수정함
            if (_desk != null)
            {
                _desk.OnClickWorkButton();
            }

            // DateTimeManager.Instance.CompleteDayWork();
            // WorkStartBubble은 업무 시작 후 비활성화 — View에서 직접 처리하거나 Presenter에서 호출
            // [TODO: WorkStartBubble 비활성화 메서드 HUDView에 추가 후 연결]
            // ★~퀘스트 구현 전에 임시로 그냥 임무 완료되게 처리중~☆
        }

        private void OnHRClicked()
        {
            ToggleBottomPopup(_hrPresenter);
        }

        private void OnProjectClicked()
        {
            ToggleBottomPopup(_projectPresenter);
        }

        private void OnCompanyClicked()
        {
            //_companyPresenter.Show();
        }

        private void OnNightQuitClicked()
        {
            CloseAllBottomPopups();
            _view.SwitchToDay();

            DateTimeManager.Instance.OnClickEndDayButton();
        }

        private void ToggleBottomPopup(IBottomNightUI targetPresenter)
        {
            bool wasVisible = targetPresenter.IsVisible;

            CloseAllBottomPopups();

            if (!wasVisible)
            {
                targetPresenter.Show();
            }
        }

        private void CloseAllBottomPopups()
        {
            foreach (var presenter in GetBottomPopupPresenters())
            {
                presenter.Hide();
            }
        }

        private IEnumerable<IBottomNightUI> GetBottomPopupPresenters()
        {
            var yielded = new HashSet<IBottomNightUI>();

            if (yielded.Add(_hrPresenter))
            {
                yield return _hrPresenter;
            }

            if (yielded.Add(_projectPresenter))
            {
                yield return _projectPresenter;
            }
            //추후 IBottomNightUI가 추가로 존재하면 여기에 추가
        }
    }
}