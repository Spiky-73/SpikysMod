using Terraria;
using Terraria.ModLoader;
using Terraria.GameInput;
using Terraria.UI;

namespace SPYM {

    public class SPPlayer : ModPlayer {

        private static bool [] defaultNoDisplay;

        public bool adrenaline;

        private bool releasedSwap;

        public SPPlayer (){
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

        public override void PreUpdateBuffs() {
            ClientConfig config = ModContent.GetInstance<ClientConfig>();

            for (int i = 0; i < player.buffType.Length; i++){
                    int type = player.buffType[i];
                    if(Main.debuff[type] || defaultNoDisplay[type]) continue;
                    bool frozen = config.frozenBuffs && ((Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts()) || adrenaline);
                    if(frozen) {
                        Main.buffNoTimeDisplay[type] = true;
                        player.buffTime[i]++; // freeze the buff time
                    }   else {
                        Main.buffNoTimeDisplay[type] = defaultNoDisplay[type];
                    }
            }
            adrenaline = false; // Needs to be here because of update order
        }
        public override void ProcessTriggers(TriggersSet triggersSet){

            if(SPYM.FavoritedBuff.JustPressed){
                Item[] inv = player.inventory;
                bool[] changed = new bool[player.inventory.Length-1];
                for (int i = 0; i < player.inventory.Length-1; i++){
                    if(!inv[i].favorited && inv[i].stack > 0) inv[i].stack *= -1;;
                }
                player.QuickBuff();
                for (int i = 0; i < inv.Length-1; i++){
                    if(!inv[i].favorited && inv[i].stack < 0) inv[i].stack *= -1;;
                }
            }

            if(Main.playerInventory && Main.mouseItem.type != 0){
                if(triggersSet.Hotbar1) {
                    TrySwapHeld(0);
                }
                else if(triggersSet.Hotbar2) {
                    TrySwapHeld(1);
                }
                else if(triggersSet.Hotbar3) {
                    TrySwapHeld(2);
                }
                else if(triggersSet.Hotbar4) {
                    TrySwapHeld(3);
                }
                else if(triggersSet.Hotbar5) {
                    TrySwapHeld(4);
                }
                else if(triggersSet.Hotbar6) {
                    TrySwapHeld(5);
                }
                else if(triggersSet.Hotbar7) {
                    TrySwapHeld(6);
                }
                else if(triggersSet.Hotbar8) {
                    TrySwapHeld(7);
                }
                else if(triggersSet.Hotbar9) {
                    TrySwapHeld(8);
                }
                else if(triggersSet.Hotbar10) {
                    TrySwapHeld(9);
                }
                else releasedSwap = true;
            }
        }

        private void TrySwapHeld(int itemIndex){
            if(!releasedSwap) return;
            releasedSwap = false;
            Item temp = player.inventory[itemIndex];
            player.inventory[itemIndex] = player.HeldItem;
            for (int i = player.inventory.Length - 1; i >= 0 ; i--){
                if(player.inventory[i].type != 0) continue;
                player.inventory[i] = temp;
                Main.mouseItem.TurnToAir();
                return;
            }
            Main.mouseItem = temp;
        }
    }

}