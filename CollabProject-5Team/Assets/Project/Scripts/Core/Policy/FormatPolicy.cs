using System.Globalization;

// 통일된 단위 표기 정책
public static class FormatPolicy 
{
    public static string FormatGold(int gold)
    {
        if (gold < 10_000)
            return $"{gold:N0}G";

        if (gold < 100_000)
            return $"{DecimalTruncate(gold, 1_000)}k";

        if (gold < 10_000_000)
            return $"{(gold / 1_000).ToString("N0", CultureInfo.InvariantCulture)}k";

        if (gold < 100_000_000)
            return $"{DecimalTruncate(gold, 1_000_000)}m";

        return $"{(gold / 1_000_000).ToString("N0", CultureInfo.InvariantCulture)}m";
    }

    static string DecimalTruncate(int value, int unit)
    {
        int scaled = value * 10 / unit; // 소수 첫째 자리까지 버림
        int intPart = scaled / 10;
        int decimalPart = scaled % 10;

        return decimalPart == 0
            ? intPart.ToString("N0", CultureInfo.InvariantCulture)
            : $"{intPart.ToString("N0", CultureInfo.InvariantCulture)}.{decimalPart}";
    }
}
