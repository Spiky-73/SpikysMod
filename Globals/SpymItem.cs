using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.UI;
using System;

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
        List<string> keys = new(); // TODO cache
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            keys.Add("Watch");
            break;
        case ItemID.Radar:
            keys.Add("Radar");
            break;
        case ItemID.TallyCounter:
            keys.Add("TallyCounter");
            break;
        case ItemID.LifeformAnalyzer:
            keys.Add("LifeformAnalyzer");
            break;
        case ItemID.Stopwatch:
            keys.Add("Stopwatch");
            break;
        case ItemID.MetalDetector:
            keys.Add("MetalDetector");
            break;
        case ItemID.WeatherRadio:
            keys.Add("WeatherRadio");
            break;
        case ItemID.Sextant:
            keys.Add("Sextant");
            break;
        case ItemID.CellPhone:
            keys.Add("CellPhone");
            break;
        }

        foreach (string key in keys) tooltips.AddLine(new(SpikysMod.Instance, key.ToUpperInvariant(), Language.GetTextValue("Mods.SPYM.Tooltips." + key)), TooltipLineID.Tooltip);
    }

    public override bool? UseItem(Item item, Player player) {
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            return true;
        case ItemID.WeatherRadio:
            ChangeRain();
            return true;;
        case ItemID.Sextant:
            Main.moonType = (Main.moonType + 1) % 9;
            return true;
        }

        return null;
    }


    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame) {
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            spymPlayer.timeWarp = 20;
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
        case ItemID.Radar:
            spymPlayer.spawnRateBoost = 2.5f;
            break;
        case ItemID.Stopwatch:
            spymPlayer.speedMult = 2.5f;
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
        case ItemID.Sextant:
            spymPlayer.sextant = true;
            if(spymPlayer.moonPhase == -1) spymPlayer.moonPhase = Main.moonPhase;
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

    private static void ChangeRain() {
        // TODO multiplayer
        if (Main.raining)
            Main.StopRain();
        else
            Main.StartRain();
    }
}

