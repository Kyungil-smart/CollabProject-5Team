using System.Collections.Generic;
using UnityEngine;
using static ManagementStatusData;

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
        RegisterAchievement(new AchievementData { id = "Gold_01", title = "돈이 복사가 된다고?"    , description = "골드 500k 모으기", targetEvent = AchievementNotifyType.OnGoldChanged,  targetValue = 500000 });
        RegisterAchievement(new AchievementData { id = "Gold_02", title = "내 주머니에 종이가 백장", description = "골드 1M 모으기", targetEvent = AchievementNotifyType.OnGoldChanged, targetValue = 1000000 });

        // 1-2. 총 골드
        RegisterAchievement(new AchievementData { id = "TotalGold_01", title = "돈의 맛"  , description = "총 매출 1M", targetEvent = AchievementNotifyType.OnTotalGoldChanged, targetValue = 1000000 });
        RegisterAchievement(new AchievementData { id = "TotalGold_02", title = "돈 돈 돈!", description = "총 매출 5M", targetEvent = AchievementNotifyType.OnTotalGoldChanged, targetValue = 5000000 });

        // 2. 회사 레벨 
        RegisterAchievement(new AchievementData { id = "Office_02", title = "좋좋소"              , description = "중형 회사로 증축", targetEvent = AchievementNotifyType.OnLevelChanged, targetValue = 2 });
        RegisterAchievement(new AchievementData { id = "Office_03", title = "우리도 이제 머기업???", description = "대형 회사로 증축", targetEvent = AchievementNotifyType.OnLevelChanged, targetValue = 3 });

        // 3. 플레이타임
        RegisterAchievement(new AchievementData { id = "Time_01", title = "엉덩이가 무거운 개발자", description = "10분 동안 개발하기", targetEvent = AchievementNotifyType.OnPlayTimeChanged, targetValue = 600 });
        RegisterAchievement(new AchievementData { id = "Time_02", title = "개발은 잘 되가나요?"   , description = "1시간 동안 개발하기", targetEvent = AchievementNotifyType.OnPlayTimeChanged, targetValue = 3600 });
        RegisterAchievement(new AchievementData { id = "Time_03", title = "컨펌 해주세요~"        , description = "3시간 동안 개발하기", targetEvent = AchievementNotifyType.OnPlayTimeChanged, targetValue = 21600 });
        RegisterAchievement(new AchievementData { id = "Time_04", title = "보너스는 없나요??"     , description = "6시간 동안 개발하기", targetEvent = AchievementNotifyType.OnPlayTimeChanged, targetValue = 21600 });

        // 4-1. 고용
        RegisterAchievement(new AchievementData { id = "Hire_01", title = "저희 회사에 어서오세요", description = "4명 고용하기" , targetEvent = AchievementNotifyType.OnHireEmployeeChanged, targetValue = 4 });
        RegisterAchievement(new AchievementData { id = "Hire_02", title = "음음 내 노예들"       , description = "7명 고용하기" , targetEvent = AchievementNotifyType.OnHireEmployeeChanged, targetValue = 7 });
        RegisterAchievement(new AchievementData { id = "Hire_03", title = "입이 많아졌네"        , description = "10명 고용하기", targetEvent = AchievementNotifyType.OnHireEmployeeChanged, targetValue = 10 });
        
        // 4-2. 해고
        RegisterAchievement(new AchievementData { id = "Fire_01", title = "미안하게 됐다."    , description = "1명 해고하기"  , targetEvent = AchievementNotifyType.OnFireEmployeeChanged, targetValue = 1 });
        RegisterAchievement(new AchievementData { id = "Fire_02", title = "혹독한 사회"      , description = "5명 해고하기"  , targetEvent = AchievementNotifyType.OnFireEmployeeChanged, targetValue = 5 });
        RegisterAchievement(new AchievementData { id = "Fire_03", title = "악덕 사장"        , description = "10명 해고하기", targetEvent = AchievementNotifyType.OnFireEmployeeChanged, targetValue = 10 });
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

            if (eventType == AchievementNotifyType.OnHireEmployeeChanged ||
            eventType == AchievementNotifyType.OnFireEmployeeChanged)
            {
                ach.currentValue += value;
            }
            else
            {
                ach.currentValue = value;
            }

            if (ach.currentValue >= ach.targetValue)
            {
                ach.isUnlocked = true;
                _achievementUI.ShowAchievement(ach.title, ach.description);
            }
        }
    }

    /////////////////////////// 저장 ///////////////////////////

    public void ExportAchievementData(SaveData data)
    {
        data._achievementStates.Clear();

        foreach (var list in _achievementRegistry.Values)
        {
            foreach (var ach in list)
            {
                data._achievementStates.Add(new AchievementSaveInfo
                {
                    id = ach.id,
                    currentValue = ach.currentValue,
                    isUnlocked = ach.isUnlocked
                });
            }
        }
    }

    public void ImportAchievementData(SaveData data)
    {
        foreach (var savedAch in data._achievementStates)
        {
            foreach (var list in _achievementRegistry.Values)
            {
                var ach = list.Find(x => x.id == savedAch.id);
                if (ach != null)
                {
                    ach.currentValue = savedAch.currentValue;
                    ach.isUnlocked = savedAch.isUnlocked;
                    break;
                }
            }
        }
    }
}