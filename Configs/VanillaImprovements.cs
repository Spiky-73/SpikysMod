using System.ComponentModel;
using System.Reflection;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace SPYM.Configs;

public class VanillaImprovements : ModConfig {

    [DefaultValue(true), Label($"${Localization.Keys.VanillaImprovements}.frozenBuffs.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.frozenBuffs.Tooltip")]
    public bool frozenBuffs;
    [DefaultValue(true), Label($"${Localization.Keys.VanillaImprovements}.betterPeaceCandle.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.betterPeaceCandle.Tooltip")]
    public bool betterCalming;
    [DefaultValue(true), Label($"${Localization.Keys.VanillaImprovements}.bannerBuff.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.bannerBuff.Tooltip")]
    public bool bannerBuff;

    [ReloadRequired, DefaultValue(true), Label($"${Localization.Keys.VanillaImprovements}.infoAccPlus.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.infoAccPlus.Tooltip")]
    public bool infoAccPlus;

    [DefaultValue(true), Label($"${Localization.Keys.VanillaImprovements}.favoriteItemsInChest.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.favoriteItemsInChest.Tooltip")]
    public bool favoritedItemsInChest;

    [ReloadRequired, DefaultValue(true), Label($"${Localization.Keys.VanillaImprovements}.bannerRecipes.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.bannerRecipes.Tooltip")]
    public bool bannerRecipes;
    [ReloadRequired, DefaultValue(0.25f), Label($"${Localization.Keys.VanillaImprovements}.bannerRarity.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.bannerRarity.Tooltip")]
    public float bannerRarity;
    [ReloadRequired, Range(0.1f, 2f), DefaultValue(0.9f), Label($"${Localization.Keys.VanillaImprovements}.bannerValue.Label"), Tooltip($"${Localization.Keys.VanillaImprovements}.bannerValue.Tooltip")]
    public float bannerValue;

    public override void OnChanged() {
        bool[] canFavoriteAt = (bool[])typeof(ItemSlot).GetField("canFavoriteAt", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        canFavoriteAt[3] = favoritedItemsInChest;
        canFavoriteAt[4] = favoritedItemsInChest;
    }


    public override ConfigScope Mode => ConfigScope.ServerSide;
#nullable disable
    public static VanillaImprovements Instance;
#nullable restore
}