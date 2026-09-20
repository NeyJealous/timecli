using System;
using System.Globalization;
using System.Text;

namespace TimeClickers.PortCore;

public enum NumberDisplayType
{
    Simplified = 0,
    Scientific = 1,
    ScientificNormalized = 2
}

/// <summary>
/// Independent reconstruction of ClickerUtils display formatting from
/// Time Clickers 1.4.5. These helpers keep presentation text consistent
/// between headless tests and the modern Unity HUD.
/// </summary>
public static class DisplayFormatting
{
    private static readonly char[] SuperscriptChars =
    {
        '⁰', '¹', '²', '³', '⁴',
        '⁵', '⁶', '⁷', '⁸', '⁹'
    };

    private static readonly string[] Suffixes1 =
    {
        string.Empty, "K", "M", "B", "T", "Q"
    };

    private static readonly string[] Suffixes2 =
    {
        "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
        "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"
    };

    public static string FormatValue(
        double value,
        NumberDisplayType type = NumberDisplayType.Simplified)
    {
        return type switch
        {
            NumberDisplayType.Scientific =>
                FormatValueScientific(value),
            NumberDisplayType.ScientificNormalized =>
                FormatValueScientificNormalized(value),
            _ =>
                FormatValueSimplified(value)
        };
    }

    public static string FormatValueSimplified(double value)
    {
        if (value < 1000.0)
            return value.ToString("0", CultureInfo.InvariantCulture);

        int group = GetEngineeringGroup(value);

        double integerMantissa = Math.Floor(
            value /
            Math.Pow(10.0, group * 3));

        if (integerMantissa > 999.0)
        {
            string digits = integerMantissa.ToString(
                "0",
                CultureInfo.InvariantCulture);

            digits = digits.Insert(
                digits.Length - 3,
                ".");

            return digits.Substring(0, 4) +
                   GetSuffix(group + 1);
        }

        return integerMantissa.ToString(
                   "0",
                   CultureInfo.InvariantCulture) +
               GetSuffix(group);
    }

    public static string FormatValueScientific(double value)
    {
        if (value < 1000.0)
            return value.ToString("0", CultureInfo.InvariantCulture);

        int group = GetEngineeringGroup(value);

        double integerMantissa = Math.Floor(
            value /
            Math.Pow(10.0, group * 3));

        if (integerMantissa > 999.0)
        {
            string digits = integerMantissa.ToString(
                "0",
                CultureInfo.InvariantCulture);

            digits = digits.Insert(
                digits.Length - 3,
                ".");

            return digits.Substring(0, 4) +
                   "e" +
                   GetSuperscriptNumber(
                       (group + 1) * 3);
        }

        return integerMantissa.ToString(
                   "0",
                   CultureInfo.InvariantCulture) +
               "e" +
               GetSuperscriptNumber(
                   group * 3);
    }

    public static string FormatValueScientificNormalized(
        double value)
    {
        if (value < 1000.0)
            return value.ToString("0", CultureInfo.InvariantCulture);

        int exponent =
            (int)Math.Floor(Math.Log10(value));

        exponent = Math.Max(0, exponent);

        double mantissa =
            value /
            Math.Pow(10.0, exponent);

        // Original truncates, rather than rounds, to two decimals before
        // applying the fixed-point format string.
        mantissa =
            Math.Truncate(mantissa * 100.0) /
            100.0;

        return mantissa.ToString(
                   "F2",
                   CultureInfo.InvariantCulture) +
               "e" +
               GetSuperscriptNumber(exponent);
    }

    public static string GetSuffix(int index)
    {
        if (index < Suffixes1.Length)
            return Suffixes1[index];

        index -= Suffixes1.Length;

        int suffixPower = 0;

        while (index >= Suffixes2.Length)
        {
            index -= Suffixes2.Length;
            suffixPower++;
        }

        string suffix = Suffixes2[index];

        return suffixPower == 0
            ? suffix
            : suffix +
              GetSuperscriptNumber(suffixPower);
    }

    public static string GetSuperscriptNumber(int value)
    {
        string digits = value.ToString(
            CultureInfo.InvariantCulture);

        var result = new StringBuilder(
            digits.Length);

        foreach (char digit in digits)
        {
            switch (digit)
            {
                case '-':
                    result.Append('⁻');
                    break;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    result.Append(
                        SuperscriptChars[
                            digit - '0']);
                    break;
            }
        }

        return result.ToString();
    }

    public static string FormatSeconds(int seconds)
    {
        int hours =
            (int)Math.Floor(
                seconds / 3600f);

        int minutes =
            (int)Math.Floor(
                seconds / 60f) -
            hours * 60;

        int remainingSeconds =
            seconds % 60;

        if (hours > 0)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}",
                hours,
                minutes.ToString(
                    "00",
                    CultureInfo.InvariantCulture),
                remainingSeconds.ToString(
                    "00",
                    CultureInfo.InvariantCulture));
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}",
            minutes,
            remainingSeconds.ToString(
                "00",
                CultureInfo.InvariantCulture));
    }

    public static string FormatSeconds(float seconds)
    {
        if (seconds <= 60f)
        {
            return seconds.ToString(
                "0.0",
                CultureInfo.InvariantCulture);
        }

        int hours =
            (int)Math.Floor(
                seconds / 3600f);

        int minutes =
            (int)Math.Floor(
                seconds / 60f) -
            hours * 60;

        int wholeSeconds =
            (int)Math.Floor(seconds) %
            60;

        int tenths =
            (int)Math.Floor(
                seconds * 10f) %
            10;

        if (hours > 0)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}:{2}.{3}",
                hours,
                minutes.ToString(
                    "00",
                    CultureInfo.InvariantCulture),
                wholeSeconds.ToString(
                    "00",
                    CultureInfo.InvariantCulture),
                tenths);
        }

        if (minutes > 0)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}.{2}",
                minutes,
                wholeSeconds.ToString(
                    "00",
                    CultureInfo.InvariantCulture),
                tenths);
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}.{1}",
            wholeSeconds.ToString(
                "00",
                CultureInfo.InvariantCulture),
            tenths);
    }

    public static string FormatULong(ulong value)
    {
        if (value < 1_000_000UL)
        {
            return value.ToString(
                "#,##0",
                CultureInfo.InvariantCulture);
        }

        ulong thousands =
            value / 1000UL;

        double millions =
            thousands /
            1000.0;

        return millions.ToString(
                   "#,##0.000",
                   CultureInfo.InvariantCulture) +
               " M";
    }

    private static int GetEngineeringGroup(
        double value)
    {
        int group =
            ((int)Math.Floor(
                Math.Log10(value)) +
             1) /
            3;

        return Math.Max(
            0,
            group - 1);
    }
}
