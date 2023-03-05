using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public class ServerConfig : ModConfig {
    [DefaultValue(true), Label($"${Localization.Keys.ServerConfig}.frozenBuffs.Label"), Tooltip($"${Localization.Keys.ServerConfig}.frozenBuffs.Tooltip")]
    public bool frozenBuffs;
    [ReloadRequired, DefaultValue(true), Label($"${Localization.Keys.ServerConfig}.bannerRecipes.Label"), Tooltip($"${Localization.Keys.ServerConfig}.bannerRecipes.Tooltip")]
    public bool bannerRecipes;
    [ReloadRequired, DefaultValue(0.25f), Label($"${Localization.Keys.ServerConfig}.bannerRarity.Label"), Tooltip($"${Localization.Keys.ServerConfig}.bannerRarity.Tooltip")]
    public float bannerRarity;
    [DefaultValue(true), Label($"${Localization.Keys.ServerConfig}.bannerBuff.Label"), Tooltip($"${Localization.Keys.ServerConfig}.bannerBuff.Tooltip")]
    public bool bannerBuff;
    [ReloadRequired, DefaultValue(true), Label($"${Localization.Keys.ServerConfig}.infoAccPlus.Label"), Tooltip($"${Localization.Keys.ServerConfig}.infoAccPlus.Tooltip")]
    public bool infoAccPlus;
    [DefaultValue(true), Label($"${Localization.Keys.ServerConfig}.betterPeaceCandle.Label"), Tooltip($"${Localization.Keys.ServerConfig}.betterPeaceCandle.Tooltip")]
    public bool betterCalming;
    [DefaultValue(true), Label($"${Localization.Keys.ServerConfig}.favoriteItemsInChest.Label"), Tooltip($"${Localization.Keys.ServerConfig}.favoriteItemsInChest.Tooltip")]
    public bool favoritedItemsInChest;


    public override ConfigScope Mode => ConfigScope.ServerSide;
#nullable disable
    public static ServerConfig Instance;
#nullable restore
}