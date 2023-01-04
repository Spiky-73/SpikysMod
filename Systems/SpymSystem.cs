using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
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

    public override void AddRecipes() {
        Dictionary<int, HashSet<int>> addedRecipes = new();
        for (int type = 0; type < NPCLoader.NPCCount; type++){
            int banner = Item.NPCtoBanner(type);
            if(banner <= 0) continue;
            banner = Item.BannerToItem(banner);
            List<IItemDropRule> drops = Main.ItemDropsDB.GetRulesForNPCID(type);
            foreach(IItemDropRule drop in drops){
                List<DropRateInfo> dropRates = new();
                drop.ReportDroprates(dropRates,new(1f));
                foreach(DropRateInfo rate in dropRates){ // TODO >>> banner groups
                    if(addedRecipes.TryGetValue(banner, out HashSet<int>? crafts) && crafts.Contains(rate.itemId)) continue;
                    if(rate.dropRate > 0.5f || rate.conditions?.Exists(c => !c.CanShowItemDropInUI()) == true) continue;
                    float count = 1f/rate.dropRate / 50f;
                    if(count > 7f) count = 7f + MathF.Log2(count - 7f);
                    if(count > 10f) count = MathF.Ceiling(count / 5f) * 5f;
                    else count = MathF.Ceiling(count);
                    Recipe.Create(rate.itemId, (rate.stackMin + rate.stackMax) / 2) // TODO >>> crafting tile based on progression
                        .AddIngredient(banner, (int)count)
                        .Register();
                    if(!addedRecipes.ContainsKey(banner)) addedRecipes[banner] = new();
                    addedRecipes[banner].Add(rate.itemId);
                }
            }
        }
    }

    public override void PostAddRecipes() {
        foreach (Recipe recipe in Main.recipe) {
            if (recipe.createItem.type != ItemID.CellPhone || recipe.requiredItem.Find(i => i.type == ItemID.PDA) == null) continue;
            recipe.requiredItem.Add(new(ItemID.PotionOfReturn, 15));
            recipe.requiredItem.Add(new(ItemID.WormholePotion, 15));
        }

    }

    private void HookFindRecipes(On.Terraria.Recipe.orig_FindRecipes orig, bool canDelayCheck) {
        if (canDelayCheck || Main.mouseItem.IsAir) {
            orig(canDelayCheck);
            return;
        }
        
        Item[] inv = Main.LocalPlayer.inventory;

        (int slot, Item? replaced) = (-1, null);
        if((slot = Array.FindIndex(inv, i => !i.IsAir)) != -1 || (slot = Array.FindIndex(inv, i => !i.material)) != -1){
            replaced = inv[slot];
            inv[slot] = Main.mouseItem;
        }

        orig(canDelayCheck);
        if(slot == -1) return;

        if (replaced != null) inv[slot] = replaced;
        else inv[slot].TurnToAir();

        int filterType = Main.mouseItem.type;

        List<int> createsItem = new();
        List<int> requiresItem = new();
        List<int> other = new();

        int focus = Main.availableRecipe[Main.focusRecipe];
        for (int r = 0; r < Main.numAvailableRecipes; r++){
            Recipe recipe = Main.recipe[Main.availableRecipe[r]];
            if(recipe.createItem.type == filterType) createsItem.Add(Main.availableRecipe[r]);
            else if(recipe.requiredItem.Exists(i => i.type == filterType)) requiresItem.Add(Main.availableRecipe[r]);
            else other.Add(Main.availableRecipe[r]);
            Main.availableRecipe[r] = 0;
        }
        createsItem.CopyTo(Main.availableRecipe, 0);
        requiresItem.CopyTo(Main.availableRecipe, createsItem.Count);
        Main.numAvailableRecipes = createsItem.Count + requiresItem.Count;

        float oldYoffset = Main.availableRecipeY[Main.focusRecipe];
        Main.focusRecipe = Array.IndexOf(Main.availableRecipe, focus);
        if(Main.focusRecipe == -1) Main.focusRecipe = Math.Min(Main.numAvailableRecipes-1, focus);
        float dYOff = Main.availableRecipeY[Main.focusRecipe] - oldYoffset;
        for (int r = 0; r < Recipe.maxRecipes; r++) {
            Main.availableRecipeY[r] -= dYOff;
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