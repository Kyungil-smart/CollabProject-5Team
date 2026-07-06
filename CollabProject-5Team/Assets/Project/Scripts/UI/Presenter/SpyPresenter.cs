using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Spy 팝업 컨트롤.
    /// 상위 매니저가 Open(employees) 호출로 진입, 스파이 지목 및 확인 처리.
    /// </summary>
    public sealed class SpyPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popupFrame;
        [SerializeField] private List<SpyView> _spyViews;
        [SerializeField] private Button _confirmButton;

        [Header("확인 버튼 스프라이트")]
        [SerializeField] private Sprite _confirmDefaultGraySprite;
        [SerializeField] private Sprite _confirmActiveRedSprite;

        [Header("알림 팝업 연결")]
        [SerializeField] private AlertView _alertView;

        private readonly CompositeDisposable _disposables = new();
        private SpyView _selectedView;
        private Action _onSpySelectCompleted;

        // 판정 연출이 시작된 순간부터 알림창이 뜨기 전까지 유저가 다른 카드를 누르거나 
        // 확인 버튼을 무한 연타하여 정답 판정 로직이 중복 실행되는 상태이상(Race Condition)을 방지하는 플래그입니다.
        private bool _isProcessing;

        private void Awake()
        {
            foreach (SpyView spyView in _spyViews)
            {
                spyView.OnClickAsObservable
                    .Where(_ => !_isProcessing)
                    .Subscribe(employee => OnCardClicked(spyView))
                    .AddTo(_disposables);
            }

            _confirmButton.OnClickAsObservable()
                .Where(_ => !_isProcessing && _selectedView != null)
                .Subscribe(_ => OnConfirmClicked())
                .AddTo(_disposables);

            UpdateButtonState(false);
        }

        private void Start()
        {
            StoryQuestManager.Instance.OnSpySelect += Open;
        }

        private void OnDestroy()
        {
            StoryQuestManager.Instance.OnSpySelect -= Open;
            _disposables.Dispose();
        }

        public void Open(List<Employee> employees, Action onComplete = null)
        {
            _onSpySelectCompleted = onComplete;
            _selectedView = null;
            _isProcessing = false;
            UpdateButtonState(false);

            for (int i = 0; i < _spyViews.Count; i++)
            {
                _spyViews[i].Bind(employees[i]);
            }

            _popupFrame.SetActive(true);
        }

        private void OnCardClicked(SpyView clickedView)
        {
            if (_selectedView == clickedView) return;

            AudioManager.Instance?.PlaySFXClick();
            _selectedView?.SetSelected(false);
            clickedView.SetSelected(true);
            _selectedView = clickedView;

            UpdateButtonState(true);
        }

        private void OnConfirmClicked()
        {
            if (_selectedView == null || _isProcessing) return;

            AudioManager.Instance?.PlaySFXPositive();
            _isProcessing = true;

            if (_alertView == null)
            {
                Debug.LogWarning("[SpyPresenter] AlertView가 연결되지 않아 즉시 판정을 진행합니다.");
                _selectedView.SetStamped(true);
                ExecuteVerification();
                return;
            }

            _alertView.ShowSpyConfirmPopup(
                onConfirm: () =>
                {
                    _selectedView.SetStamped(true);
                    ExecuteVerification();
                },
                onCancel: () =>
                {
                    _isProcessing = false;
                }
            );
        }

        private void ExecuteVerification()
        {
            Employee selectedEmployee = _selectedView.BoundEmployee;
            bool isCorrect = StoryQuestManager.Instance.CheckIsSpy(selectedEmployee);

            ShowResultNotification(isCorrect, selectedEmployee);
        }

        /// <summary>
        /// 확인 버튼의 활성화/비활성화 및 이미지 스위칭 처리를 제어합니다.
        /// </summary>
        private void UpdateButtonState(bool interactable)
        {
            _confirmButton.interactable = interactable;

            if (_confirmButton.image != null)
            {
                _confirmButton.image.sprite = interactable ? _confirmActiveRedSprite : _confirmDefaultGraySprite;
            }
        }

        /// <summary>
        /// 알림창 오픈 매개체 및 스파이 결과 연출 링커
        /// </summary>
        private void ShowResultNotification(bool isCorrect, Employee target)
        {
            if (isCorrect)
            {
                Debug.Log($"[SpySystem] 정답 성공 판정: {target.so.Name} 검거 완료.");

                target.isSpy = false;
                StoryQuestManager.Instance.isCorrectSpySelected = true;

                ClosePopup();

                _alertView.ShowSpySuccessResult(onClose: () =>
                {
                    _isProcessing = false;

                    _onSpySelectCompleted?.Invoke();
                    _onSpySelectCompleted = null;
                });
            }
            else
            {
                Debug.Log($"[SpySystem] 오답 실패 판정: {target.so.Name} 선택.");

                StoryQuestManager.Instance.isCorrectSpySelected = false;

                ClosePopup();
                _isProcessing = false;

                _onSpySelectCompleted?.Invoke();
                _onSpySelectCompleted = null;
            }
        }

        public void ClosePopup()
        {
            _popupFrame.SetActive(false);
        }
    }
}