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
       AddAchievement("Gold_01",      "돈이 복사가 된다고!"    , "골드 5만 모으기" ,  AchievementType.ReachGold,   50000);
       AddAchievement("Gold_02",      "내 주머니에 종이가 백장" , "골드 10만 모으기",  AchievementType.ReachGold,  100000);
       AddAchievement("Gold_03",      "정승처럼 써보자"        , "골드 20만 모으기",  AchievementType.ReachGold,  200000);
       AddAchievement("TotalGold_01", "우린 부자가 될꺼야!"    , "총 매출 5만 달성" , AchievementType.TotalGold,   50000);
       AddAchievement("TotalGold_02", "돈의 맛"               , "총 매출 10만 달성", AchievementType.TotalGold,  100000);

       AddAchievement("Office_02", "좋좋소",                    "중형 회사로 증축", AchievementType.MaxLevel, 2);
       AddAchievement("Office_03", "우리도 이제 머기업인가요?" , "대형 회사로 증축", AchievementType.MaxLevel, 3);
    }

    private void AddAchievement(string id, string title, string desc, AchievementType type, int target)
    {
        achievements.Add(new Achievement
        {
            id           = id,
            title        = title,
            description  = desc,
            type         = type,
            targetValue  = target,
            isUnlocked   = false,
            currentValue = 0
        });
    }

    private void BindDataObservations() 
    {
        Company.Instance.gold
            .Subscribe(currentGold =>
            {
                UpdateCurrentValue(AchievementType.ReachGold, currentGold);
                UpdateCurrentValue(AchievementType.TotalGold, Company.Instance.totalRevenue);
                UpdateCurrentValue(AchievementType.MaxLevel , Company.Instance.level);

                CheckAll();
            })
            .AddTo(_disposables);
    }

    private void UpdateCurrentValue(AchievementType type, int value)
    {
        foreach (var ach in achievements)
        {
            if (ach.type == type && !ach.isUnlocked)
                ach.currentValue = value;
        }
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
}