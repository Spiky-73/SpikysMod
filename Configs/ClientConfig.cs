using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public class ClientConfig : ModConfig {


    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.smartConsume"), Tooltip("$Mods.SPYM.Configs.Client.t_smartConsume")]
    public bool smartConsume;
    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.smartAmmo"), Tooltip("$Mods.SPYM.Configs.Client.t_smartAmmo ")]
    public bool smartAmmo;
    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.frozenBuffs"), Tooltip("$Mods.SPYM.Configs.Client.t_frozenBuffs")]
    public bool frozenBuffs;


    public override ConfigScope Mode => ConfigScope.ClientSide;
#nullable disable
    public static ClientConfig Instance;
#nullable restore
}