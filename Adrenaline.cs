using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYM {
    
	public class Adrenaline : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Freezes the duration of buffs");
        }

        public override void SetDefaults() {
            item.width = 20;
            item.height = 20;
            item.maxStack = 1;
            item.uniqueStack = true;
            item.accessory = true;
            item.rare = ItemRarityID.Orange;
            item.value = Item.buyPrice(gold: 50);
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<SPPlayer>().adrenaline = true;
        }
    }
}