using DG.Tweening;
using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
    /// <summary>
    /// 타이틀 씬 UI 담당.
    /// 버튼 이벤트 발행 및 패널 Show/Hide.
    /// 씬 전환 및 데이터 처리는 외부에 위임.
    /// </summary>
    public sealed class TitleSceneUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _exitButton;

        [Header("Load Panel")]
        [SerializeField] private GameObject _loadPanel;
        [SerializeField] private Button     _loadPanelCloseButton;

        public event Action      OnStartClicked;
        public event Action      OnExitClicked;
        public event Action<int> OnSlotSelected;

        private void Awake()
        {
            _loadPanel.SetActive(false);
        }

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            _startButton.OnClickAsObservable()
                .Subscribe(_ => ShowLoadPanel())
                .AddTo(this);

            _exitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    OnExitClicked?.Invoke();
                    QuitGame();
                })
                .AddTo(this);

            _loadPanelCloseButton.OnClickAsObservable()
                .Subscribe(_ => HideLoadPanel())
                .AddTo(this);
        }

        public void NotifySlotSelected(int slotIndex)
        {
            HideLoadPanel();
            OnSlotSelected?.Invoke(slotIndex);
        }

        public void ShowLoadPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            _loadPanel.SetActive(true);
            OnStartClicked?.Invoke();
        }

        public void HideLoadPanel()
        {
            // [DoTween 설치 후 팝업 연출 추가]
            _loadPanel.SetActive(false);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}