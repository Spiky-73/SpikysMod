using System.ComponentModel;
using System.Reflection;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace SPYM.Configs;

public class VanillaImprovements : ModConfig {

    [DefaultValue(true)] public bool frozenBuffs;
    [DefaultValue(true)] public bool betterCalming;
    [DefaultValue(true)] public bool bannerBuff;

    [ReloadRequired, DefaultValue(true)] public bool infoAccPlus;

    [DefaultValue(true)] public bool favoriteItemsInChest;

    [ReloadRequired, DefaultValue(true)] public bool bannerRecipes;
    [ReloadRequired, DefaultValue(0.25f)] public float bannerRarity;
    [ReloadRequired, Range(0.1f, 2f), DefaultValue(0.9f)] public float bannerValue;

    public override void OnChanged() {
        bool[] canFavoriteAt = (bool[])typeof(ItemSlot).GetField("canFavoriteAt", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        canFavoriteAt[3] = favoriteItemsInChest;
        canFavoriteAt[4] = favoriteItemsInChest;
    }


    public override ConfigScope Mode => ConfigScope.ServerSide;
#nullable disable
    public static VanillaImprovements Instance;
#nullable restore
}