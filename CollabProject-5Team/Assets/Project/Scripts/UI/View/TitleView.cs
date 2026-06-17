using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 담당 View.
    /// 버튼 이벤트 발행. 씬 전환 및 팝업 처리는 Presenter에 위임.
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        public Observable<Unit> OnStartClicked => _startButton.OnClickAsObservable();
        public Observable<Unit> OnLoadClicked => _loadButton.OnClickAsObservable();
        public Observable<Unit> OnSettingsClicked => _settingsButton.OnClickAsObservable();
        public Observable<Unit> OnQuitClicked => _quitButton.OnClickAsObservable();
    }
}