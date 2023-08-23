using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

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
        if(Configs.VanillaImprovements.Instance.bannerRecipes) VanillaImprovements.Chests.AddBannerRecipes();
    }
    public override void PostAddRecipes() {
        if (Configs.VanillaImprovements.Instance.infoAccPlus) VanillaImprovements.InfoAccessories.EditRecipes();
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
}