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

    private bool swappedHotBar;


    public bool adrenaline;
    private readonly HashSet<int> hiddenBuffs = new();


    public int timeWarp;

    public float speedMult;

    public float spawnRateBoost;

    public bool weatherRadio;

    public bool fishGuide;

    public bool sextant;
    public int savedMoonPhase;


    public override void Load() {
        On.Terraria.Main.UpdateWeather += HookUpdateWeather;
        On.Terraria.Player.Fishing_GetPowerMultiplier += HookGetPowerMultiplier;
        On.Terraria.Projectile.GetFishingPondState += HookGetFishingPondState;
    }


    private static float HookGetPowerMultiplier(On.Terraria.Player.orig_Fishing_GetPowerMultiplier orig, Player self, Item pole, Item bait) {
        if(Main.LocalPlayer.GetModPlayer<SpymPlayer>().fishGuide) return 1.2f * 1.1f * 1.3f * 1.1f; // Not done with the tml hook to prevent other global items to edit the value (+/-)
        return orig(self, pole, bait);
    }

    private void HookGetFishingPondState(On.Terraria.Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
        orig(x, y, out lava, out honey, out numWaters, out chumCount);
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().fishGuide) numWaters = 300;
    }


    private static void HookUpdateWeather(On.Terraria.Main.orig_UpdateWeather orig, Main self, GameTime gameTime) {
        if(!Main.LocalPlayer.GetModPlayer<SpymPlayer>().weatherRadio) orig(self, gameTime); // TODO muliplayer
    }

    public override void ResetEffects() {
        timeWarp = 1;
        spawnRateBoost = 1;
        speedMult = 1;
        weatherRadio = false;
        fishGuide = false;
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
        if (!sextant) savedMoonPhase = -1;
    }

    public override void PostUpdateRunSpeeds() {
        Player.maxRunSpeed *= speedMult;
        Player.accRunSpeed *= speedMult;

        Player.jumpSpeed *= MathF.Sqrt(speedMult);
        Player.jumpSpeedBoost *= speedMult;
        
        Player.maxFallSpeed *= speedMult;
        Player.gravity *= speedMult;
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

            if(slot == -1) swappedHotBar = false;
            else if(!swappedHotBar) SwapHeld(slot);
        }
    }

    private void SwapHeld(int itemIndex) {
        swappedHotBar = true;
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
