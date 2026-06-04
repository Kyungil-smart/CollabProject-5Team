using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 담당 View.
    /// 버튼 이벤트 발행 및 LoadPanel Show/Hide.
    /// 씬 전환 및 데이터 처리는 Presenter에 위임.
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _settingsButton;

        [Header("LoadPanel")]
        [SerializeField] private GameObject _loadPanel;
        [SerializeField] private Button     _loadPanelCloseButton;
        [SerializeField] private Transform  _slotContent;

        public Observable<Unit> OnStartClicked       => _startButton.OnClickAsObservable();
        public Observable<Unit> OnSettingsClicked    => _settingsButton.OnClickAsObservable();
        public Observable<Unit> OnLoadPanelCloseClicked => _loadPanelCloseButton.OnClickAsObservable();

        public Transform SlotContent => _slotContent;

        private void Awake()
        {
            _loadPanel.SetActive(false);

            _loadPanelCloseButton.OnClickAsObservable()
                .Subscribe(_ => HideLoadPanel())
                .AddTo(this);
        }

        public void ShowLoadPanel()
        {
            _loadPanel.SetActive(true);
            // [DoTween 팝업 연출 추가 예정]
        }

        public void HideLoadPanel()
        {
            // [DoTween 팝업 연출 추가 예정]
            _loadPanel.SetActive(false);
        }
    }
}