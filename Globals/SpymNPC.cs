using Terraria;
using Terraria.ModLoader;

namespace SPYM.Globals;
class SpymNPC : GlobalNPC {

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
        float mult = player.GetModPlayer<SpymPlayer>().spawnRateBoost;
        spawnRate = (int)(spawnRate*mult);
        maxSpawns = (int)(spawnRate*mult);
    }
}

