using System.Collections.Generic;

namespace Dialogue
{
    public static class NeglectPenaltyHandler
    {
        // 다음 주에 적용할 피로도 +10 대상 목록
        private static readonly List<int> _pendingFatigueIds = new List<int>();

        // 금요일 밤에 호출
        // 충성도 -5 즉시 적용, 피로도 +10은 다음 주 적용을 위해 저장
        public static void ApplyNeglectPenalty(IReadOnlyList<int> neglectedEmployeeIds)
        {
            _pendingFatigueIds.Clear();

            foreach (int id in neglectedEmployeeIds)
            {
                // 충성도 -5 즉시 적용
                DialogueEvents.RequestStatChange(new StatDelta
                {
                    employeeId = id,
                    loyaltyDelta = -5,
                });

                // 피로도 +10 다음 주 적용 대상 저장
                _pendingFatigueIds.Add(id);
            }
        }

        // 다음 주 월요일 전환 시 호출
        public static void ApplyPendingFatigue()
        {
            foreach (int id in _pendingFatigueIds)
            {
                DialogueEvents.RequestStatChange(new StatDelta
                {
                    employeeId = id,
                    fatigueDelta = 10,
                });
            }

            _pendingFatigueIds.Clear();
        }
    }
}