using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public class InventoryManagement : ModConfig {

    [DefaultValue(true), Label($"${Localization.Keys.InventoryManagement}.smartConsume.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.smartConsume.Tooltip")]
    public bool smartConsumption;
    [DefaultValue(true), Label($"${Localization.Keys.InventoryManagement}.smartAmmo.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.smartAmmo.Tooltip")]
    public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly), Label($"${Localization.Keys.InventoryManagement}.smartPickup.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.smartPickup.Tooltip")]
    public SmartPickupLevel smartPickup;
    
    [DefaultValue(true), Label($"${Localization.Keys.InventoryManagement}.itemSwap.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.itemSwap.Tooltip")]
    public bool itemSwap;
    [DefaultValue(true), Label($"${Localization.Keys.InventoryManagement}.fastRightClick.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.fastRightClick.Tooltip")]
    public bool fastRightClick;
    [DefaultValue(true), Label($"${Localization.Keys.InventoryManagement}.itemRightClick.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.itemRightClick.Tooltip")]
    public bool itemRightClick;
    
    [DefaultValue(true), Label($"${Localization.Keys.InventoryManagement}.filterRecipes.Label"), Tooltip($"${Localization.Keys.InventoryManagement}.filterRecipes.Tooltip")]
    public bool filterRecipes;


    public override ConfigScope Mode => ConfigScope.ClientSide;
#nullable disable
    public static InventoryManagement Instance;
#nullable restore
}