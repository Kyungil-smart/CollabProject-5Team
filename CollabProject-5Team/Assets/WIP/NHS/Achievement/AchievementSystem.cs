using System.Collections.Generic;
using UnityEngine;

public class AchievementSystem : MonoBehaviour
{
    public static AchievementSystem Instance { get; private set; }

    [SerializeField] private AchievementUI _achievementUI;
    private Dictionary<AchievementNotifyType, List<AchievementData>> _achievementRegistry = new Dictionary<AchievementNotifyType, List<AchievementData>>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        InitializeRegistry();
        LoadAchievementData();
    }

    private void InitializeRegistry()
    {
        foreach(AchievementNotifyType type in System.Enum.GetValues(typeof(AchievementNotifyType)))
        {
            _achievementRegistry[type] = new List<AchievementData>();
        }
    }

    private void LoadAchievementData()
    {
        // 1. 골드
        RegisterAchievement(new AchievementData { id = "Gold_01", title = "돈이 복사가 된다고!"    , description = "골드 5만 모으기", targetEvent = AchievementNotifyType.OnGoldChanged,  targetValue = 50000 });
        RegisterAchievement(new AchievementData { id = "Gold_02", title = "내 주머니에 종이가 백장", description = "골드 10만 모으기", targetEvent = AchievementNotifyType.OnGoldChanged, targetValue = 100000 });

        // 2. 회사 레벨 
        RegisterAchievement(new AchievementData { id = "Office_02", title = "좋좋소"              , description = "중형 회사로 증축", targetEvent = AchievementNotifyType.OnLevelChanged, targetValue = 2 });
        RegisterAchievement(new AchievementData { id = "Office_03", title = "우리도 이제 머기업???", description = "대형 회사로 증축", targetEvent = AchievementNotifyType.OnLevelChanged, targetValue = 3 });

        // 3. 플레이타임
        RegisterAchievement(new AchievementData { id = "Time_01", title = "엉덩이가 무거운 개발자", description = "10분 동안 개발하기", targetEvent = AchievementNotifyType.OnPlayTimeChanged, targetValue = 600 });
    }

    private void RegisterAchievement(AchievementData data)
    {
        _achievementRegistry[data.targetEvent].Add(data);
    }

    public void NotifyEvent(AchievementNotifyType eventType, int value)
    {
        if (!_achievementRegistry.ContainsKey(eventType)) return;

        List<AchievementData> targetedAchievements = _achievementRegistry[eventType];

        for (int i = 0; i < targetedAchievements.Count; i++)
        {
            AchievementData ach = targetedAchievements[i];

            if (ach.isUnlocked) continue;

            ach.currentValue = value;

            if (ach.currentValue >= ach.targetValue)
            {
                ach.isUnlocked = true;
                _achievementUI.ShowAchievement(ach.title, ach.description);
            }
        }
    }
}