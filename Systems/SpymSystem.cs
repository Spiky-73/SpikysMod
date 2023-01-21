using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria.Localization;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.GameContent.ItemDropRules.Conditions;

namespace SPYM.Systems;

public class SpymSystem : ModSystem {

    public static bool ForcedSeassons { get; private set; }
    public static bool InUpdateTime { get; private set; }


    public override void Load() {
        On.Terraria.Recipe.FindRecipes += HookFindRecipes;
        On.Terraria.Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;
        On.Terraria.Main.UpdateTime += HookUpdateTime;
        On.Terraria.Main.UpdateTime_StartDay += HookUpdateTime_StartDay;
        On.Terraria.Main.UpdateTime_StartNight += HookUpdateTime_StartNight;
    }

    private static void HookUpdateTime_StartNight(On.Terraria.Main.orig_UpdateTime_StartNight orig, ref bool stopEvents) {
        InUpdateTime = true;
        orig(ref stopEvents);
        InUpdateTime = false;
    }

    private static void HookUpdateTime_StartDay(On.Terraria.Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
        InUpdateTime = true;
        orig(ref stopEvents);
        InUpdateTime = false;
    }

    private void HookUpdateTime(On.Terraria.Main.orig_UpdateTime orig) {
        foreach(Player player in Main.player){
            if(!player.active || player.DeadOrGhost || !player.GetModPlayer<Globals.SpymPlayer>().forcedSeasons) continue;
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

    private static bool HookTryAllowingToCraftRecipe(On.Terraria.Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, Configs.ClientConfig.Instance.filterRecipes || tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);

    public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate) {
        // TODO multiplayer
        float mult = Main.LocalPlayer.GetModPlayer<Globals.SpymPlayer>().timeMult;
        timeRate *= mult;
        tileUpdateRate *= mult;
        eventUpdateRate *= mult;
    }

    private record struct BannerRecipe(int ItemID, int Stack, int Tile, int BannerCount);

    public override void AddRecipes() {
        if(Configs.ServerConfig.Instance.bannerRecipes) AddBannerRecipes();
    }

    public void AddBannerRecipes() {
        Dictionary<int, Dictionary<int, DropRateInfo>> drops = new();
        for (int type = -65; type < NPCLoader.NPCCount - 65; type++) {
            int banner = Item.NPCtoBanner(type);
            if (banner <= 0) continue;
            Dictionary<int, DropRateInfo> bannerDrops = drops.TryGetValue(banner, out Dictionary<int, DropRateInfo>? bds) ? bds : drops[banner] = new();

            // TODO globals and yoyos
            foreach (IItemDropRule dropRule in Main.ItemDropsDB.GetRulesForNPCID(type, false)) {
                List<DropRateInfo> dropRates = new();
                dropRule.ReportDroprates(dropRates, new(1f));

                foreach(DropRateInfo drop in dropRates){
                    if (drop.conditions?.Exists(c => !c.CanShowItemDropInUI()) == true) continue;
                    if(bannerDrops.TryGetValue(drop.itemId, out DropRateInfo d) && d.stackMax*d.dropRate < drop.stackMax*d.dropRate) continue;
                    bannerDrops[drop.itemId] = drop;
                }
            }
        }

        Dictionary<BannerRecipe, HashSet<int>> recipes = new();
        foreach((int banner, Dictionary<int, DropRateInfo> bannerDrops) in drops) {
            foreach ((int item, DropRateInfo drop) in bannerDrops) {
                if (drop.dropRate > Configs.ServerConfig.Instance.bannerRarity) continue;
                int amount = (int)MathF.Ceiling((drop.stackMin-1 + drop.stackMax) / 2f);

                int stack, mat;
                if (drop.dropRate >= 1 / 50f) {
                    stack = Math.Min(new Item(item).maxStack, (int)(amount * drop.dropRate * 50));
                    mat = 1;
                }
                else {
                    float count = 1f / drop.dropRate / 50f;
                    if (count > 7f) count = 7f + MathF.Log2(count - 7f);
                    if (count > 10f) count = count.Snap(5f, Utility.SnapMode.Ceiling);
                    else count = MathF.Ceiling(count);
                    stack = amount;
                    mat = (int)count;
                }
                int tile;
                if(drop.conditions is null) tile = TileID.Solidifier;
                else if(drop.conditions.Exists(i => i is DownedPlantera or DownedAllMechBosses)) tile = TileID.LihzahrdFurnace;
                else if(drop.conditions.Exists(i => i is IsHardmode)) tile = TileID.Blendomatic;
                else tile = TileID.Solidifier;

                BannerRecipe br = new(item, stack, tile, mat);
                if (!recipes.ContainsKey(br)) recipes[br] = new();
                recipes[br].Add(banner);
            }
        }

        foreach ((BannerRecipe recipe, HashSet<int> banners) in recipes) {
            string Name() {
                StringBuilder builder = new();
                List<string> names = new(banners.Count);
                foreach (int banner in banners) names.Add(Lang.GetNPCNameValue(Item.BannerToNPC(banner)));
                for (int i = 0; i < banners.Count - 1; i++) {
                    builder.Append(names[i]);
                    if (i == banners.Count - 2) continue;
                    builder.Append(", ");
                    if (i % 4 == 3 || (i == banners.Count - 3 && i % 4 == 2)) builder.Append('\n');
                }
                return Language.GetTextValue("Mods.SPYM.Tooltips.bannerGroup", builder.ToString(), names[^1]);
            }
            Recipe r = Recipe.Create(recipe.ItemID, recipe.Stack).AddTile(recipe.Tile);
            if (banners.Count > 1) {
                List<int> bannerItems = new();
                foreach (int banner in banners) bannerItems.Add(Item.BannerToItem(banner));
                int group = RecipeGroup.RegisterGroup($"Banners {string.Join(", ", banners)}", new(Name, bannerItems.ToArray()));
                r.AddRecipeGroup(group, recipe.BannerCount);
            } else r.AddIngredient(Item.BannerToItem(banners.ToArray()[0]), recipe.BannerCount);
            r.Register();
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
        if (!Configs.ClientConfig.Instance.filterRecipes && canDelayCheck || Main.mouseItem.IsAir) {
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
            else if(recipe.acceptedGroups.Exists(g => RecipeGroup.recipeGroups[g].ContainsItem(filterType))) requiresItem.Add(Main.availableRecipe[r]);
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