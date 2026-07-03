using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

public class AchievementSystem : MonoBehaviour
{
    public static AchievementSystem Instance { get; private set; }

    [SerializeField] private AchievementUI _achievementUI;
    public List<Achievement> achievements = new List<Achievement>();
    private CompositeDisposable _disposables = new CompositeDisposable();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeAchievements();
        BindDataObservations();
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void InitializeAchievements()
   {
        // 1. 현금 업적
        AddAchievement("Gold_01", "돈이 복사가 된다고!", "골드 5만 모으기", AchievementType.ReachGold, 50000,
              () => Company.Instance.gold.Value >= 50000);

        AddAchievement("Gold_02", "내 주머니에 종이가 백장", "골드 10만 모으기", AchievementType.ReachGold, 100000,
            () => Company.Instance.gold.Value >= 100000);

        AddAchievement("Gold_02", "우린 부자가 될꺼야!", "골드 20만 모으기", AchievementType.ReachGold, 100000,
            () => Company.Instance.gold.Value >= 2000000);

        // 2. 총 매출 업적
        AddAchievement("TotalGold_01", "돈의 맛", "총 매출 5만 달성", AchievementType.TotalGold, 50000,
            () => Company.Instance.totalRevenue >= 50000);

        AddAchievement("TotalGold_02", "저희 회사 들어보셨나요??", "총 매출 10만 달성", AchievementType.TotalGold, 100000,
            () => Company.Instance.totalRevenue >= 100000);

        // 3. 회사 증축 업적
        AddAchievement("Office_01", "좋좋소", "중형 회사로 증축", AchievementType.MaxLevel, 2,
            () => Company.Instance.level >= 2);

        AddAchievement("Office_02", "우리도 이제 머기업??", "대형 회사로 증축", AchievementType.MaxLevel, 3,
            () => Company.Instance.level >= 3);

        // 4. 플레이타임 업적
        AddAchievement("Time_01", "엉덩이가 무거운 개발자", "10분 동안 개발하기", AchievementType.PlayTime, 600,
            () => DateTimeManager.Instance != null && Mathf.FloorToInt(GetPlayTimeRaw()) >= 600);

        AddAchievement("Time_02", "시간의 방", "30분 동안 개발하기", AchievementType.PlayTime, 1800,
            () => DateTimeManager.Instance != null && Mathf.FloorToInt(GetPlayTimeRaw()) >= 1800);
    }

    private void AddAchievement(string id, string title, string desc, AchievementType type, int target, System.Func<bool> condition)
    {
        achievements.Add(new Achievement
        {
            id             = id,
            title          = title,
            description    = desc,
            type           = type,
            targetValue    = target,
            isUnlocked     = false,
            conditionCheck = condition // 콜백 보관
        });
    }

    private void BindDataObservations() 
    {
        Company.Instance.gold
            .Subscribe(_ => CheckAll())
            .AddTo(_disposables);
    }

    public void CheckAll()
    {
        foreach (var ach in achievements)
        {
            if (ach.isUnlocked) continue;

            bool unlocked = ach.type switch
            {
                AchievementType.ReachGold => Company.Instance.gold.Value   >= ach.targetValue,
                AchievementType.TotalGold => Company.Instance.totalRevenue >= ach.targetValue,

                AchievementType.MaxLevel => Company.Instance.level >= ach.targetValue,

                _ => false
            };

            if (unlocked)
            {
                ach.isUnlocked = true;
                _achievementUI.ShowAchievement(ach.title, ach.description);
            }
        }
    }

    private float GetPlayTimeRaw()
    {
        string timeStr = DateTimeManager.Instance.GetPlayTime();
        string[] split = timeStr.Split(':');
        if (split.Length == 2 && int.TryParse(split[0], out int min) && int.TryParse(split[1], out int sec))
        {
            return (min * 60) + sec;
        }
        return 0f;
    }
}