using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public class ClientConfig : ModConfig {


    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.SmartConsume"), Tooltip("$Mods.SPYM.Configs.Client.t_smartConsume")]
    public bool smartConsume;
    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.SmartAmmo"), Tooltip("$Mods.SPYM.Configs.Client.t_smartAmmo")]
    public bool smartAmmo;


    public override ConfigScope Mode => ConfigScope.ClientSide;
#nullable disable
    public static ClientConfig Instance;
#nullable restore
}