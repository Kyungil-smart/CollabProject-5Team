using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Quest 담당 View.
    /// QuestDetailPopup / DailyQuestAlertPopup / QuestClearPopup 세 팝업 제어.
    /// 퀘스트 아이템 동적 생성 위치(Content Transform) 및 버튼 이벤트 발행.
    /// 팝업 내 데이터 바인딩과 QuestItemView 생성은 Presenter에서 담당.
    /// </summary>
    public sealed class QuestView : MonoBehaviour
    {
        [Header("QuestDetailPopup")]
        [SerializeField] private GameObject _questDetailPopup;
        [SerializeField] private Transform _dailyQuestContent;
        [SerializeField] private Transform _storyQuestContent;
        [SerializeField] private Transform _eventQuestContent;
        [SerializeField] private TextMeshProUGUI _storyQuestTitleLabel;
        [SerializeField] private TextMeshProUGUI _eventQuestTitleLabel;
        [SerializeField] private Button _questDetailCloseButton;

        [Header("DailyQuestAlertPopup")]
        [SerializeField] private GameObject _dailyQuestAlertPopup;
        [SerializeField] private TextMeshProUGUI _alertQuestTypeLabel;
        [SerializeField] private TextMeshProUGUI _alertQuestNameLabel;
        [SerializeField] private TextMeshProUGUI _alertProgressLabel;
        [SerializeField] private Button _alertConfirmButton;

        [Header("QuestClearPopup")]
        [SerializeField] private GameObject _questClearPopup;
        [SerializeField] private GameObject _completionUpIcon;
        [SerializeField] private GameObject _stabilityUpIcon;
        [SerializeField] private GameObject _appealUpIcon;

        [Header("QuestClearPopupGold")]
        [SerializeField] private GameObject _questClearPopupGold;
        [SerializeField] private TextMeshProUGUI _goldRewardLabel;

        // QuestDetailPopup 이벤트
        public Observable<Unit> OnQuestDetailCloseClicked => _questDetailCloseButton.OnClickAsObservable();

        // DailyQuestAlertPopup 이벤트
        public Observable<Unit> OnAlertConfirmClicked => _alertConfirmButton.OnClickAsObservable();

        // Content Transform
        public Transform DailyQuestContent => _dailyQuestContent;
        public Transform StoryQuestContent => _storyQuestContent;
        public Transform EventQuestContent => _eventQuestContent;

        public bool IsDetailVisible => _questDetailPopup.activeSelf;

        private void Awake()
        {
            if (_alertQuestTypeLabel == null && _dailyQuestAlertPopup != null)
                _alertQuestTypeLabel = FindDeepChild(_dailyQuestAlertPopup.transform, "TitleLabel")?.GetComponent<TextMeshProUGUI>();

            _questDetailPopup.SetActive(false);
            _dailyQuestAlertPopup.SetActive(false);
            _questClearPopup.SetActive(false);
            _questClearPopupGold.SetActive(false);
        }

        // QuestDetailPopup
        public void ShowDetail() => _questDetailPopup.SetActive(true);
        public void HideDetail() => _questDetailPopup.SetActive(false);

        public void SetStoryQuestTitle(int count)
            => _storyQuestTitleLabel.text = $"스토리 퀘스트 ({count})";

        public void SetEventQuestTitle(int count)
            => _eventQuestTitleLabel.text = $"이벤트 퀘스트 ({count})";

        // DailyQuestAlertPopup
        public void ShowDailyQuestAlert(string questName, int current, int total)
            => ShowQuestAlert("일일 퀘스트", questName, current, total);

        public void ShowQuestAlert(string questType, string questName, int current, int total)
        {
            if (_alertQuestTypeLabel != null)
                _alertQuestTypeLabel.text = questType;

            _alertQuestNameLabel.text = questName;
            _alertProgressLabel.text = $"{current} / {total}";
            _dailyQuestAlertPopup.SetActive(true);
        }

        public void HideDailyQuestAlert() => _dailyQuestAlertPopup.SetActive(false);

        public void SetAlertProgress(int current, int total)
            => _alertProgressLabel.text = $"{current} / {total}";

        // QuestClearPopup — UP 아이콘은 직군 연관 여부에 따라 선택적 활성
        public void ShowClearPopup(bool completionUp, bool stabilityUp, bool appealUp)
        {
            _completionUpIcon.SetActive(completionUp);
            _stabilityUpIcon.SetActive(stabilityUp);
            _appealUpIcon.SetActive(appealUp);
            _questClearPopup.SetActive(true);
        }
        // 골드가 있는 퀘스트 클리어 팝업은 이거
        public void ShowGoldClearPopup(int goldAmount)
        {
            _goldRewardLabel.text = $"+ {goldAmount:N0}";

            _questClearPopupGold.SetActive(true);
        }

        public void HideClearPopup()
        {
            _questClearPopup.SetActive(false);
            _questClearPopupGold.SetActive(false);
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
