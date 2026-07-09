using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Popup.ProjectCompletedPopup 담당 View.
    /// 프로젝트 완료 시 등급, 점수 표시 및 확인 버튼 이벤트 발행.
    /// </summary>
    public sealed class ProjectCompletedPopupView : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject _popup;

        [Header("PopupFrame")]
        [SerializeField] private TextMeshProUGUI _celebrationLabel;
        [SerializeField] private TextMeshProUGUI _projectResultLabel;

        [Header("ScoreItem_Completion")]
        [SerializeField] private Slider _completionBar;
        [SerializeField] private TextMeshProUGUI _completionValueLabel;

        [Header("ScoreItem_Stability")]
        [SerializeField] private Slider _stabilityBar;
        [SerializeField] private TextMeshProUGUI _stabilityValueLabel;

        [Header("ScoreItem_Appeal")]
        [SerializeField] private Slider _appealBar;
        [SerializeField] private TextMeshProUGUI _appealValueLabel;

        [Header("ConfirmButton")]
        [SerializeField] private Button _confirmButton;

        public Observable<Unit> OnConfirmClicked => _confirmButton.OnClickAsObservable();

        private void Awake()
        {
            _popup.SetActive(false);
        }

        public void Show() => _popup.SetActive(true);
        public void Hide() => _popup.SetActive(false);
        public bool IsVisible => _popup.activeSelf;

        /// <summary>
        /// 완료 팝업에 프로젝트 결과 데이터를 바인딩.
        /// score 값은 0~100 범위. 바 너비는 1점당 2px 기준으로 Slider value에 반영.
        /// </summary>
        public void Bind(string projectName, string grade, int completion, int stability, int appeal)
        {
            _projectResultLabel.text = $"{projectName}이(가) {grade}으로 제작 완료되었습니다.";

            SetScoreBar(_completionBar, _completionValueLabel, completion);
            SetScoreBar(_stabilityBar, _stabilityValueLabel, stability);
            SetScoreBar(_appealBar, _appealValueLabel, appeal);
        }

        private static void SetScoreBar(Slider bar, TextMeshProUGUI label, int score)
        {
            bar.value = score;
            label.text = score.ToString();
        }
    }
}