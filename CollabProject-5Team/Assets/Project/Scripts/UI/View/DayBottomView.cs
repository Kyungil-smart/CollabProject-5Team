using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_DayBottom 담당 View.
    /// 진척도 표시, 저장/퇴근 버튼 이벤트 발행.
    /// 진행 중인 프로젝트 1개 기준으로 직접 바인딩. 동적 생성 없음.
    /// </summary>
    public sealed class DayBottomView : MonoBehaviour
    {
        [Header("EmptyGroup / ProjectInfoGroup")]
        [SerializeField] private GameObject _emptyGroup;
        [SerializeField] private GameObject _projectInfoGroup;

        [Header("ProjectInfoGroup")]
        [SerializeField] private TextMeshProUGUI _projectNameLabel;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TextMeshProUGUI _progressValueLabel;

        [Header("Buttons")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _dayQuitButton;

        [Header("퇴근 버튼 뒤 배경 이미지")]
        [SerializeField] private Image _dayQuitBackgroundImage;
        [SerializeField] private Sprite _dayQuitActiveSprite;
        [SerializeField] private Sprite _dayQuitInactiveSprite;

        public Observable<Unit> OnSaveClicked => _saveButton.OnClickAsObservable();
        public Observable<Unit> OnDayQuitClicked => _dayQuitButton.OnClickAsObservable();

        private void Awake()
        {
            SetDayQuitInteractable(false);
            _emptyGroup.SetActive(true);
            _projectInfoGroup.SetActive(false);
        }

        /// <summary>
        /// 진행 중인 프로젝트 유무에 따라 EmptyGroup / ProjectInfoGroup 전환.
        /// </summary>
        public void SetProjectInfoVisible(bool hasProject)
        {
            _emptyGroup.SetActive(!hasProject);
            _projectInfoGroup.SetActive(hasProject);
        }

        public void SetProjectProgress(string projectName, float progressRate)
        {
            _projectNameLabel.text = projectName;
            _progressSlider.value = progressRate;
            _progressValueLabel.text = $"{progressRate:F0}%";
        }

        public void SetDayQuitInteractable(bool interactable)
        {
            _dayQuitButton.interactable = interactable;

            Sprite sprite = interactable ? _dayQuitActiveSprite : _dayQuitInactiveSprite;
            if (_dayQuitBackgroundImage != null && sprite != null)
                _dayQuitBackgroundImage.sprite = sprite;
        }
    }
}