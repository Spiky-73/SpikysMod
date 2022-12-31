using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYM.Items;

public class Adrenaline : ModItem {
    public override void SetStaticDefaults() {
        Tooltip.SetDefault("[Testing] Freezes the duration of buffs");
    }

    public override void SetDefaults() {
        Item.width = 20;
        Item.height = 20;
        Item.maxStack = 1;
        Item.accessory = true;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.buyPrice(gold: 50);
    }

    public override void UpdateAccessory(Player player, bool hideVisual) {
        player.GetModPlayer<Globals.SpymPlayer>().adrenaline = true;
    }
}
