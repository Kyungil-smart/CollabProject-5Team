using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_HUD 담당 View.
    /// TopBar 수치 표시, DayUI/NightUI 전환, 버튼 이벤트 발행.
    /// 낮/밤 전환 및 수치 갱신은 외부(Presenter)에서 호출.
    /// SettingsPanel 제어는 HUDPresenter에서 SettingsPresenter를 통해 처리.
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        [Header("TopBar")]
        [SerializeField] private TextMeshProUGUI _timeLabel;
        [SerializeField] private TextMeshProUGUI _moneyLabel;

        [Header("TopBar — 낮/밤 아이콘")]
        [SerializeField] private Image _timeIcon;
        [SerializeField] private Sprite _dayTimeIconSprite;
        [SerializeField] private Sprite _nightTimeIconSprite;

        [Header("Buttons")]
        [SerializeField] private Button _settingsButton;

        [Header("DayUI")]
        public GameObject _dayUI;
        [SerializeField] private Button _questIconButton;
        [SerializeField] private Button _workStartButton;

        [Header("DayUI — QuestBanner")]
        [SerializeField] private GameObject _questBanner;
        [SerializeField] private TextMeshProUGUI _questTypeLabel;
        [SerializeField] private TextMeshProUGUI _questNameLabel;
        [SerializeField] private TextMeshProUGUI _questProgressLabel;

        [Header("NightUI")]
        [SerializeField] private GameObject _nightUI;

        [Header("NightUI — Buttons")]
        [SerializeField] private Button _hrButton;
        [SerializeField] private Button _projectButton;
        [SerializeField] private Button _companyButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _nightQuitButton;

        [Header("NightUI — Icons")]
        [SerializeField] private RectTransform _hrIcon;
        [SerializeField] private RectTransform _projectIcon;
        [SerializeField] private RectTransform _companyIcon;
        [SerializeField] private RectTransform _saveIcon;
        [SerializeField] private RectTransform _nightQuitIcon;

        [Header("NightUI — Labels")]
        [SerializeField] private TextMeshProUGUI _hrLabel;
        [SerializeField] private TextMeshProUGUI _projectLabel;
        [SerializeField] private TextMeshProUGUI _companyLabel;
        [SerializeField] private TextMeshProUGUI _saveLabel;
        [SerializeField] private TextMeshProUGUI _nightQuitLabel;

        [Header("NightUI — Sprites")]
        [SerializeField] private Sprite _nightButtonActiveSprite;

        [Header("NightUI — Label Colors")]
        [SerializeField] private Color _activeLabelColor = new Color(0.337f, 0.247f, 0.063f);  // #563F10
        [SerializeField] private Color _inactiveLabelColor = new Color(0.624f, 0.471f, 0.157f);  // #9F7828

        private static readonly float ButtonHeightDefault = 220f;
        private static readonly float ButtonHeightSelected = 260f;
        private static readonly float IconPosYDefault = 20f;
        private static readonly float IconPosYSelected = 80f;
        private static readonly float LabelPosYDefault = -85f;
        private static readonly float LabelPosYSelected = -50f;
        private static readonly float LabelFontSizeDefault = 36f;
        private static readonly float LabelFontSizeSelected = 50f;
        private static readonly float AnimDuration = 0.2f;

        private RectTransform[] _nightButtonRects;
        private RectTransform[] _nightIconRects;
        private RectTransform[] _nightLabelRects;
        private TextMeshProUGUI[] _nightLabels;
        private Image[] _nightButtonImages;
        private Sprite[] _nightButtonDefaultSprites;

        private Tweener[] _buttonTweeners;
        private Tweener[] _iconTweeners;
        private Tweener[] _labelPosTweeners;
        private Tweener[] _labelFontTweeners;

        private int _selectedIndex = -1;

        public Observable<Unit> OnSettingsClicked => _settingsButton.OnClickAsObservable();
        public Observable<Unit> OnQuestIconClicked => _questIconButton.OnClickAsObservable();
        public Observable<Unit> OnWorkStartClicked => _workStartButton.OnClickAsObservable();
        public Observable<Unit> OnHRClicked => _hrButton.OnClickAsObservable();
        public Observable<Unit> OnProjectClicked => _projectButton.OnClickAsObservable();
        public Observable<Unit> OnCompanyClicked => _companyButton.OnClickAsObservable();
        public Observable<Unit> OnSaveClicked => _saveButton.OnClickAsObservable();
        public Observable<Unit> OnNightQuitClicked => _nightQuitButton.OnClickAsObservable();

        private void Awake()
        {
            _nightButtonRects = new RectTransform[]
            {
                _hrButton.GetComponent<RectTransform>(),
                _projectButton.GetComponent<RectTransform>(),
                _companyButton.GetComponent<RectTransform>(),
                _saveButton.GetComponent<RectTransform>(),
                _nightQuitButton.GetComponent<RectTransform>(),
            };

            _nightIconRects = new RectTransform[]
            {
                _hrIcon, _projectIcon, _companyIcon, _saveIcon, _nightQuitIcon
            };

            _nightLabels = new TextMeshProUGUI[]
            {
                _hrLabel, _projectLabel, _companyLabel, _saveLabel, _nightQuitLabel
            };

            _nightLabelRects = new RectTransform[]
            {
                _hrLabel.GetComponent<RectTransform>(),
                _projectLabel.GetComponent<RectTransform>(),
                _companyLabel.GetComponent<RectTransform>(),
                _saveLabel.GetComponent<RectTransform>(),
                _nightQuitLabel.GetComponent<RectTransform>(),
            };

            _nightButtonImages = new Image[]
            {
                _hrButton.GetComponent<Image>(),
                _projectButton.GetComponent<Image>(),
                _companyButton.GetComponent<Image>(),
                _saveButton.GetComponent<Image>(),
                _nightQuitButton.GetComponent<Image>(),
            };

            // 버튼 Image에 이미 적용된 비활성화 스프라이트를 초기값으로 캐싱
            _nightButtonDefaultSprites = new Sprite[_nightButtonImages.Length];
            for (int i = 0; i < _nightButtonImages.Length; i++)
                _nightButtonDefaultSprites[i] = _nightButtonImages[i].sprite;

            _buttonTweeners = new Tweener[_nightButtonRects.Length];
            _iconTweeners = new Tweener[_nightIconRects.Length];
            _labelPosTweeners = new Tweener[_nightLabels.Length];
            _labelFontTweeners = new Tweener[_nightLabels.Length];

            _dayUI.SetActive(true);
            _nightUI.SetActive(false);
            _nightQuitButton.interactable = false;

            if (_questTypeLabel == null && _questBanner != null)
                _questTypeLabel = FindDeepChild(_questBanner.transform, "QuestTypeLabel")?.GetComponent<TextMeshProUGUI>();

            _questBanner.SetActive(false);
        }

        // [TODO: DateTimeManager에 year/month 데이터 추가 후 파라미터 확정]
        public void SetTimeLabel(string timeText)
        {
            _timeLabel.text = timeText;
        }

        public void SetMoneyLabel(int money)
        {
            _moneyLabel.text = $"{money:N0}G";
        }

        public void SetTimeIcon(bool isDay)
        {
            if (_timeIcon == null) return;

            Sprite sprite = isDay ? _dayTimeIconSprite : _nightTimeIconSprite;
            if (sprite != null)
                _timeIcon.sprite = sprite;
        }

        public void SetWorkStartActive(bool active)
        {
            _workStartButton.gameObject.SetActive(active);
        }

        public void SetNightQuitInteractable(int activeProjectCount)
        {
            _nightQuitButton.interactable = activeProjectCount > 0;
        }

        public void SwitchToDay()
        {
            // [DoTween 페이드 연출 추가 예정]
            _nightUI.SetActive(false);
            _dayUI.SetActive(true);
        }

        public void SwitchToNight()
        {
            // [DoTween 페이드 연출 추가 예정]
            _dayUI.SetActive(false);
            _nightUI.SetActive(true);
            DeselectAllNightButtons();
        }

        /// <summary>
        /// 해당 인덱스 버튼을 선택 상태로, 나머지는 비선택으로 전환.
        /// 동일 인덱스 재호출 시 토글(비선택).
        /// </summary>
        public void SelectNightButton(int index)
        {
            bool deselecting = _selectedIndex == index;
            DeselectAllNightButtons();

            if (!deselecting)
            {
                _selectedIndex = index;
                AnimateButton(index, selected: true);
            }
        }

        public void DeselectAllNightButtons()
        {
            for (int i = 0; i < _nightButtonRects.Length; i++)
                AnimateButton(i, selected: false);

            _selectedIndex = -1;
        }

        public void ShowQuestBanner(string questName, int current, int total)
            => ShowQuestBanner("일일 퀘스트", questName, current, total);

        public void ShowQuestBanner(string questType, string questName, int current, int total)
        {
            if (_questTypeLabel != null)
                _questTypeLabel.text = questType;

            _questNameLabel.text = questName;
            _questProgressLabel.text = $"{current}/{total}";

            var group = _questBanner.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = 1f;

            _questBanner.SetActive(true);
        }

        public void HideQuestBanner() => _questBanner.SetActive(false);

        public void SetQuestBannerProgress(int current, int total)
        {
            _questProgressLabel.text = $"{current}/{total}";
        }

        // 퀘스트 완료 시 반투명 처리
        public void SetQuestBannerCompleted()
        {
            var group = _questBanner.GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = 0.5f;
        }

        private void AnimateButton(int index, bool selected)
        {
            float targetHeight = selected ? ButtonHeightSelected : ButtonHeightDefault;
            float targetIconY = selected ? IconPosYSelected : IconPosYDefault;
            float targetLabelY = selected ? LabelPosYSelected : LabelPosYDefault;
            float targetFontSize = selected ? LabelFontSizeSelected : LabelFontSizeDefault;
            Color targetColor = selected ? _activeLabelColor : _inactiveLabelColor;
            Sprite targetSprite = selected ? _nightButtonActiveSprite : _nightButtonDefaultSprites[index];

            var rect = _nightButtonRects[index];
            var icon = _nightIconRects[index];
            var labelRect = _nightLabelRects[index];
            var label = _nightLabels[index];

            _buttonTweeners[index]?.Kill();
            _iconTweeners[index]?.Kill();
            _labelPosTweeners[index]?.Kill();
            _labelFontTweeners[index]?.Kill();

            _buttonTweeners[index] = rect
                .DOSizeDelta(new Vector2(rect.sizeDelta.x, targetHeight), AnimDuration)
                .SetEase(Ease.OutQuad);

            _iconTweeners[index] = icon
                .DOAnchorPosY(targetIconY, AnimDuration)
                .SetEase(Ease.OutQuad);

            _labelPosTweeners[index] = labelRect
                .DOAnchorPosY(targetLabelY, AnimDuration)
                .SetEase(Ease.OutQuad);

            // DOTween Pro 없이 font size 트윈 — float 값을 직접 보간
            float currentSize = label.fontSize;
            _labelFontTweeners[index] = DOTween.To(
                () => label.fontSize,
                x => label.fontSize = x,
                targetFontSize,
                AnimDuration
            ).SetEase(Ease.OutQuad);

            label.color = targetColor;
            _nightButtonImages[index].sprite = targetSprite;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }
    }
}
