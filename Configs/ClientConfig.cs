using System.ComponentModel;

using Terraria.ModLoader.Config;

namespace SPYM.Configs;

public enum SmartPickupLevel {
    Off,
    FavoriteOnly,
    AllItems
}

public class ClientConfig : ModConfig {


    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.SmartConsume"), Tooltip("$Mods.SPYM.Configs.Client.t_smartConsume")]
    public bool smartConsume;
    [DefaultValue(true), Label("$Mods.SPYM.Configs.Client.SmartAmmo"), Tooltip("$Mods.SPYM.Configs.Client.t_smartAmmo")]
    public bool smartAmmo;
    [DefaultValue(SmartPickupLevel.FavoriteOnly), Label("$Mods.SPYM.Configs.Client.SmartPickup"), Tooltip("$Mods.SPYM.Configs.Client.t_smartPickup")]
    public SmartPickupLevel smartPickup;


    public override ConfigScope Mode => ConfigScope.ClientSide;
#nullable disable
    public static ClientConfig Instance;
#nullable restore
}