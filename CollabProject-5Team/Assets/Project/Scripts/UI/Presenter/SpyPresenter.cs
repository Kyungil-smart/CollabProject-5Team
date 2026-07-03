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

        [Header("확인 버튼 스프라이트 (규칙 5번)")]
        [SerializeField] private Sprite _confirmDefaultGraySprite;
        [SerializeField] private Sprite _confirmActiveRedSprite;

        [Header("알림 팝업 연결")]
        [SerializeField] private AlertView _alertView;

        private readonly CompositeDisposable _disposables = new();
        private SpyView _selectedView;

        // 판정 연출이 시작된 순간부터 알림창이 뜨기 전까지 유저가 다른 카드를 누르거나 
        // 확인 버튼을 무한 연타하여 정답 판정 로직이 중복 실행되는 상태이상(Race Condition)을 방지하는 플래그입니다.
        private bool _isProcessing;

        private void Awake()
        {
            foreach (SpyView spyView in _spyViews)
            {
                spyView.OnClickAsObservable
                    .Where(_ => !_isProcessing) // 연출 처리 중에는 카드 선택 입력을 원천 차단합니다.
                    .Subscribe(employee => OnCardClicked(spyView))
                    .AddTo(_disposables);
            }

            _confirmButton.OnClickAsObservable()
                .Where(_ => !_isProcessing && _selectedView != null)
                .Subscribe(_ => OnConfirmClicked())
                .AddTo(_disposables);

            UpdateButtonState(false);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        public void Open(List<Employee> employees)
        {
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
            // 팝업이 뜨는 순간 True로 만들어 Canvas_Spy 내부의 모든 카드 클릭 및 확인 버튼 상호작용을 막습니다.
            _isProcessing = true;

            if (_alertView == null)
            {
                Debug.LogWarning("[SpyPresenter] AlertView가 연결되지 않아 즉시 판정을 진행합니다.");
                _selectedView.SetStamped(true);
                ExecuteVerification();
                return;
            }

            // 하이어라키 기반으로 제작한 스파이 확정 전용 팝업 호출
            _alertView.ShowSpyConfirmPopup(
                onConfirm: () =>
                {
                    // 확인을 누르면 그제서야 도장을 찍고 최종 판정을 보냅니다.
                    _selectedView.SetStamped(true);
                    ExecuteVerification();
                },
                onCancel: () =>
                {
                    // 취소를 누르면 플래그를 풀어 상호작용을 다시 허용합니다. (Canvas_Spy 복귀)
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
        /// 알림창 오픈 매개체 (미완성 영역 연결 고리)
        /// </summary>
        private void ShowResultNotification(bool isCorrect, Employee target)
        {
            if (isCorrect)
            {
                Debug.Log($"[SpySystem] 정답 성공 판정: {target.so.Name} 검거 완료.");
                // TODO: 성공 후속 처리 또는 결과 팝업 연계 후 ClosePopup() 호출
            }
            else
            {
                Debug.Log($"[SpySystem] 오답 실패 판정: {target.so.Name}은 일반 직원입니다.");
                // TODO: 실패 후속 처리 또는 결과 팝업 연계 후 ClosePopup() 호출
            }
        }

        public void ClosePopup()
        {
            _popupFrame.SetActive(false);
        }
    }
}