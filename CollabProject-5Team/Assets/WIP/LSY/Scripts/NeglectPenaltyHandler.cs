using System.Collections.Generic;

namespace Dialogue
{
    public static class NeglectPenaltyHandler
    {
        // TODO: 낮밤 시스템 완성 후 피로도 +10을 다음 주 적용으로 교체
        public static void ApplyNeglectPenalty(IReadOnlyList<int> allEmployeeIds)
        {
            List<int> neglectedIds = DayTalkTracker.Instance.GetNeglectedIds(allEmployeeIds);

            foreach (int id in neglectedIds)
            {
                DialogueEvents.RequestStatChange(new StatDelta
                {
                    employeeId   = id,
                    loyaltyDelta = -5,
                    fatigueDelta = 10,
                });
            }
        }
    }
}