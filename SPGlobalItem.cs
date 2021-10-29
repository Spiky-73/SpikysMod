
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYM {
    
    public class SPGlobalItem : GlobalItem {

        public override void SetDefaults(Item item){

        }
        public override bool UseItem(Item item, Player player){
            switch (item.type){
            case ItemID.GoldWatch: case ItemID.PlatinumWatch:
                Main.time += 59; // 60
            break;
            default:
                return false;
            }
            return true;
        }

        public override void OnConsumeItem(Item item, Player player) {
            ClientConfig config = ModContent.GetInstance<ClientConfig>();
            if(config.smartConsume){
                Item smallestStack = Utility.SmallestStack(item, player, true);
                if(smallestStack == null) return;
                item.stack++;
                smallestStack.stack--;
            }
        }
        
        public override void OnConsumeAmmo(Item ammo, Player player) {
            ClientConfig config = ModContent.GetInstance<ClientConfig>();
            if(config.smartAmmo && ammo.consumable){
                Item smallestAmmo = Utility.SmallestStack(ammo, player, true);
                if(smallestAmmo == null) return;
                ammo.stack++;
                smallestAmmo.stack--;
            }
        }
    }
}

