using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using static Terraria.GameContent.ItemDropRules.Conditions;

namespace SPYM.Globals;

public class SpymSystem : ModSystem {


    public override void Load() {
        On_Main.UpdateTime += HookUpdateTime;

        On_Main.UpdateTime_StartDay += HookUpdateTime_StartDay;
        On_Main.UpdateTime_StartNight += HookUpdateTime_StartNight;

        On_Main.DamageVar_float_int_float += HookDamageVar;
        On_UnifiedRandom.Next_int += HookRngNext_int;
    }


    private static bool _forcedSeassons;
    private void HookUpdateTime(On_Main.orig_UpdateTime orig) {
        foreach(Player player in Main.player){
            if(!player.active || player.DeadOrGhost || !player.GetModPlayer<SpymPlayer>().forcedSeasons) continue;
            Main.halloween = true;
            Main.xMas = true;
            _forcedSeassons = true;
            orig();
            return;
        }
        if(_forcedSeassons){
            Main.checkXMas();
            Main.checkHalloween();
            _forcedSeassons = false;
        }
        orig();
    }

    private static void HookUpdateTime_StartNight(On_Main.orig_UpdateTime_StartNight orig, ref bool stopEvents) {
        AlterEventChance();
        orig(ref stopEvents);
        BoostedRngRates = null;
    }
    private static void HookUpdateTime_StartDay(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
        AlterEventChance();
        orig(ref stopEvents);
        BoostedRngRates = null;
    }
    private static void AlterEventChance(){
        switch (Main.netMode) {
        case NetmodeID.SinglePlayer:
            BoostedRngRates = Main.LocalPlayer.GetModPlayer<SpymPlayer>().eventsBoost;
            return;
        case NetmodeID.Server: // TODO multiplayer
            return;
        }
    }

    public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate) {
        float mult;
        switch (Main.netMode) {
        case NetmodeID.SinglePlayer:
            mult = Main.LocalPlayer.GetModPlayer<SpymPlayer>().timeMult;
            break;
        case NetmodeID.Server: // TODO multiplayer
            mult = 1f;
            break;
        default:
            return;
        }

        timeRate *= mult;
        tileUpdateRate *= mult;
        eventUpdateRate *= mult;
    }

    public override void AddRecipes() {
        if(Configs.VanillaImprovements.Instance.bannerRecipes) AddBannerRecipes();
    }

    public override void PostAddRecipes() {
        if (Configs.VanillaImprovements.Instance.infoAccPlus) {
            foreach (Recipe recipe in Main.recipe) {
                if (recipe.createItem.type != ItemID.CellPhone || recipe.requiredItem.Find(i => i.type == ItemID.PDA) == null) continue;
                recipe.requiredItem.Add(new(ItemID.PotionOfReturn, 15));
                recipe.requiredItem.Add(new(ItemID.WormholePotion, 15));
            }
        }
    }

    public static bool FixedDamage { get; set; }
    private static int HookDamageVar(On_Main.orig_DamageVar_float_int_float orig, float dmg, int percent, float luck) {
        if (!FixedDamage) return orig(dmg, percent, luck);
        return (int)System.MathF.Round(dmg);
    }

    public static float? BoostedRngRates { get; set; }
    private static int HookRngNext_int(On_UnifiedRandom.orig_Next_int orig, UnifiedRandom self, int maxValue) {
        if (BoostedRngRates.HasValue) return orig(self, Utility.BoostRate(maxValue, BoostedRngRates.Value)); // BUG negtive int (to replicate)
        return orig(self, maxValue);
    }

    public static void AddBannerRecipes() {
        Dictionary<int, Dictionary<int, DropRateInfo>> drops = new();
        for (int type = -65; type < NPCLoader.NPCCount - 65; type++) {
            int banner = Item.NPCtoBanner(type);
            if (banner <= 0) continue;
            Dictionary<int, DropRateInfo> bannerDrops = drops.TryGetValue(banner, out Dictionary<int, DropRateInfo>? bds) ? bds : drops[banner] = new();

            // TODO globals and yoyos
            foreach (IItemDropRule dropRule in Main.ItemDropsDB.GetRulesForNPCID(type, false)) {
                List<DropRateInfo> dropRates = new();
                dropRule.ReportDroprates(dropRates, new(1f));

                foreach (DropRateInfo drop in dropRates) {
                    if (drop.conditions?.Exists(c => !c.CanShowItemDropInUI()) == true) continue;
                    if (bannerDrops.TryGetValue(drop.itemId, out DropRateInfo d) && d.stackMax * d.dropRate < drop.stackMax * d.dropRate) continue;
                    bannerDrops[drop.itemId] = drop;
                }
            }
        }

        Dictionary<BannerRecipe, HashSet<int>> recipes = new();
        foreach ((int banner, Dictionary<int, DropRateInfo> bannerDrops) in drops) {
            foreach ((int item, DropRateInfo drop) in bannerDrops) {
                if (drop.dropRate > Configs.VanillaImprovements.Instance.bannerRarity) continue;
                int amount = (int)System.MathF.Ceiling((drop.stackMin - 1 + drop.stackMax) / 2f);

                int stack, mat;
                if (drop.dropRate >= 1 / 50f) {
                    stack = System.Math.Clamp((int)(amount * drop.dropRate * 50 * Configs.VanillaImprovements.Instance.bannerValue), 1, new Item(item).maxStack);
                    mat = 1;
                } else {
                    float count = 1f / drop.dropRate / (50f * Configs.VanillaImprovements.Instance.bannerValue);
                    if (count > 7f) count = 7f + System.MathF.Log2(count - 7f);
                    if (count > 10f) count = count.Snap(5f, Utility.SnapMode.Ceiling);
                    else count = System.MathF.Ceiling(count);
                    stack = amount;
                    mat = (int)count;
                }
                int tile;
                if (drop.conditions is null) tile = TileID.Solidifier;
                else if (drop.conditions.Exists(i => i is DownedPlantera or DownedAllMechBosses)) tile = TileID.LihzahrdFurnace;
                else if (drop.conditions.Exists(i => i is IsHardmode)) tile = TileID.Blendomatic;
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
                return Language.GetTextValue($"{Localization.Keys.RecipesGroups}.Banners.DisplayName", builder.ToString(), names[^1]);
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

    private record struct BannerRecipe(int ItemID, int Stack, int Tile, int BannerCount);
}