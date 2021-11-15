using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYM.NPCs {
	class SpyNPC : GlobalNPC{

		public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
			int level = player.GetModPlayer<SpyPlayer>().radarLevel;
			if (level == 1) {
				spawnRate = (int)(spawnRate / 3 );
				maxSpawns = (int)(maxSpawns * 3 );
			}
		}

		public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)  {
			int level = spawnInfo.player.GetModPlayer<SpyPlayer>().analyserLevel;
			NPC npc = new NPC();
			//foreach(int type in pool.Keys){
			//	npc.SetDefaults(type);
			//	if (npc.rarity == 0) pool[type] = 0;
			//}
		}
	}
}
