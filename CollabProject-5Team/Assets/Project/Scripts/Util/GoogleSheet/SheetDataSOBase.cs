using System;
using System.Globalization;
using UnityEngine;

public abstract class SheetDataSOBase : ScriptableObject
{
    public int id; // 파싱 기준값
    public int row; // DataRequestSet이 주입

    public abstract void SetData(string[] data);

    protected int ParseInt(string raw)
    {
        if (int.TryParse(raw, out int result)) return result;
        LogWarn(raw, "int");
        return default;
    }
    protected float ParseFloat(string raw)
    {
        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) return result;
        LogWarn(raw, "float");
        return default;
    }
    protected bool ParseBool(string raw)
    {
        if (bool.TryParse(raw, out bool result)) return result;
        LogWarn(raw, "bool");
        return default;
    }
    protected T ParseEnum<T>(string raw) where T : struct, Enum
    {
        if (Enum.TryParse(raw, ignoreCase: true, out T result)) return result;
        LogWarn(raw, typeof(T).Name);
        return default;
    }

    /// <summary>
    /// [Flags] Enum 파싱 (테스트 필요)
    /// - 구분자(| ,) 방식: "Shy|Active" → HashTags.Shy | HashTags.Active
    /// - 문자 연속 방식: "INFP" → 각 문자(I, N, F, P)를 개별 플래그로 OR 조합
    /// </summary>
    protected T ParseEnumFlags<T>(string raw) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw)) return default;

        // 구분자가 있으면 구분자 방식으로 파싱
        string[] tokens = raw.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length > 1 || Enum.TryParse(raw.Trim(), ignoreCase: true, out T _))
        {
            // 구분자 방식
            int flags = 0;
            foreach (string token in tokens)
            {
                string trimmed = token.Trim();
                if (Enum.TryParse(trimmed, ignoreCase: true, out T value))
                    flags |= (int)(object)value;
                else
                    LogWarn(trimmed, typeof(T).Name);
            }
            return (T)(object)flags;
        }
        else
        {
            // 문자 연속 방식 (예: "INFP" → I | N | F | P)
            int flags = 0;
            bool anyParsed = false;
            foreach (char c in raw)
            {
                string letter = c.ToString();
                if (Enum.TryParse(letter, ignoreCase: true, out T value))
                {
                    flags |= (int)(object)value;
                    anyParsed = true;
                }
                else
                {
                    LogWarn(letter, typeof(T).Name);
                }
            }
            if (!anyParsed) LogWarn(raw, typeof(T).Name);
            return (T)(object)flags;
        }
    }

    private void LogWarn(string raw, string expectedType)
    {
        Debug.LogWarning(
            $"[파싱실패]  <b>{row}행</b> \n 입력값: \"{raw}\" | 기대타입: {expectedType}"
        );
    }
}