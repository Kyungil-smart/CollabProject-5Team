using System;
using UnityEngine;

namespace Dialogue
{
    public static class DialogueEffectParser
    {
        public static void Apply(string effectScript, int employeeId)
        {
            if (string.IsNullOrEmpty(effectScript)) return;

            string[]  parts = effectScript.Split(',');
            StatDelta delta = new StatDelta { employeeId = employeeId };

            foreach (string part in parts)
            {
                string trimmed = part.Trim();

                if (trimmed.Contains("+="))
                {
                    string[] kv = trimmed.Split(new string[] { "+=" }, StringSplitOptions.None);
                    ApplyStat(ref delta, kv[0].Trim(), ParseValue(kv[1]));
                }
                else if (trimmed.Contains("-="))
                {
                    string[] kv = trimmed.Split(new string[] { "-=" }, StringSplitOptions.None);
                    ApplyStat(ref delta, kv[0].Trim(), -ParseValue(kv[1]));
                }
                else if (trimmed.Contains("="))
                {
                    string[] kv = trimmed.Split(new string[] { "=" }, StringSplitOptions.None);
                    ApplyStat(ref delta, kv[0].Trim(), ParseValue(kv[1]));
                }
            }

            DialogueEvents.RequestStatChange(delta);
        }

        static void ApplyStat(ref StatDelta delta, string statName, int value)
        {
            switch (statName.ToLower())
            {
                case "desire":  delta.desireDelta  += value; break;
                case "fatigue": delta.fatigueDelta += value; break;
                case "loyalty": delta.loyaltyDelta += value; break;
                case "gold":
                    if (Company.Instance != null)
                    {
                        Company.Instance.gold.Value += value;
                        if (value >= 0)
                        {
                            Company.Instance.curManagementStatus.otherIncome += value;
                            Company.Instance.cumulativeManagementStatus.otherIncome += value;
                        }
                        else
                        {
                            Company.Instance.curManagementStatus.otherExpense += -value;
                            Company.Instance.cumulativeManagementStatus.otherExpense += -value;
                        }

                        Company.Instance.curManagementStatus.Recalculate();
                        Company.Instance.cumulativeManagementStatus.Recalculate();
                    }
                    break;
                default:
                    break;
            }
        }

        static int ParseValue(string raw)
        {
            if (int.TryParse(raw.Trim(), out int result)) return result;
            return 0;
        }
    }
}