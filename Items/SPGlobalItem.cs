
using Terraria;
using Terraria.ModLoader;

namespace SPYM {
    
    public class SPGlobalItem : GlobalItem {
        public override void SetDefaults(Item item){

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

        public static bool Equipable(Item item){
            return item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0 || item.accessory || Main.projHook[item.shoot] || item.mountType != -1 || (item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType]));
        }
    }
}

