using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;


namespace SPYM.Globals;

public class SpymItem : GlobalItem {

    public override void SetDefaults(Item item) {
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            item.autoReuse = true;
            item.useTurn = true;
            item.useStyle = ItemUseStyleID.HoldUp;
            item.useTime = 15;
            item.useAnimation = 15;
            break;
        case ItemID.WeatherRadio:
            item.useStyle = ItemUseStyleID.HoldUp;
            item.useTime = 45;
            item.useAnimation = 45;
            break;
        case ItemID.Sextant:
            item.useStyle = ItemUseStyleID.HoldUp;
            item.useTime = 45;
            item.useAnimation = 45;
            break;
        }
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        List<(string, object[]?)> keys = new(); // ? cache
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            keys.Add(("Watch", null));
            break;
        case ItemID.Radar:
            keys.Add(("Radar", null));
            break;
        case ItemID.TallyCounter:
            keys.Add(("TallyCounter", null));
            break;
        case ItemID.LifeformAnalyzer:
            keys.Add(("LifeformAnalyzer", null));
            break;
        case ItemID.Stopwatch:
            keys.Add(("Stopwatch", null));
            break;
        case ItemID.MetalDetector:
            keys.Add(("MetalDetector", null));
            break;
        case ItemID.DPSMeter:
            keys.Add(("DPSMeter", null));
            keys.Add(("multiplicative", new object[]{5}));
            break;
        case ItemID.WeatherRadio:
            keys.Add(("WeatherRadio", null));
            break;
        case ItemID.FishermansGuide:
            keys.Add(("FishGuide", null));
            break;
        case ItemID.Sextant:
            keys.Add(("Sextant", null));
            break;
        case ItemID.CellPhone:
            keys.Add(("CellPhone", null));
            break;
        }

        foreach ((string key, object[]? args) in keys) tooltips.AddLine(new(SpikysMod.Instance, key.ToUpperInvariant(), args is null ? Language.GetTextValue("Mods.SPYM.Tooltips." + key) : Language.GetTextValue("Mods.SPYM.Tooltips." + key, args)), TooltipLineID.Tooltip);
    }

    public override bool? UseItem(Item item, Player player) {
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            return true;
        case ItemID.WeatherRadio:
            ChangeRain();
            return true;;
        case ItemID.Sextant: // TODO multiplayer
            Main.moonType = (Main.moonType + 1) % 9;
            return true;
        }

        return null;
    }


    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame) {
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            spymPlayer.timeWarp *= 10;
            break;
        case ItemID.CellPhone:
            if (player.itemTime == player.itemTimeMax / 2 + 1)
                player.DoPotionOfReturnTeleportationAndSetTheComebackPoint();
            break;
        }
    }

    public override void UpdateEquip(Item item, Player player) {
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            spymPlayer.timeWarp *= 0.9f;
            break;
        case ItemID.Radar:
            spymPlayer.spawnRateBoost += 1.5f;
            break;
        case ItemID.TallyCounter:
            spymPlayer.tallyMult += 0.25f;
            break;
        case ItemID.LifeformAnalyzer:
            spymPlayer.npcExtraRerolls += 19;
            break;
        case ItemID.Stopwatch:
            spymPlayer.speedMult += 0.5f;
            break; 
        case ItemID.DPSMeter:
            spymPlayer.dpsMeter = true;
            player.GetDamage(DamageClass.Generic) *= 1.05f;
            break;
        case ItemID.MetalDetector:
            if (player.HeldItem.type == ItemID.SpelunkerGlowstick) break;
            player.spelunkerTimer++;
            if (player.spelunkerTimer++ < 10) break;
            player.spelunkerTimer = 0;
            Main.instance.SpelunkerProjectileHelper.AddSpotToCheck(player.Center);
            break;
        case ItemID.WeatherRadio:
            spymPlayer.weatherRadio = true;
            break;
        case ItemID.FishermansGuide:
            spymPlayer.fishGuide = true;
            break;
        case ItemID.Sextant: // TODO multiplayer
            spymPlayer.sextant = true;
            if(spymPlayer.savedMoonPhase == -1) spymPlayer.savedMoonPhase = Main.moonPhase;
            else if(Main.moonPhase != spymPlayer.savedMoonPhase) Main.moonPhase = spymPlayer.savedMoonPhase;
            break;
        }
    }

    public static void SmartConsume(Player player, Item consumed, bool lastStack = false){
        Item? smartStack = lastStack ? player.LastStack(consumed, true) : player.SmallestStack(consumed, true);
        if (smartStack == null) return;
        consumed.stack++;
        smartStack.stack--;
    }

    public override void OnConsumeItem(Item item, Player player) {
        if (Configs.ClientConfig.Instance.smartConsume) SmartConsume(player, item);
    }

    public override void OnConsumedAsAmmo(Item ammo, Item weapon, Player player) {
        if (Configs.ClientConfig.Instance.smartAmmo) SmartConsume(player, ammo, true);
    }

    public static void ChangeRain() {
        // BUG wierd stuff on multi
        if (Main.raining) Main.StopRain();
        else Main.StartRain();
    }
}


