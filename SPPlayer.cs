using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
namespace SPYM {

    public class SPPlayer : ModPlayer {

        private static bool [] defaultNoDisplay;
        private static bool bossDiffState;

        private bool adrenaline;

        public SPPlayer (){
            bossDiffState = false;
            adrenaline = false;
            // saves the value of the buffs
            if(defaultNoDisplay == null) {
                defaultNoDisplay = new bool[BuffLoader.BuffCount];
                Main.buffNoTimeDisplay.CopyTo(defaultNoDisplay, 0);
            }
            
        }

        public override void PlayerDisconnect(Player player){
            defaultNoDisplay.CopyTo(Main.buffNoTimeDisplay, 0);
        }

        public override void ResetEffects(){
            adrenaline = Utility.IsEquiped(ModContent.ItemType<Adrenaline>(), player);
        }

        public override void PreUpdateBuffs() {

            if(Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts() || adrenaline){
                // if(bossDiffState == false) player.QuickBuff();
                for (int i = 0; i < player.buffType.Length; i++){
                    int type = player.buffType[i];
                    if(Main.debuff[type] || defaultNoDisplay[type]) continue;
                    Main.buffNoTimeDisplay[type] = true;
                    player.buffTime[i]++; // freeze the buff time
                }
                bossDiffState = true;
            }else if(/*!Utility.BossOrEvent() && */bossDiffState){
                for (int i = 0; i < player.buffType.Length; i++){
                    int type = player.buffType[i];
                    if(Main.debuff[type] || defaultNoDisplay[type]) continue;
                    Main.buffNoTimeDisplay[type] = defaultNoDisplay[type];
                }
                bossDiffState = false;
            }

        }
    }
}