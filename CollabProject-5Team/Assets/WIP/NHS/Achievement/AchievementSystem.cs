using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using UnityEngine;

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
        AddAchievement("rich_50k", "부자", "골드 5만 모으기", AchievementType.ReachGold, 50000);
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
                AchievementType.ReachGold => Company.Instance.gold.Value >= ach.targetValue,
                _ => false
            };

            if (unlocked)
            {
                ach.isUnlocked = true;
                _achievementUI.ShowAchievement(ach.title, ach.description);
                Debug.Log($"🎉 업적 달성: {ach.title}");
                // TODO: UI 팝업
            }
        }
    }
}