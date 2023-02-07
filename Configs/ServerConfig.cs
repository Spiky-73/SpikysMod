using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public class ServerConfig : ModConfig {
    [DefaultValue(true), Label($"${LocKeys.ServerConfig}.frozenBuffs.Label"), Tooltip($"${LocKeys.ServerConfig}.frozenBuffs.Tooltip")]
    public bool frozenBuffs;
    [ReloadRequired, DefaultValue(true), Label($"${LocKeys.ServerConfig}.bannerRecipes.Label"), Tooltip($"${LocKeys.ServerConfig}.bannerRecipes.Tooltip")]
    public bool bannerRecipes;
    [ReloadRequired, DefaultValue(0.25f), Label($"${LocKeys.ServerConfig}.bannerRarity.Label"), Tooltip($"${LocKeys.ServerConfig}.bannerRarity.Tooltip")]
    public float bannerRarity;
    [DefaultValue(true), Label($"${LocKeys.ServerConfig}.bannerBuff.Label"), Tooltip($"${LocKeys.ServerConfig}.bannerBuff.Tooltip")]
    public bool bannerBuff;
    [ReloadRequired, DefaultValue(true), Label($"${LocKeys.ServerConfig}.infoAccPlus.Label"), Tooltip($"${LocKeys.ServerConfig}.infoAccPlus.Tooltip")]
    public bool infoAccPlus;
    [DefaultValue(true), Label($"${LocKeys.ServerConfig}.betterPeaceCandle.Label"), Tooltip($"${LocKeys.ServerConfig}.betterPeaceCandle.Tooltip")]
    public bool betterCalming;
    [DefaultValue(true), Label($"${LocKeys.ServerConfig}.favoriteItemsInChest.Label"), Tooltip($"${LocKeys.ServerConfig}.favoriteItemsInChest.Tooltip")]
    public bool favoritedItemsInChest;


    public override ConfigScope Mode => ConfigScope.ServerSide;
#nullable disable
    public static ServerConfig Instance;
#nullable restore
}