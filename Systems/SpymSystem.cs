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
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;

namespace SPYM.Systems;

public class SpymSystem : ModSystem {

    public static bool ForcedSeassons { get; private set; }
    public static bool InUpdateTime { get; private set; }


    public override void Load() {
        IL.Terraria.Recipe.FindRecipes += ILFindRecipes;
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
            orig();
            return;
        }
        if(ForcedSeassons){
            Main.checkXMas();
            Main.checkHalloween();
            ForcedSeassons = false;
        }
        orig();
    }

    private static bool HookTryAllowingToCraftRecipe(On.Terraria.Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, InventoryFeatures.FilterRecipes || tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);

    public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate) {
        float mult;
        switch (Main.netMode) {
        case NetmodeID.SinglePlayer:
            mult = Main.LocalPlayer.GetModPlayer<Globals.SpymPlayer>().timeMult;
            break;
        case NetmodeID.Server: // TODO multiplayer
            mult = 1f;
            // int total = 0;
            // SortedDictionary<float, int> mults = new(new Utility.DescendingComparer<float>());
            // foreach(Player player in Main.player){
            //     if(!player.active || player.DeadOrGhost) continue;
            //     float m = player.GetModPlayer<Globals.SpymPlayer>().timeMult;
            //     mults[m] = mults.GetValueOrDefault(m)+1;
            //     total++;
            // }
            // mult = 1f;
            // int count = 0;
            // foreach ((float m, int c) in mults) {
            //     count += c;
            //     if(c < total/2) continue;
            //     mult = m;
            //     break;
            // }
            break;
        default:
            return;
        }

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
                return Language.GetTextValue($"{LocKeys.RecipesGroups}.Banners.DisplayName", builder.ToString(), names[^1]);
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
        if (ImprovedInfoAcc.Enabled) ImprovedInfoAcc.PostAddRecipes();
    }

    private static void ILFindRecipes(ILContext il) {
        static void FilteringFail(){
            Configs.ClientConfig.Instance.filterRecipes = false;
            SpikysMod.Instance.Logger.Warn("Recipe filtering hook could not be applied. This feature has been automaticaly disabled");
        }

        ILCursor cursor = new(il);

        if (cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(6))) {
            cursor.Emit(OpCodes.Ldloc_S, (byte)6);
            cursor.EmitDelegate((Dictionary<int, int> materials) => {
                if(InventoryFeatures.FilterRecipes) InventoryFeatures.AddCratingMaterials(materials);
            });
        } else FilteringFail();

        MethodInfo recipeAvailableMethod = typeof(RecipeLoader).GetMethod(nameof(RecipeLoader.RecipeAvailable), BindingFlags.Public | BindingFlags.Static, new System.Type[]{typeof(Recipe)})!;
        if(cursor.TryGotoNext(MoveType.After, i => i.MatchCall(recipeAvailableMethod))){
            cursor.Emit(OpCodes.Ldloc_S, (byte)13);
            cursor.EmitDelegate((bool available, int n) => available && (!InventoryFeatures.FilterRecipes || !InventoryFeatures.HideRecipe(Main.recipe[n])));
        } else FilteringFail();
    }
}