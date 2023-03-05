using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SPYM.Globals;

public class SpymItem : GlobalItem {

    public override void SetDefaults(Item item) {
        if (Configs.ServerConfig.Instance.infoAccPlus) VanillaImprovements.InfoAccessories.SetDefaults(item);
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (Configs.ServerConfig.Instance.infoAccPlus) VanillaImprovements.InfoAccessories.ModifyTooltips(item, tooltips);
    }

    public override bool CanUseItem(Item item, Player player) {
        if (Configs.ServerConfig.Instance.infoAccPlus && !VanillaImprovements.InfoAccessories.CanUseItem(item)) return false;
        return true;
    }
    public override bool AltFunctionUse(Item item, Player player) {
        if (Configs.ServerConfig.Instance.infoAccPlus && VanillaImprovements.InfoAccessories.AltFunctionUse(item)) return true;
        return false;
    }

    public override bool? UseItem(Item item, Player player){
        bool? res = null;
        if(player.altFunctionUse != 2){
            if (Configs.ServerConfig.Instance.infoAccPlus) res = (res ?? true) & VanillaImprovements.InfoAccessories.UseItem_Use(item, player);
        }else {
            if (Configs.ServerConfig.Instance.infoAccPlus) res = (res ?? true) & VanillaImprovements.InfoAccessories.UseItem_Alt(item);
        }
        return res;
    }

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame) {
        if (Configs.ServerConfig.Instance.infoAccPlus) VanillaImprovements.InfoAccessories.UseStyle(item, player);
    }

    public override void UpdateEquip(Item item, Player player) {
        if (Configs.ServerConfig.Instance.infoAccPlus) VanillaImprovements.InfoAccessories.UpdateEquip(item, player);
    }

    public override void OnConsumeItem(Item item, Player player) {
        if (Configs.ClientConfig.Instance.smartConsumption) InventoryFeatures.Items.OnConsume(item, player);
    }
    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.ClientConfig.Instance.smartAmmo) InventoryFeatures.Items.OnConsume(ammo, player, true);
    }
}


