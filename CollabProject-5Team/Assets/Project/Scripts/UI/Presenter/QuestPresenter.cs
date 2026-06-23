using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameDevTycoon.UI.Ingame
{
    /// <summary>
    /// Canvas_Quest Presenter.
    /// QuestView 이벤트 구독, 퀘스트 데이터 기반 QuestItemView 동적 생성.
    /// QuestItemView는 Addressables 로드 + 재사용(비활성화) 방식으로 관리.
    /// QuestClearPopup 2초 자동 닫힘은 UniTask로 처리.
    /// 퀘스트 시스템 Manager 미확정 항목은 [TODO]로 표기.
    /// </summary>
    public sealed class QuestPresenter : MonoBehaviour
    {
        [SerializeField] private QuestView _view;

        private const string QUEST_ITEM_ADDRESS = "QuestItemView";

        // 섹션별 재사용 풀 — 비활성 오브젝트를 먼저 꺼내 쓰고 부족하면 추가 생성
        private readonly List<QuestItemView> _dailyPool = new();
        private readonly List<QuestItemView> _storyPool = new();
        private readonly List<QuestItemView> _eventPool = new();

        private AsyncOperationHandle<GameObject> _prefabHandle;
        private GameObject _questItemPrefab;
        private bool _isReady;

        private void Start()
        {
            BindButtons();
            LoadPrefabAsync().Forget();
            BindQuestManager();
        }

        private void OnDestroy()
        {
            if (_prefabHandle.IsValid())
                Addressables.Release(_prefabHandle);
        }

        private void BindButtons()
        {
            _view.OnQuestDetailCloseClicked
                .Subscribe(_ => _view.HideDetail())
                .AddTo(this);

            _view.OnAlertConfirmClicked
                .Subscribe(_ => _view.HideDailyQuestAlert())
                .AddTo(this);
        }

        // QuestManager 상태/진행도 변화 구독
        private void BindQuestManager()
        {
            QuestManager.Instance.dailyQuestState
                .Subscribe(OnDailyQuestStateChanged)
                .AddTo(this);

            QuestManager.Instance.dailyQuestProgress
                .Subscribe(progress =>
                {
                    DailyQuest quest = QuestManager.Instance.curDailyQuest;
                    if (quest == null) return;

                    UpdateDailyQuestProgress(progress, quest.TargetCount);
                })
                .AddTo(this);
        }

        private void OnDailyQuestStateChanged(QuestState state)
        {
            DailyQuest quest = QuestManager.Instance.curDailyQuest;
            if (quest == null) return;

            switch (state)
            {
                case QuestState.Playing:
                    ShowDailyQuestAlert();
                    break;

                case QuestState.End:
                    if (quest.result == QuestResult.Success)
                        ShowClearPopupForCurrentQuest();
                    break;
            }
        }

        // 완료한 퀘스트의 직군에 맞는 스탯 UP 아이콘만 표시 (Project.cs의 직군별 스탯 매핑과 동일)
        private void ShowClearPopupForCurrentQuest()
        {
            Role role = QuestManager.Instance.curDailyQuest.so.role;

            ShowClearPopup(
                completionUp: role == Role.PLANNER,
                stabilityUp: role == Role.PROGRAMMER,
                appealUp: role == Role.ARTIST
            );
        }

        private async UniTaskVoid LoadPrefabAsync()
        {
            _prefabHandle = Addressables.LoadAssetAsync<GameObject>(QUEST_ITEM_ADDRESS);
            await _prefabHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

            _questItemPrefab = _prefabHandle.Result;
            _isReady = true;
        }

        // HUDPresenter(클립보드 버튼) 또는 외부에서 호출
        public async UniTaskVoid ShowDetailAsync()
        {
            if (!_isReady)
                await UniTask.WaitUntil(() => _isReady, cancellationToken: this.GetCancellationTokenOnDestroy());

            RefreshQuestDetail();
            _view.ShowDetail();
        }

        public void HideDetail() => _view.HideDetail();
        public bool IsDetailVisible => _view.IsDetailVisible;

        /// <summary>
        /// 일일 퀘스트 시작(Ready→Playing 전환) 시 자동 호출.
        /// </summary>
        public void ShowDailyQuestAlert()
        {
            DailyQuest quest = QuestManager.Instance.curDailyQuest;
            if (quest == null) return;

            _view.ShowDailyQuestAlert(
                questName: quest.so.Name,
                current: quest.curCount,
                total: quest.TargetCount
            );
        }

        /// <summary>
        /// 퀘스트 완료 시 외부에서 호출.
        /// completionUp/stabilityUp/appealUp은 해당 직군 퀘스트 여부로 결정.
        /// </summary>
        public void ShowClearPopup(bool completionUp, bool stabilityUp, bool appealUp)
        {
            _view.ShowClearPopup(completionUp, stabilityUp, appealUp);
            AutoHideClearPopupAsync().Forget();
        }

        public void UpdateDailyQuestProgress(int current, int total)
        {
            _view.SetAlertProgress(current, total);
        }

        private void RefreshQuestDetail()
        {
            var dailyQuests = GetDailyQuests();
            var storyQuests = GetStoryQuests();
            var eventQuests = GetEventQuests();

            BindSection(_view.DailyQuestContent, _dailyPool, dailyQuests);

            _view.SetStoryQuestTitle(storyQuests.Count);
            BindSection(_view.StoryQuestContent, _storyPool, storyQuests);

            _view.SetEventQuestTitle(eventQuests.Count);
            BindSection(_view.EventQuestContent, _eventPool, eventQuests);
        }

        // 재사용: 기존 아이템은 Bind 재호출, 부족하면 추가 생성, 남으면 비활성화
        private void BindSection(Transform content, List<QuestItemView> pool, List<QuestItemData> dataList)
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                QuestItemView item;
                if (i < pool.Count)
                {
                    item = pool[i];
                    item.gameObject.SetActive(true);
                }
                else
                {
                    item = Object.Instantiate(_questItemPrefab, content).GetComponent<QuestItemView>();
                    pool.Add(item);
                }
                item.Bind(dataList[i].questName, dataList[i].isCompleted);
            }

            for (int i = dataList.Count; i < pool.Count; i++)
                pool[i].gameObject.SetActive(false);
        }

        // 2초 후 자동 닫힘
        private async UniTaskVoid AutoHideClearPopupAsync()
        {
            await UniTask.Delay(2000, cancellationToken: this.GetCancellationTokenOnDestroy());
            _view.HideClearPopup();
        }

        // 일일 퀘스트는 하루에 하나만 존재
        private static List<QuestItemData> GetDailyQuests()
        {
            DailyQuest quest = QuestManager.Instance.curDailyQuest;
            if (quest == null) return new();

            return new()
            {
                new QuestItemData
                {
                    questName = quest.so.Name,
                    isCompleted = quest.state == QuestState.End
                }
            };
        }

        // [TODO: 스토리/이벤트 퀘스트 시스템 구현 후 연결]
        private static List<QuestItemData> GetStoryQuests() => new();
        private static List<QuestItemData> GetEventQuests() => new();
    }

    public sealed class QuestItemData
    {
        public string questName;
        public bool isCompleted;
    }
}