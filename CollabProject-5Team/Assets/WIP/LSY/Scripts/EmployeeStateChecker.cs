namespace Dialogue
{
    public static class EmployeeStateChecker
    {
        // 피로도·의욕 수치를 받아 대화 상태 반환
        public static EmployeeDialogueState GetState(int fatigue, int motivation)
        {
            bool highFatigue = fatigue > 50;
            bool lowMotivation = motivation < 50;

            if (!highFatigue && !lowMotivation) return EmployeeDialogueState.Normal;
            if ( highFatigue && lowMotivation) return EmployeeDialogueState.Danger;
            return EmployeeDialogueState.Warning;
        }

        // Warning 또는 Danger이면 true
        public static bool IsInCrisis(EmployeeDialogueState state)
            => state != EmployeeDialogueState.Normal;

        // 의욕 수치 기반 보고서 가중치 반환
        public static int GetMotivationWeight(int motivation)
        {
            if (motivation >= 80) return  5;
            if (motivation >= 40) return  0;
            return -10;
        }
    }
}