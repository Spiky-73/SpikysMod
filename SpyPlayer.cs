using Terraria;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;

namespace SPYM {

    public class SpyPlayer : ModPlayer {

        private bool [] changedBuffDisplays;

        public bool adrenaline;

        private bool swapped;
           
        public bool weatherRadio;
		private bool[] changedFrozenWeather;

        public int stopWatchLevel;
        public int radarLevel, analyserLevel, tallyLevel;

        public SpyPlayer (){
            changedFrozenWeather = new bool[2];
            // saves the value of the buffs
            changedBuffDisplays = new bool[BuffLoader.BuffCount];
            Main.buffNoTimeDisplay.CopyTo(changedBuffDisplays, 0);
        }

        public override void PlayerDisconnect(Player Player){
            for (int i = 0; i < changedBuffDisplays.Length; i++) {
                if (changedBuffDisplays[i])
                    Main.buffNoTimeDisplay[i] = false;
            }
        }
		public override void ResetEffects() {
            radarLevel = 0;
            tallyLevel = 0;
            analyserLevel = 0;
            stopWatchLevel = 0;
            weatherRadio = false;
        }
		public override void PreUpdateBuffs() {
            ClientConfig config = ModContent.GetInstance<ClientConfig>();

            bool frozenBuffs = adrenaline || (config.frozenBuffs && (Utility.BossAlive() || Utility.BusyWithInvasion()));

            for (int i = 0; i < Player.buffType.Length; i++){
                int type = Player.buffType[i];
                if(Main.debuff[type] || (Main.buffNoTimeDisplay[type] && !changedBuffDisplays[type])) continue;

                if (frozenBuffs) {
                    changedBuffDisplays[type] = true;
                    Player.buffTime[i] = 30 * 60;
                    Main.buffNoTimeDisplay[type] = true;
				} else if (changedBuffDisplays[type]) {
                    Main.buffNoTimeDisplay[type] = false;
                    changedBuffDisplays[type] = false;
                }
            }
            adrenaline = false; // Needs to be here because of update order

        }
        public override void UpdateEquips() {
			if (weatherRadio) {
                if (!CreativePowerManager.Instance.GetPower<CreativePowers.FreezeRainPower>().Enabled)
                    changedFrozenWeather[0] = true;
                if (!CreativePowerManager.Instance.GetPower<CreativePowers.FreezeWindDirectionAndStrength>().Enabled)
                    changedFrozenWeather[1] = true;
                CreativePowerManager.Instance.GetPower<CreativePowers.FreezeRainPower>().SetPowerInfo(true);
                CreativePowerManager.Instance.GetPower<CreativePowers.FreezeWindDirectionAndStrength>().SetPowerInfo(true);
            }else {
                if (changedFrozenWeather[0]) {
                    CreativePowerManager.Instance.GetPower<CreativePowers.FreezeRainPower>().SetPowerInfo(false);
                    changedFrozenWeather[0] = false;
				}
                if (changedFrozenWeather[1]) {
                    changedFrozenWeather[1] = false;
                    CreativePowerManager.Instance.GetPower<CreativePowers.FreezeWindDirectionAndStrength>().SetPowerInfo(false);
                }
            }
            
        }

		public override void PostUpdateRunSpeeds() {
			switch (stopWatchLevel) {
            case 1:
                Player.maxRunSpeed *= 1.5f;
				Player.accRunSpeed *= 1.5f;
                //Player.runAcceleration *= 1.5f;
				break;
			}
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
                else swapped = false;
            }
        }

        private void TrySwapHeld(int itemIndex){
			if (swapped) return;
			swapped = true;
            Item toSwap = Player.inventory[itemIndex];
            Player.inventory[itemIndex] = Player.HeldItem;

            // Place to slot under mouse / of the item ????

            for (int i = Player.inventory.Length-2; i >= 0; i--) {
                if ((i == Player.inventory.Length - 1 && !toSwap.FitsAmmoSlot()) || (i == Player.inventory.Length - 5 && !toSwap.IsACoin)) {
                    i -= 4;
                    continue;
                }
                if (Player.inventory[i].IsAir) {
                    Player.inventory[i] = toSwap;
                    Main.mouseItem.TurnToAir();
                    return;
				}
			}
            Main.mouseItem = toSwap;
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