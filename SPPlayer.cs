using Terraria;
using Terraria.ModLoader;
using Terraria.GameInput;

namespace SPYM {

    public class SPPlayer : ModPlayer {

        private static bool [] defaultNoDisplay;
        private static bool bossDiffState;

        private bool adrenaline;

        private bool releasedSwap;

        public SPPlayer (){
            bossDiffState = false;
            adrenaline = false;
            releasedSwap = true;
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
            ClientConfig config = ModContent.GetInstance<ClientConfig>();
            if((config.frozenBuffs && (Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts())) || adrenaline){
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
        public override void ProcessTriggers(TriggersSet triggersSet){
            if(Main.playerInventory && Main.mouseItem.Name != ""){
                if(triggersSet.Hotbar1) { if(releasedSwap){
                    SwapHeld(0);
                } } 
                else if(triggersSet.Hotbar2) { if(releasedSwap){
                    SwapHeld(1);
                } } 
                else if(triggersSet.Hotbar3) { if(releasedSwap){
                    SwapHeld(2);
                } } 
                else if(triggersSet.Hotbar4) { if(releasedSwap){
                    SwapHeld(3);
                } } 
                else if(triggersSet.Hotbar5) { if(releasedSwap){
                    SwapHeld(4);
                } } 
                else if(triggersSet.Hotbar6) { if(releasedSwap){
                    SwapHeld(5);
                } } 
                else if(triggersSet.Hotbar7) { if(releasedSwap){
                    SwapHeld(6);
                } } 
                else if(triggersSet.Hotbar8) { if(releasedSwap){
                    SwapHeld(7);
                } } 
                else if(triggersSet.Hotbar9) { if(releasedSwap){
                    SwapHeld(8);
                } } 
                else if(triggersSet.Hotbar10) { if(releasedSwap){
                    SwapHeld(9);
                } } 
                else releasedSwap = true;
            }
        }

        private void SwapHeld(int itemIndex){
            releasedSwap = false;
            Item temp = player.inventory[itemIndex];
            player.inventory[itemIndex] = player.HeldItem;
            for (int i = player.inventory.Length - 1; i >= 0 ; i--){
                if(player.inventory[i].Name != "") continue;
                Item temp2 = player.inventory[i];
                player.inventory[i] = temp;
                Main.mouseItem = temp2;
                return;
            }
            Main.mouseItem = temp;

        }
    }

}