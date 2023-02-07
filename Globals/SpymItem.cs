using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using System.Reflection;

namespace SPYM.Globals;

public class SpymItem : GlobalItem {

    public override void Load() {
        // TODO refactor
        bool[] canFavoriteAt = (bool[])typeof(ItemSlot).GetField("canFavoriteAt", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        canFavoriteAt[3] = true;

        On.Terraria.UI.ChestUI.LootAll += HookLootAll;
        On.Terraria.UI.ChestUI.Restock += HookRestock;

    }

    // TODO refactor
    private static void HookRestock(On.Terraria.UI.ChestUI.orig_Restock orig) => Utility.RunWithHiddenItems(Main.LocalPlayer.Chest()!, i => i.favorited, () => orig());
    private static void HookLootAll(On.Terraria.UI.ChestUI.orig_LootAll orig) => Utility.RunWithHiddenItems(Main.LocalPlayer.Chest()!, i => i.favorited, () => orig());

    public override void SetDefaults(Item item) {
        if (ImprovedInfoAcc.Enabled) ImprovedInfoAcc.SetDefaults(item);
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (ImprovedInfoAcc.Enabled) ImprovedInfoAcc.ModifyTooltips(item, tooltips);
    }

    public override bool CanUseItem(Item item, Player player) {
        if (ImprovedInfoAcc.Enabled && !ImprovedInfoAcc.CanUseItem(item)) return false;
        return true;
    }
    public override bool AltFunctionUse(Item item, Player player) {
        if (ImprovedInfoAcc.Enabled && ImprovedInfoAcc.AltFunctionUse(item)) return true;
        return false;
    }

    public override bool? UseItem(Item item, Player player){
        bool? res = null;
        if(player.altFunctionUse != 2){
            if (ImprovedInfoAcc.Enabled) res = (res ?? true) & ImprovedInfoAcc.UseItem_Use(item, player);
        }else {
            if (ImprovedInfoAcc.Enabled) res = (res ?? true) & ImprovedInfoAcc.UseItem_Alt(item);
        }
        return res;
    }

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame) {
        if (ImprovedInfoAcc.Enabled) ImprovedInfoAcc.UseStyle(item, player);
    }

    public override void UpdateEquip(Item item, Player player) {
        if (ImprovedInfoAcc.Enabled) ImprovedInfoAcc.UpdateEquip(item, player);
    }

    public override void OnConsumeItem(Item item, Player player) {
        if (InventoryFeatures.SmartConsumption) InventoryFeatures.SmartConsume(item, player);
    }
    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (InventoryFeatures.SmartAmmo) InventoryFeatures.SmartConsume(ammo, player, true);
    }
}


