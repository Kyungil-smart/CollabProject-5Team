using System.Collections.Generic;

/// <summary>
/// 스토리 퀘스트 대사 안에 박혀있는 토큰(USER, COMP, SPY, UCSPY, NPC1, NPC2)을
/// 실제 닉네임/회사명/직원이름으로 치환한다.
/// </summary>
public static class StoryTextResolver
{
    public static string Resolve(string text, Dictionary<string, string> speakerNames)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string result = text;

        if (Company.Instance != null)
        {
            result = result.Replace("COMP", Company.Instance.CompanyName);
            result = result.Replace("USER", Company.Instance.playerName);
        }

        if (speakerNames != null)
        {
            foreach (var kv in speakerNames)
                result = result.Replace(kv.Key, kv.Value);
        }

        return result;
    }
}