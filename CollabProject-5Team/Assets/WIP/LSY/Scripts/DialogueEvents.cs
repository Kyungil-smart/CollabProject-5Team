using R3;

namespace Dialogue
{
    public static class DialogueEvents
    {
        public static readonly Subject<StatDelta> OnStatChangeRequested = new();
        public static void RequestStatChange(StatDelta delta) => OnStatChangeRequested.OnNext(delta);

        public static readonly Subject<MotivationWeight> OnNightWeightReady = new();
        public static void SendNightWeight(MotivationWeight weight) => OnNightWeightReady.OnNext(weight);

        public static readonly Subject<int> OnVacationPromised = new();
        public static void NotifyVacationPromised(int employeeId) => OnVacationPromised.OnNext(employeeId);

        public static readonly Subject<int> OnBonusPayRequested = new();
        public static void RequestBonusPay(int employeeId) => OnBonusPayRequested.OnNext(employeeId);

        public static readonly Subject<DialogueStartPayload> OnDialogueReady = new();
        public static void NotifyDialogueReady(DialogueStartPayload payload) => OnDialogueReady.OnNext(payload);
    }
}