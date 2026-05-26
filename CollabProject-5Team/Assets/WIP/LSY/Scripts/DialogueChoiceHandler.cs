namespace Dialogue
{
    public static class DialogueChoiceHandler
    {
        public static void ApplyChoice(int employeeId, DialogueChoice choice)
        {
            StatDelta delta;

            switch (choice)
            {
                case DialogueChoice.BonusPay:
                    // A. 특별 보너스 지급
                    delta = new StatDelta
                    {
                        employeeId = employeeId,
                        motivationDelta = 30,
                        loyaltyDelta = 10,
                    };
                    DialogueEvents.RequestStatChange(delta);
                    DialogueEvents.RequestBonusPay(employeeId);
                    break;

                case DialogueChoice.VacationPromise:
                    // B. 휴가를 약속
                    delta = new StatDelta
                    {
                        employeeId = employeeId,
                        fatigueDelta = -20,
                        loyaltyDelta = 10,
                    };
                    DialogueEvents.RequestStatChange(delta);
                    DialogueEvents.NotifyVacationPromised(employeeId);
                    break;

                case DialogueChoice.Pressure:
                    // C. 정신력 강조
                    delta = new StatDelta
                    {
                        employeeId = employeeId,
                        motivationDelta = -15,
                        fatigueDelta = 20,
                    };
                    DialogueEvents.RequestStatChange(delta);
                    break;
            }
        }

        // 일상 대화 결과 처리.
        public static void ApplyNormalResult(int employeeId, bool isCorrect)
        {
            StatDelta delta = new StatDelta
            {
                employeeId = employeeId,
                loyaltyDelta = isCorrect ? 3 : -5,
            };
            DialogueEvents.RequestStatChange(delta);
        }
    }
}