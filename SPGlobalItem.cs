using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Terraria;
using Terraria.ObjectData;
using Terraria.ModLoader;
using Terraria.ID;

namespace SPYM {
    
    public class InfiniteConsumables : GlobalItem {

        public override void OnConsumeItem(Item item, Player player) {
            Item smallestStack = Utility.SmallestStack(item, player, true);
            if(smallestStack == null) return;
            item.stack++;
            smallestStack.stack--;
        }

        public override void OnConsumeAmmo(Item ammo, Player player) {
            if(!ammo.consumable) return;
            Item smallestAmmo = Utility.SmallestStack(ammo, player, true);
            if(smallestAmmo == null) return;
            ammo.stack++;
            smallestAmmo.stack--;
        }
    }
}

