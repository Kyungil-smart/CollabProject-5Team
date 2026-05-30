using R3;

namespace Dialogue
{
    public static class DialogueEvents
    {
        public static readonly Subject<StatDelta> OnStatChangeRequested = new();
        public static void RequestStatChange(StatDelta delta) => OnStatChangeRequested.OnNext(delta);

        public static readonly Subject<int> OnVacationPromised = new();
        public static void NotifyVacationPromised(int employeeId) => OnVacationPromised.OnNext(employeeId);

        public static readonly Subject<DialogueStartPayload> OnDialogueReady = new();
        public static void NotifyDialogueReady(DialogueStartPayload payload) => OnDialogueReady.OnNext(payload);

        public static readonly Subject<int> OnDialogueEnded = new();
        public static void NotifyDialogueEnded(int employeeId) => OnDialogueEnded.OnNext(employeeId);
    }
}