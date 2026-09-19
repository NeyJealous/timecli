using System;

namespace TimeClickers.PortCore;

public static class AbilityMath
{
    public static double GetCostForLevel(int level) =>
        Math.Floor(250.0 * Math.Pow(90.0, level));

    public static double GetDimensionShiftMultiplier(
        int dimensionShifts,
        double artifactDimensionShiftMultiplier)
    {
        double value = 1.0;
        for (int i = 0; i < dimensionShifts; i++)
            value *= artifactDimensionShiftMultiplier;
        return value;
    }
}
