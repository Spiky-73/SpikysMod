using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public class ClientConfig : ModConfig {

    [DefaultValue(true), Label($"${LocKeys.ClientConfig}.smartConsume.Label"), Tooltip($"${LocKeys.ClientConfig}.smartConsume.Tooltip")]
    public bool smartConsumption;
    [DefaultValue(true), Label($"${LocKeys.ClientConfig}.smartAmmo.Label"), Tooltip($"${LocKeys.ClientConfig}.smartAmmo.Tooltip")]
    public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly), Label($"${LocKeys.ClientConfig}.smartPickup.Label"), Tooltip($"${LocKeys.ClientConfig}.smartPickup.Tooltip")]
    public SmartPickupLevel smartPickup;
    [DefaultValue(true), Label($"${LocKeys.ClientConfig}.itemSwap.Label"), Tooltip($"${LocKeys.ClientConfig}.itemSwap.Tooltip")]
    public bool itemSwap;
    [DefaultValue(true), Label($"${LocKeys.ClientConfig}.fastRightClick.Label"), Tooltip($"${LocKeys.ClientConfig}.fastRightClick.Tooltip")]
    public bool fastRightClick;
    [DefaultValue(true), Label($"${LocKeys.ClientConfig}.itemRightClick.Label"), Tooltip($"${LocKeys.ClientConfig}.itemRightClick.Tooltip")]
    public bool itemRightClick;
    [DefaultValue(true), Label($"${LocKeys.ClientConfig}.filterRecipes.Label"), Tooltip($"${LocKeys.ClientConfig}.filterRecipes.Tooltip")]
    public bool filterRecipes;


    public override ConfigScope Mode => ConfigScope.ClientSide;
#nullable disable
    public static ClientConfig Instance;
#nullable restore
}