using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace SPYM {
    public class Utility {

        public static bool IsEquiped(int type, Player player){
            foreach(Item i in player.armor){
                if(i.type == type) return true;
            }
            return false;
        }
        // Returns the last non held stack of a specified item from the player's inventory
        // Returns null if no stack satisfiy the above
        public static Item LastStack(Item item, Player player, bool notArg = false) {
            for (int i = player.inventory.Length - 1; i >= 0 ; i--){
                if(item.type == player.inventory[i].type &&
                  (!notArg || player.inventory[i] != item))
                    return player.inventory[i];
            }
            return null;
        }
        public static Item SmallestStack(Item item, Player player, bool notArg = false) {
            Item currentMin = null;
            for (int i = player.inventory.Length - 1; i >= 0 ; i--){
                if(item.type == player.inventory[i].type &&
                  (currentMin == null || player.inventory[i].stack < currentMin.stack) &&
                  (!notArg || player.inventory[i] != item))
                    currentMin = player.inventory[i];
            }

            return currentMin;
        }

        public static bool BossAlive() {
            foreach (NPC npc in Main.npc){
                if(npc.active && npc.boss) return true;
            }
            return false;
        }
    }

}