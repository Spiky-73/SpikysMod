using Terraria;
using Terraria.GameInput;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;
using SPYM.Configs;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using Microsoft.Xna.Framework;
using System;

namespace SPYM.Globals;

public class SpymPlayer : ModPlayer {

    private readonly HashSet<int> hiddenBuffs = new();

    public bool adrenaline;

    private bool swapped;

    public bool weatherRadio;
    private readonly bool[] changedFrozenWeather = new bool[2];

    public int timeWarp;

    public float speedMult;
    public float spawnRateBoost;

    public bool sextant = false;
    public int moonPhase = -1;

    public override void Load() {
        On.Terraria.Main.UpdateWeather += HookUpdateWeather;
    }

    private static void HookUpdateWeather(On.Terraria.Main.orig_UpdateWeather orig, Main self, GameTime gameTime) {
        if(!Main.LocalPlayer.GetModPlayer<SpymPlayer>().weatherRadio) orig(self, gameTime);
    }

    public override void PlayerDisconnect(Player Player) { // TODO not working in single player
        foreach(int buff in hiddenBuffs){
            Main.buffNoTimeDisplay[buff] = false;
        }
        hiddenBuffs.Clear();
    }


    public override void ResetEffects() {
        timeWarp = 1;
        spawnRateBoost = 1;
        speedMult = 1;
        weatherRadio = false;
        sextant = false;

        foreach (int buff in hiddenBuffs) Main.buffNoTimeDisplay[buff] = false;
        hiddenBuffs.Clear();
    }


    public override void PreUpdateBuffs() {

        if (adrenaline || (ClientConfig.Instance.frozenBuffs && (Utility.BossAlive() || Utility.BusyWithInvasion()))) {
            for (int i = 0; i < Player.buffType.Length; i++) {
                int buff = Player.buffType[i];
                if (Main.debuff[buff] || Main.buffNoTimeDisplay[buff]) continue;

                hiddenBuffs.Add(buff);
                Main.buffNoTimeDisplay[buff] = true;
                Player.buffTime[i] += 1;
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
        } else {
            if (changedFrozenWeather[0]) {
                CreativePowerManager.Instance.GetPower<CreativePowers.FreezeRainPower>().SetPowerInfo(false);
                changedFrozenWeather[0] = false;
            }
            if (changedFrozenWeather[1]) {
                changedFrozenWeather[1] = false;
                CreativePowerManager.Instance.GetPower<CreativePowers.FreezeWindDirectionAndStrength>().SetPowerInfo(false);
            }
        }
        if(sextant) {
            if(moonPhase != -1 && Main.moonPhase != moonPhase) Main.moonPhase = moonPhase;
        }else moonPhase = -1;

    }

    public override void PostUpdateRunSpeeds() {
        Player.maxRunSpeed *= speedMult;
        Player.accRunSpeed *= speedMult; // TODO wing accend rate
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {
        if (Main.mouseRight && Main.mouseRightRelease && Player.altFunctionUse == 0) ItemSlot.RightClick(Player.inventory, 0, Player.selectedItem);

        if (SpikysMod.FavoritedBuff.JustPressed) FavoritedBuff();

        if (Main.playerInventory && !Main.mouseItem.IsAir) {
            int slot = -1;
            if (triggersSet.Hotbar1)       slot = 0;
            else if (triggersSet.Hotbar2)  slot = 1;
            else if (triggersSet.Hotbar3)  slot = 2;
            else if (triggersSet.Hotbar4)  slot = 3;
            else if (triggersSet.Hotbar5)  slot = 4;
            else if (triggersSet.Hotbar6)  slot = 5;
            else if (triggersSet.Hotbar7)  slot = 6;
            else if (triggersSet.Hotbar8)  slot = 7;
            else if (triggersSet.Hotbar9)  slot = 8;
            else if (triggersSet.Hotbar10) slot = 9;

            if(slot == -1) swapped = false;
            else if(!swapped) SwapHeld(slot);
        }
    }

    private void SwapHeld(int itemIndex) {
        swapped = true;
        Item toSwap = Player.inventory[itemIndex];
        Player.inventory[itemIndex] = Player.HeldItem;

        // TODO Place to slot under mouse / of the item
        // ? Use Player.GetItem()
        for (int i = Player.inventory.Length - 2; i >= 0; i--) {
            if ((i == Player.inventory.Length - 2 && !toSwap.FitsAmmoSlot()) || (i == Player.inventory.Length - 6 && !toSwap.IsACoin)) {
                i -= 3;
                continue;
            }
            if (Player.inventory[i].IsAir) {
                Player.inventory[i] = toSwap;
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }
        }
        Main.mouseItem = toSwap;
        SoundEngine.PlaySound(SoundID.Grab);
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
