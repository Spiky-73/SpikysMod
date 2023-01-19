using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public class ServerConfig : ModConfig {

    [DefaultValue(true), Label("$Mods.SPYM.Configs.Server.FrozenBuffs"), Tooltip("$Mods.SPYM.Configs.Server.t_frozenBuffs")]
    public bool frozenBuffs;
    [ReloadRequired, DefaultValue(true), Label("$Mods.SPYM.Configs.Server.BannerRecipes"), Tooltip("$Mods.SPYM.Configs.Server.t_bannerRecipes")]
    public bool bannerRecipes;
    [ReloadRequired, DefaultValue(0.25f), Label("$Mods.SPYM.Configs.Server.BannerRarity"), Tooltip("$Mods.SPYM.Configs.Server.t_bannerRarity")]
    public float bannerRarity;
    [DefaultValue(true), Label("$Mods.SPYM.Configs.Server.BannerBuff"), Tooltip("$Mods.SPYM.Configs.Server.t_bannerBuff")]
    public bool bannerBuff;
    [ReloadRequired, DefaultValue(true), Label("$Mods.SPYM.Configs.Server.InfoAccPlus"), Tooltip("$Mods.SPYM.Configs.Server.t_infoAccPlus")]
    public bool infoAccPlus;
    [DefaultValue(true), Label("$Mods.SPYM.Configs.Server.BetterPeaceCandle"), Tooltip("$Mods.SPYM.Configs.Server.t_betterPeaceCandle")]
    public bool betterCalming;


    public override ConfigScope Mode => ConfigScope.ServerSide;
#nullable disable
    public static ServerConfig Instance;
#nullable restore
}