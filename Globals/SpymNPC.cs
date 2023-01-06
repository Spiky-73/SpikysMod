using Terraria;
using Terraria.ModLoader;

namespace SPYM.Globals;
class SpymNPC : GlobalNPC {

    public static bool InDropItem { get; private set; }
    public static bool BannerBuff { get; private set; }
    public static Player? ClosestPlayer { get; private set; }

    public override void Load() {
        On.Terraria.NPC.NPCLoot_DropItems += HookDropItem;
        On.Terraria.Utilities.UnifiedRandom.Next_int += HookRngNext_int;
    }

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
        float mult = player.GetModPlayer<SpymPlayer>().spawnRateBoost;
        spawnRate = (int)(spawnRate*mult);
        maxSpawns = (int)(spawnRate*mult);
    }

    private static void HookDropItem(On.Terraria.NPC.orig_NPCLoot_DropItems orig, NPC self, Player closestPlayer) {
        InDropItem = true;
        ClosestPlayer = closestPlayer;
        int banner = Item.NPCtoBanner(self.BannerID());
        BannerBuff = closestPlayer.HasNPCBannerBuff(banner);
        orig(self, closestPlayer);
        InDropItem = false;
        ClosestPlayer = null;
    }

    private static int HookRngNext_int(On.Terraria.Utilities.UnifiedRandom.orig_Next_int orig, Terraria.Utilities.UnifiedRandom self, int maxValue) {
        if (!InDropItem || ClosestPlayer is null) return orig(self, maxValue);
        return orig(self, AlterDropRate(maxValue));
    }

    private static int AlterDropRate(int chanceDenominator) {
        float tallyMult = ClosestPlayer!.GetModPlayer<SpymPlayer>().tallyMult;
        if(BannerBuff) tallyMult += 0.1f;

        if (tallyMult <= 1f) return chanceDenominator;
        chanceDenominator = (int)System.MathF.Ceiling(System.MathF.Pow(2, System.MathF.Pow(System.MathF.Log2(chanceDenominator), 1/tallyMult)));
        return chanceDenominator;
    }
}

