using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    public class DayTalkTracker : MonoBehaviour
    {
        public static DayTalkTracker Instance { get; private set; }

        private const int MaxDailyTalkCount = 2; // 하루 최대 대화 횟수

        private int _dailyTalkCount = 0;
        // 오늘 대화한 직원 ID (낮 시작마다 초기화)
        private readonly HashSet<int> _talkedToday    = new();
        // 이번 주 대화한 직원 ID (새 주 시작마다 초기화)
        private readonly HashSet<int> _talkedThisWeek = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // 오늘 대화 횟수가 최대치(2회)에 도달했는지 여부
        public bool IsAtDailyLimit()
            => _dailyTalkCount >= MaxDailyTalkCount;

        // 오늘 해당 직원과 대화 완료 여부
        public bool HasTalkedToday(int employeeId)
            => _talkedToday.Contains(employeeId);

        // 대화 완료 기록. 오늘 + 이번 주 모두 기록, 일일 횟수 증가
        public void MarkTalked(int employeeId)
        {
            _talkedToday.Add(employeeId);
            _talkedThisWeek.Add(employeeId);
            _dailyTalkCount++;
        }

        // 낮 시작 시 호출. 오늘 대화 기록 및 횟수 초기화
        public void ResetDaily()
        {
            _talkedToday.Clear();
            _dailyTalkCount = 0;
        }

        // 새 주 시작 시 호출. 오늘 + 이번 주 대화 기록 및 횟수 전체 초기화
        public void ResetWeekly()
        {
            _talkedToday.Clear();
            _talkedThisWeek.Clear();
            _dailyTalkCount = 0;
        }

        // 이번 주 한 번도 대화하지 않은 직원 ID 목록 반환. 금요일 밤 방치 패널티 판정에 사용
        public List<int> GetNeglectedIds(IReadOnlyList<int> allEmployeeIds)
        {
            List<int> neglected = new List<int>();
            foreach (int id in allEmployeeIds)
            {
                if (!_talkedThisWeek.Contains(id))
                    neglected.Add(id);
            }
            return neglected;
        }
    }
}