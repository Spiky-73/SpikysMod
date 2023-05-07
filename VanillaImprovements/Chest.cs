using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria;
using Terraria.ModLoader;
using static Terraria.GameContent.ItemDropRules.Conditions;
using Terraria.ID;
using System.Text;
using Terraria.Localization;
using System.Linq;
using Terraria.UI;

namespace SPYM.VanillaImprovements;


public static class Chests {

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

    public static bool DepositedFavItem { get; private set; }
    public static void OnSlotLeftClick() => DepositedFavItem = Main.mouseItem.favorited;
    public static void OnItemTranfer(ItemSlot.ItemTransferInfo info) => DepositedFavItem &= info.FromContenxt == 21 && info.ToContext.InRange(0, 4);


    private record struct BannerRecipe(int ItemID, int Stack, int Tile, int BannerCount);
}