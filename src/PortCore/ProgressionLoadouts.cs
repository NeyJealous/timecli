using System;

namespace TimeClickers.PortCore;

public sealed class ArtifactLoadout
{
    private readonly ulong[] _levels = new ulong[(int)ArtifactType.Total];

    public ulong GetLevel(ArtifactType type) => _levels[(int)type];

    public void SetLevel(ArtifactType type, ulong level)
    {
        var data = CanonicalArtifacts.Get(type);
        if (level > data.MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(level));

        _levels[(int)type] = level;
    }

    public double GetModValue(ArtifactType type) =>
        CanonicalArtifacts.GetModValue(type, GetLevel(type));

    public double GetNextUpgradeCost(ArtifactType type) =>
        CanonicalArtifacts.GetUpgradeCost(type, GetLevel(type));

    public ArtifactEffects BuildEffects()
    {
        var values = new double[(int)ArtifactType.Total];
        for (int i = 0; i < values.Length; i++)
        {
            var type = (ArtifactType)i;
            values[i] = CanonicalArtifacts.GetModValue(type, _levels[i]);
        }

        return ArtifactEffectsMath.Recalculate(values);
    }

    public void Reset()
    {
        Array.Clear(_levels, 0, _levels.Length);
    }
}

public sealed class WeaponAugmentLoadout
{
    private readonly ulong[] _levels = new ulong[(int)WeaponAugmentType.Total];

    public ulong GetLevel(WeaponAugmentType type) => _levels[(int)type];

    public void SetLevel(WeaponAugmentType type, ulong level)
    {
        var data = CanonicalWeaponAugments.Get(type);
        if (level > data.MaxLevel)
            throw new ArgumentOutOfRangeException(nameof(level));

        _levels[(int)type] = level;
    }

    public double GetModValue(WeaponAugmentType type) =>
        CanonicalWeaponAugments.GetModValue(type, GetLevel(type));

    public ulong GetNextUpgradeCost(WeaponAugmentType type) =>
        CanonicalWeaponAugments.GetUpgradeCost(type, GetLevel(type));

    public WeaponAugmentEffects BuildEffects()
    {
        var values = new double[(int)WeaponAugmentType.Total];
        for (int i = 0; i < values.Length; i++)
        {
            var type = (WeaponAugmentType)i;
            values[i] = CanonicalWeaponAugments.GetModValue(type, _levels[i]);
        }

        return WeaponAugmentEffectsMath.Recalculate(values);
    }

    public void Reset()
    {
        Array.Clear(_levels, 0, _levels.Length);
    }
}
