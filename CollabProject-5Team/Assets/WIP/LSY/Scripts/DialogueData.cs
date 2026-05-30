namespace Dialogue
{
    public enum EmployeeDialogueState
    {
        Normal,
        Caution,
        Critical,
    }

    public struct StatDelta
    {
        public int employeeId;
        public int desireDelta;
        public int fatigueDelta;
        public int loyaltyDelta;
    }

    public struct DialogueStartPayload
    {
        public int employeeId;
        public EmployeeDialogueState state;
        public string text; // 대사 텍스트
        public bool isChoice; // true면 선택지 표시
        public string choice01; // 선택지 1 텍스트
        public string choice02; // 선택지 2 텍스트
    }
}