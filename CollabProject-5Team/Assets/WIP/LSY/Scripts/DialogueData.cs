namespace Dialogue
{
    public enum EmployeeDialogueState
    {
        Normal,
        Warning,
        Danger,
    }

    public enum DialogueChoice
    {
        BonusPay,
        VacationPromise,
        Pressure,
    }

    public struct StatDelta
    {
        public int employeeId;
        public int motivationDelta;
        public int fatigueDelta;
        public int loyaltyDelta;
    }

    public struct MotivationWeight
    {
        public int employeeId;
        public int weight;
    }

    public struct DialogueStartPayload
    {
        public int employeeId;
        public EmployeeDialogueState state;
        // TODO: 대화 텍스트, 선택지 데이터 (DialogueDataSO 작성 후 추가)
    }
}