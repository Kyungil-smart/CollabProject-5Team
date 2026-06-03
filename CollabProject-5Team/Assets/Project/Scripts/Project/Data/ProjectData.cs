using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectSize
{
    small,
    medium,
    large,
}

[Serializable]
public class ProjectCompleted
{
    // 이전 데이터 연동
    public int projectID;
    public string projectName;
    public ProjectSize scale;
    public int qualityScore;
    public int stabilityScore;
    public int charmScore;
    public char grade; // 등급 (S, A, B, C)

    // 완료 데이터
    public float rating; // 평점 (0.5~5.0)
    public float Rating
    {
        get => rating;
        set => rating = Mathf.Clamp(value, 0.5f, 5f);
    }

    float _retentionFactor; // 유지력 계수 (0.0~1.0, 출시 후 감소)
    public float RetentionFactor
    {
        get => _retentionFactor;
        set => _retentionFactor = Mathf.Clamp01(value);
    }

    public int users;       // 현재 유저 수
    public int dailySales;  // 일일 판매량
    public int goodsSales;  // 굿즈 판매량
    public int dailyGold;   // 일일 매출 (= dailySales * factor + goodsSales * factor)
    public int dailyCost;   // 유지비

    // 주간 누적 매출 (하루씩 쌓다가 금요일 밤에 히스토리로 이동)
    public int weeklyGoldAccum;

    // 지난 주 비교 데이터 (UI 증감 표시용)
    public int prevWeekUsers;
    public int prevWeekGold;

    // 최근 4주간 주간 매출 히스토리 (그래프용)
    public Queue<int> weeklyGoldHistory = new();
    const int WEEKLY_HISTORY_MAX = 4;
    public void QueueWeeklyGold(int weekGold)
    {
        if (weeklyGoldHistory.Count >= WEEKLY_HISTORY_MAX)
            weeklyGoldHistory.Dequeue();
        weeklyGoldHistory.Enqueue(weekGold);
    }

    // 종료
    public bool isServiceOver; // 서비스 종료 여부
}
