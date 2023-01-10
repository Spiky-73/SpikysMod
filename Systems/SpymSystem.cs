using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYM.Systems;

public class SpymSystem : ModSystem {

    public static bool ForcedSeassons { get; private set; }

    public override void Load(){
        On.Terraria.Recipe.FindRecipes += HookFindRecipes;
        On.Terraria.Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
        On.Terraria.Main.UpdateTime += HookUpdateTime;
    }

    private void HookUpdateTime(On.Terraria.Main.orig_UpdateTime orig) {
        foreach(Player player in Main.player){
            if(!player.active || player.DeadOrGhost || !player.GetModPlayer<Globals.SpymPlayer>().weatherRadio) continue;
            Main.halloween = true;
            Main.xMas = true;
            ForcedSeassons = true;
            goto success;
        }
        if(ForcedSeassons){
            Main.checkXMas();
            Main.checkHalloween();
            ForcedSeassons = false;
        }
    success:
        orig();
    }

    private static bool HookTryAllowingToCraftRecipe(On.Terraria.Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting) {
        return orig(currentRecipe, true, out movedAnItemToAllowCrafting);
    }

    public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate) {
        // TODO multiplayer
        float mult = Main.LocalPlayer.GetModPlayer<Globals.SpymPlayer>().timeWarp;
        timeRate *= mult;
        tileUpdateRate *= mult;
        eventUpdateRate *= mult;
    }

    public override void AddRecipes() {
        if(Configs.ServerConfig.Instance.bannerRecipes){
            Dictionary<int, HashSet<int>> addedRecipes = new();
            for (int type = 0; type < NPCLoader.NPCCount; type++) {
                int banner = Item.NPCtoBanner(type);
                if (banner <= 0) continue;
                banner = Item.BannerToItem(banner);
                List<IItemDropRule> drops = Main.ItemDropsDB.GetRulesForNPCID(type);
                foreach (IItemDropRule drop in drops) {
                    List<DropRateInfo> dropRates = new();
                    drop.ReportDroprates(dropRates, new(1f));
                    foreach (DropRateInfo rate in dropRates) { // TODO banner groups and mech summons
                        if (addedRecipes.TryGetValue(banner, out HashSet<int>? crafts) && crafts.Contains(rate.itemId)) continue;
                        if (rate.dropRate > 0.5f || rate.conditions?.Exists(c => !c.CanShowItemDropInUI()) == true) continue;
                        float count = 1f / rate.dropRate / 50f;
                        if (count > 7f) count = 7f + MathF.Log2(count - 7f);
                        if (count > 10f) count = MathF.Ceiling(count / 5f) * 5f;
                        else count = MathF.Ceiling(count);
                        Recipe.Create(rate.itemId, (rate.stackMin + rate.stackMax) / 2)
                            .AddIngredient(banner, (int)count)
                            .AddTile(TileID.TinkerersWorkbench) // TODO Progression scaling
                            .Register();
                        if (!addedRecipes.ContainsKey(banner)) addedRecipes[banner] = new();
                        addedRecipes[banner].Add(rate.itemId);
                    }
                }
            }
        }
    }

    public override void PostAddRecipes() {
        if (Configs.ServerConfig.Instance.infoAccPlus) {
            foreach (Recipe recipe in Main.recipe) {
                if (recipe.createItem.type != ItemID.CellPhone || recipe.requiredItem.Find(i => i.type == ItemID.PDA) == null) continue;
                recipe.requiredItem.Add(new(ItemID.PotionOfReturn, 15));
                recipe.requiredItem.Add(new(ItemID.WormholePotion, 15));
            }
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
        if(Main.focusRecipe == -1) Main.focusRecipe = Main.numAvailableRecipes <= 0 ? 0 : Math.Min(Main.numAvailableRecipes-1, focus);
        float dYOff = Main.availableRecipeY[Main.focusRecipe] - oldYoffset;
        for (int r = 0; r < Recipe.maxRecipes; r++) {
            Main.availableRecipeY[r] -= dYOff;
        }
    }
}