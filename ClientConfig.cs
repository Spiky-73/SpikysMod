using System.ComponentModel;

using Terraria;
using Terraria.ModLoader.Config;

namespace SPYM {

    public class ClientConfig : ModConfig {

        public override ConfigScope Mode => ConfigScope.ClientSide;
        
        [DefaultValue(true), Label("Smart item consumption"), Tooltip("Consumes items from the smallest stack inventory")]
        public bool smartConsume;
        [DefaultValue(true), Label("Smart ammo consumption"), Tooltip("Consumes items from the smallest stack inventory\nRequires 'Smart item Consumption' on ")]
        public bool smartAmmo;
        [DefaultValue(true), Label("Frozen buffs"), Tooltip("Freezes buffs duration during events or bosses")]
        public bool frozenBuffs;

    }

}