using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public class ClientConfig : ModConfig {

    [DefaultValue(true), Label($"${Localization.Keys.ClientConfig}.smartConsume.Label"), Tooltip($"${Localization.Keys.ClientConfig}.smartConsume.Tooltip")]
    public bool smartConsumption;
    [DefaultValue(true), Label($"${Localization.Keys.ClientConfig}.smartAmmo.Label"), Tooltip($"${Localization.Keys.ClientConfig}.smartAmmo.Tooltip")]
    public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly), Label($"${Localization.Keys.ClientConfig}.smartPickup.Label"), Tooltip($"${Localization.Keys.ClientConfig}.smartPickup.Tooltip")]
    public SmartPickupLevel smartPickup;
    [DefaultValue(true), Label($"${Localization.Keys.ClientConfig}.itemSwap.Label"), Tooltip($"${Localization.Keys.ClientConfig}.itemSwap.Tooltip")]
    public bool itemSwap;
    [DefaultValue(true), Label($"${Localization.Keys.ClientConfig}.fastRightClick.Label"), Tooltip($"${Localization.Keys.ClientConfig}.fastRightClick.Tooltip")]
    public bool fastRightClick;
    [DefaultValue(true), Label($"${Localization.Keys.ClientConfig}.itemRightClick.Label"), Tooltip($"${Localization.Keys.ClientConfig}.itemRightClick.Tooltip")]
    public bool itemRightClick;
    [DefaultValue(true), Label($"${Localization.Keys.ClientConfig}.filterRecipes.Label"), Tooltip($"${Localization.Keys.ClientConfig}.filterRecipes.Tooltip")]
    public bool filterRecipes;


    public override ConfigScope Mode => ConfigScope.ClientSide;
#nullable disable
    public static ClientConfig Instance;
#nullable restore
}