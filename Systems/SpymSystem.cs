using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYM.Systems;

public class SpymSystem : ModSystem {

    public override void Load(){
        On.Terraria.Recipe.FindRecipes += HookFindRecipes;
        On.Terraria.Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
    }

    private static bool HookTryAllowingToCraftRecipe(On.Terraria.Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        return orig(currentRecipe, true, out movedAnItemToAllowCrafting);
    }

    public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate) {
        // // TODO multiplayer
        float mult = Main.LocalPlayer.GetModPlayer<Globals.SpymPlayer>().timeWarp;
        timeRate *= mult;
        tileUpdateRate *= mult;
        eventUpdateRate *= mult;
    }

    public override void PostAddRecipes() {
        foreach (Recipe recipe in Main.recipe) {
            recipe.AddCondition(Terraria.Localization.NetworkText.Empty, ShowWithFilters);
            if (recipe.createItem.type != ItemID.CellPhone || recipe.requiredItem.Find(i => i.type == ItemID.PDA) == null) continue;
            recipe.requiredItem.Add(new(ItemID.PotionOfReturn, 15));
            recipe.requiredItem.Add(new(ItemID.WormholePotion, 15));
        }

    }

    private void HookFindRecipes(On.Terraria.Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        Item[] inv = Main.LocalPlayer.inventory;
        int slot = -1;
        Item? replaced = null;
        if(!Main.mouseItem.IsAir){
            for (int i = 0; i < 58; i++){
                if(!inv[i].IsAir) continue;
                slot = i;
                replaced = inv[slot];
                inv[slot] = Main.mouseItem;
                break;
            }
            if(slot == -1){
                for (int i = 0; i < 58; i++) {
                    if (inv[i].material) continue;
                    slot = i;
                    replaced = inv[slot];
                    inv[slot] = Main.mouseItem;
                    break;
                }
            }
        }
        orig(canDelayCheck);
        if(slot != -1){
            if(replaced != null) inv[slot] = replaced;
            else inv[slot].TurnToAir();
        }
    }


    public static bool ShowWithFilters(Recipe recipe) {
        if(Main.mouseItem.IsAir) return true;
        int type = Main.mouseItem.type;
        if(recipe.createItem.type == type) return true;
        if(recipe.requiredItem.Find(i => i.type == type) != null) return true;
        if(recipe.requiredTile.Contains(Main.mouseItem.createTile)) return true;

        foreach(int groups in recipe.acceptedGroups){
            if(RecipeGroup.recipeGroups[groups].ContainsItem(type)) return true;
        }
        return false;
    }
}