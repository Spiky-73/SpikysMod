using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYM.NPCs {
	class SpyNPC : GlobalNPC{

		public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
			int level = player.GetModPlayer<SpyPlayer>().radarLevel;
			switch (level) {
			case 1:
				spawnRate = (int)(spawnRate / 2 );
				maxSpawns = (int)(maxSpawns * 2 );
				break;
			case 2:
				spawnRate = (int)(spawnRate / 5);
				maxSpawns = (int)(maxSpawns * 5);
				break;
			case 3:
				spawnRate = (int)(spawnRate / 10);
				maxSpawns = (int)(maxSpawns * 10);
				break;
			}
		}

		//public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) {

		//}
		//public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)  {
		//	int level = spawnInfo.player.GetModPlayer<SpyPlayer>().analyserLevel;
		//}
	}
}
