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
                else
                {
                    Debug.LogWarning($"[DialogueEffectParser] 파싱 불가 항목: {trimmed}");
                }
            }

            DialogueEvents.RequestStatChange(delta);
        }

        static void ApplyStat(ref StatDelta delta, string statName, int value)
        {
            switch (statName)
            {
                case "desire":  delta.desireDelta  += value; break;
                case "fatigue": delta.fatigueDelta += value; break;
                case "loyalty": delta.loyaltyDelta += value; break;
                case "gold":
                    if (Company.Instance != null)
                        Company.Instance.gold += value;
                    else
                        Debug.LogWarning("[DialogueEffectParser] Company.Instance가 null — gold 효과 미적용");
                    break;
                default:
                    Debug.LogWarning($"[DialogueEffectParser] 알 수 없는 스탯: {statName}");
                    break;
            }
        }

        static int ParseValue(string raw)
        {
            if (int.TryParse(raw.Trim(), out int result)) return result;
            Debug.LogWarning($"[DialogueEffectParser] 숫자 파싱 실패: {raw}");
            return 0;
        }
    }
}