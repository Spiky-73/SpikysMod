using Terraria;
using Terraria.ModLoader;
using Terraria.GameInput;
using Terraria.UI;


namespace SPYM {

    public class SpyPlayer : ModPlayer {

        private static bool [] defaultNoDisplay;
        private bool releasedSwap;

        public int radarLevel, analyserLevel, tallyLevel;

        public bool adrenaline;

        public SpyPlayer (){
            adrenaline = false;
            releasedSwap = true;
            // saves the value of the buffs
            if(defaultNoDisplay == null) {
                defaultNoDisplay = new bool[BuffLoader.BuffCount];
                Main.buffNoTimeDisplay.CopyTo(defaultNoDisplay, 0);
            }
            
        }

        public override void PlayerDisconnect(Player Player){
            defaultNoDisplay.CopyTo(Main.buffNoTimeDisplay, 0);
        }
		public override void ResetEffects() {
            radarLevel = 0;
            tallyLevel = 0;
            analyserLevel = 0;
		}

		public override void PostUpdateBuffs() {
            ClientConfig config = ModContent.GetInstance<ClientConfig>();

            for (int i = 0; i < Player.buffType.Length; i++){
                int type = Player.buffType[i];
                if(Main.debuff[type] || defaultNoDisplay[type]) continue;
                bool frozen = config.frozenBuffs && ((Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts()) || adrenaline);
                if(frozen) {
                    Main.buffNoTimeDisplay[type] = true;
                    Player.buffTime[i] = 30*60; // freeze the buff time
                }   else {
                    Main.buffNoTimeDisplay[type] = defaultNoDisplay[type];
                }
            }
            adrenaline = false; // Needs to be here because of update order

        }

        public override void ProcessTriggers(TriggersSet triggersSet){
            if(Main.mouseRight && Main.mouseRightRelease
                    && Player.selectedItem < 10) { // Potential issue for accesories having a right click action
                ItemSlot.SwapEquip(Player.inventory, 0, Player.selectedItem);
			}
            if(SpikysMod.FavoritedBuff.JustPressed){
                FavoritedBuff();
            }

            if(Main.playerInventory && !Main.mouseItem.IsAir){
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
			if (!releasedSwap) return;
			releasedSwap = false;
            Item temp = Player.inventory[itemIndex];
            Player.inventory[itemIndex] = Player.HeldItem;
            for (int i = Player.inventory.Length - 1; i >= 0 ; i--){
                if(!Player.inventory[i].IsAir) continue;
                Player.inventory[i] = temp;
                Main.mouseItem.TurnToAir();
                return;
            }
            Main.mouseItem = temp;
        }

        private void FavoritedBuff() {
            Item[] inv = Player.inventory;
            for (int i = 0; i < Player.inventory.Length - 1; i++) {
                if (!inv[i].favorited && inv[i].stack > 0) inv[i].stack *= -1;
            }
            Player.QuickBuff();
            for (int i = 0; i < inv.Length - 1; i++) {
                if (!inv[i].favorited && inv[i].stack < 0) inv[i].stack *= -1;
            }
        }
    }

}