using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// 직원 모집 직군별 슬라이더 행 프리팹 바인딩.
    /// Panel_Recruit.JobSliders 하위에 배리언트 5개 고정 배치.
    /// 직군 버튼 클릭 시 슬라이더 활성화, 슬라이더 변경 시 인원수/비용 외부 전달.
    /// </summary>
    public sealed class JobSliderGroupView : MonoBehaviour
    {
        [Header("직군 버튼")]
        [SerializeField] private Button _jobButton;

        [Header("모집인원 슬라이더")]
        [SerializeField] private Slider _countSlider;

        [Header("잠금 오버레이")]
        [SerializeField] private GameObject _lockOverlay;

        [Header("직군 정보")]
        [SerializeField] private Role _role;

        private bool _isSelected;

        // 인원수 변경 시 외부로 전달 — (role, count)
        public Observable<(Role role, int count)> OnCountChanged { get; private set; }

        public Role Role => _role;
        public int Count => _isSelected ? Mathf.RoundToInt(_countSlider.value) : 0;
        public bool IsSelected => _isSelected;
        public bool IsLocked => _lockOverlay != null && _lockOverlay.activeSelf;

        public event System.Action OnSelectedChanged;

        private void Awake()
        {
            _countSlider.wholeNumbers = true;
            _countSlider.minValue = 1;
            _countSlider.maxValue = 5;
            _countSlider.value = 1;
            _countSlider.interactable = false;

            OnCountChanged = _countSlider.OnValueChangedAsObservable()
                .Select(v => (_role, Mathf.RoundToInt(v)));

            _jobButton.OnClickAsObservable()
                .Subscribe(_ => ToggleSelected())
                .AddTo(this);
        }

        /// <summary>
        /// 회사 레벨 기준으로 잠금 여부 설정. HRPresenter에서 초기화 시 호출.
        /// </summary>
        public void Setup(bool isLocked)
        {
            _lockOverlay.SetActive(isLocked);
            _jobButton.interactable = !isLocked;
            _countSlider.interactable = false;

            if (isLocked)
            {
                _isSelected = false;
                _countSlider.value = 1;
            }
        }

        public void ResetSelection()
        {
            _isSelected = false;
            _countSlider.interactable = false;
            _countSlider.value = 1;
            RefreshJobButtonVisual();
        }

        private void ToggleSelected()
        {
            _isSelected = !_isSelected;
            _countSlider.interactable = _isSelected;

            if (!_isSelected)
                _countSlider.value = 1;

            RefreshJobButtonVisual();

            OnSelectedChanged?.Invoke();
        }

        // 선택 상태 시각 피드백 — 실제 스프라이트 교체는 Inspector 배리언트에서 처리
        private void RefreshJobButtonVisual()
        {
            var colors = _jobButton.colors;
            colors.normalColor = _isSelected ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            _jobButton.colors = colors;
        }
    }
}