// using DG.Tweening;             // TODO: DoTween 패키지 설치 후 주석 해제
// using Cysharp.Threading.Tasks; // TODO: UniTask 패키지 설치 후 주석 해제
// using R3;                      // TODO: R3 패키지 설치 후 주석 해제

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Title
{
    public sealed class TitleSceneUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _exitButton;

        [Header("Load Panel")]
        [SerializeField] private GameObject    _loadPanel;
        [SerializeField] private Button        _loadPanelCloseButton;
        [SerializeField] private LoadSlotView[] _slots;

        [Header("Title Logo")]
        [SerializeField] private CanvasGroup _logoGroup;

        public event Action<int> OnSlotSelected;

        private void Awake()
        {
            _loadPanel.SetActive(false);

            // [DoTween 설치 후 교체]
            // _loadPanel.transform.localScale = Vector3.zero;
        }

        private void Start()
        {
            BindButtons();
            BindSlots();
            StartCoroutine(PlayLogoEntrance());

            Debug.LogWarning("[TitleSceneUI] DoTween / R3 / UniTask 미설치 상태 - 임시 코드 사용 중");
        }

        private void OnDestroy()
        {
            // [DoTween 설치 후 주석 해제]
            // DOTween.Kill(this);
            // DOTween.Kill(_loadPanel.transform);
            // DOTween.Kill(_logoGroup);
        }

        private void BindButtons()
        {
            // [R3 설치 후 아래 코드로 교체]
            // _startButton.OnClickAsObservable().Subscribe(_ => ShowLoadPanel()).AddTo(this);
            // _exitButton.OnClickAsObservable().Subscribe(_ => OnExitClicked()).AddTo(this);
            // _loadPanelCloseButton.OnClickAsObservable().Subscribe(_ => HideLoadPanel()).AddTo(this);

            _startButton.onClick.AddListener(ShowLoadPanel);
            _exitButton.onClick.AddListener(OnExitClicked);
            _loadPanelCloseButton.onClick.AddListener(HideLoadPanel);
        }

        private void BindSlots()
        {
            foreach (var slot in _slots)
            {
                slot.OnSelected += index =>
                {
                    HideLoadPanel();
                    OnSlotSelected?.Invoke(index);
                };
            }
        }

        public void SetSlotData(SaveSlotData[] data)
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Bind(i < data.Length ? data[i] : null);
        }

        private void ShowLoadPanel()
        {
            // [DoTween 설치 후 아래 코드로 교체]
            // DOTween.Kill(_loadPanel.transform);
            // _loadPanel.SetActive(true);
            // _loadPanel.transform.localScale = Vector3.zero;
            // _loadPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetTarget(this);
            // for (int i = 0; i < _slots.Length; i++)
            //     _slots[i].PlayEntrance(delaySeconds: i * 0.08f);

            _loadPanel.SetActive(true);
        }

        private void HideLoadPanel()
        {
            // [DoTween 설치 후 아래 코드로 교체]
            // DOTween.Kill(_loadPanel.transform);
            // _loadPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
            //     .SetTarget(this).OnComplete(() => _loadPanel.SetActive(false));

            _loadPanel.SetActive(false);
        }

        private IEnumerator PlayLogoEntrance()
        {
            // [DoTween + UniTask 설치 후 아래 코드로 교체]
            // _logoGroup.alpha = 0f;
            // await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: destroyCancellationToken);
            // await _logoGroup.DOFade(1f, 0.8f).ToUniTask(cancellationToken: destroyCancellationToken);

            _logoGroup.alpha = 0f;
            yield return new WaitForSeconds(0.5f);
            _logoGroup.alpha = 1f;
        }

        private static void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}