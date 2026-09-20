namespace TimeClickers.PortCore;

/// <summary>
/// The three independent ClickerWeapon controls exposed by the original HUD.
/// </summary>
public enum ClickWeaponSlot
{
    Pistol,
    Cannon,
    Launcher
}

/// <summary>
/// UIButtonIdleMode cycles ManualAim -> AutoAim -> Disabled -> ManualAim.
/// Disabled calls ClickerWeapon.Hide(); returning to ManualAim calls Show().
/// </summary>
public enum ClickWeaponMode
{
    ManualAim,
    AutoAim,
    Disabled
}
