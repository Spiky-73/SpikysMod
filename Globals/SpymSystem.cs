using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;

namespace SPYM.Globals;

public class SpymSystem : ModSystem {


    public override void Load() {
        On.Terraria.Main.UpdateTime += HookUpdateTime;

        On.Terraria.Main.UpdateTime_StartDay += HookUpdateTime_StartDay;
        On.Terraria.Main.UpdateTime_StartNight += HookUpdateTime_StartNight;

        IL.Terraria.Recipe.FindRecipes += ILFindRecipes;
        On.Terraria.Main.TryAllowingToCraftRecipe += HookTryAllowingToCraftRecipe;

        On.Terraria.Main.DamageVar += HookDamageVar;
        On.Terraria.Utilities.UnifiedRandom.Next_int += HookRngNext_int;
    }


    private static bool _forcedSeassons;
    private void HookUpdateTime(On.Terraria.Main.orig_UpdateTime orig) {
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

    private static void HookUpdateTime_StartNight(On.Terraria.Main.orig_UpdateTime_StartNight orig, ref bool stopEvents) {
        AlterEventChance();
        orig(ref stopEvents);
        AlteredRngRates = null;
    }
    private static void HookUpdateTime_StartDay(On.Terraria.Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
        AlterEventChance();
        orig(ref stopEvents);
        AlteredRngRates = null;
    }
    private static void AlterEventChance(){
        switch (Main.netMode) {
        case NetmodeID.SinglePlayer:
            AlteredRngRates = Main.LocalPlayer.GetModPlayer<SpymPlayer>().eventsBoost;
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
        if(Configs.VanillaImprovements.Instance.bannerRecipes) VanillaImprovements.Chests.AddBannerRecipes();
    }
    public override void PostAddRecipes() {
        if (Configs.VanillaImprovements.Instance.infoAccPlus) VanillaImprovements.InfoAccessories.EditRecipes();
    }


    private static void ILFindRecipes(ILContext il) {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(6))){
            SpikysMod.Instance.Logger.Error("Recipe filtering hook could not be applied. This feature has been automaticaly disabled");
            return;
        }
        cursor.Emit(OpCodes.Ldloc_S, (byte)6);
        cursor.EmitDelegate((Dictionary<int, int> materials) => {
            if (Configs.InventoryManagement.Instance.filterRecipes) InventoryManagement.Chests.AddCratingMaterials(materials);
        });

        MethodInfo recipeAvailableMethod = typeof(RecipeLoader).GetMethod(nameof(RecipeLoader.RecipeAvailable), BindingFlags.Public | BindingFlags.Static, new System.Type[]{typeof(Recipe)})!;
        cursor.GotoNext(MoveType.After, i => i.MatchCall(recipeAvailableMethod));
        cursor.Emit(OpCodes.Ldloc_S, (byte)13);
        cursor.EmitDelegate((bool available, int n) => available && (!Configs.InventoryManagement.Instance.filterRecipes || !InventoryManagement.Chests.HideRecipe(Main.recipe[n])));
    }

    private static bool HookTryAllowingToCraftRecipe(On.Terraria.Main.orig_TryAllowingToCraftRecipe orig, Recipe currentRecipe, bool tryFittingItemInInventoryToAllowCrafting, out bool movedAnItemToAllowCrafting)
        => orig(currentRecipe, Configs.InventoryManagement.Instance.filterRecipes || tryFittingItemInInventoryToAllowCrafting, out movedAnItemToAllowCrafting);


    public static bool FixedDamage { get; set; }
    private static int HookDamageVar(On.Terraria.Main.orig_DamageVar orig, float dmg, float luck) {
        if (FixedDamage) return orig(dmg, luck);
        return (int)System.MathF.Round(dmg);
    }

    public static float? AlteredRngRates { get; set; }
    private static int HookRngNext_int(On.Terraria.Utilities.UnifiedRandom.orig_Next_int orig, Terraria.Utilities.UnifiedRandom self, int maxValue) {
        if (AlteredRngRates.HasValue) return orig(self, Utility.AlterRate(maxValue, AlteredRngRates.Value));
        return orig(self, maxValue);
    }
}