using System.Collections.Generic;
using SPYM.Globals;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYM;

public static class ImprovedInfoAcc {

    public static bool Enabled => Configs.ServerConfig.Instance.infoAccPlus;

    public static void SetDefaults(Item item) {
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            item.autoReuse = true;
            item.useTurn = true;
            item.useStyle = ItemUseStyleID.HoldUp;
            item.useTime = 15;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item15;
            break;
        case ItemID.WeatherRadio:
            item.useStyle = ItemUseStyleID.HoldUp;
            item.useTime = 45;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item43;
            break;
        case ItemID.Sextant:
            item.useStyle = ItemUseStyleID.HoldUp;
            item.useTime = 45;
            item.useAnimation = 45;
            item.UseSound = SoundID.Roar;
            break;
        case ItemID.Compass or ItemID.DepthMeter:
            item.useStyle = ItemUseStyleID.Swing;
            item.useTime = 45;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item6;
            break;
        }
    }
    public static void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        void AddTooltip(string key, params object[] args) {
            tooltips.AddLine(
                new(SpikysMod.Instance, key.ToUpperInvariant(),
                    args is null ? Language.GetTextValue($"{LocKeys.Items}.{key}.Tooltip") : Language.GetTextValue($"{LocKeys.Items}.{key}.Tooltip", args)
                ),
                TooltipLineID.Tooltip
            );
        }

        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            AddTooltip("Watch");
            break;
        case ItemID.Radar:
            AddTooltip("Radar");
            break;
        case ItemID.TallyCounter:
            AddTooltip("TallyCounter");
            break;
        case ItemID.LifeformAnalyzer:
            AddTooltip("LifeformAnalyzer");
            break;
        case ItemID.Stopwatch:
            AddTooltip("Stopwatch");
            break;
        case ItemID.MetalDetector:
            List<string> kb = SpikysMod.PrioritizeOre.GetAssignedKeys();
            AddTooltip("MetalDetector", kb.Count == 0 ? Lang.menu[195].Value : kb[0]);
            break;
        case ItemID.DPSMeter:
            AddTooltip("multiplicative", 5);
            AddTooltip("DPSMeter");
            break;
        case ItemID.WeatherRadio:
            AddTooltip("WeatherRadio");
            break;
        case ItemID.FishermansGuide:
            AddTooltip("FishermansGuide");
            break;
        case ItemID.Sextant:
            AddTooltip("Sextant");
            break;
        case ItemID.CellPhone:
            AddTooltip("CellPhone");
            break;
        case ItemID.Compass or ItemID.DepthMeter:
            AddTooltip("Compass");
            break;
        }
    }

    public static bool CanUseItem(Item item) {
        if (item.type == ItemID.Sextant) return NPC.BusyWithAnyInvasionOfSorts();
        return true;
    }
    public static bool AltFunctionUse(Item item) => item.type == ItemID.WeatherRadio;

    public static bool? UseItem_Use(Item item, Player player) {
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            return true;
        case ItemID.WeatherRadio:
            // TODO multiplayer
            if (Main.raining) Main.StopRain();
            else Main.StartRain();
            return true;
        case ItemID.Sextant: // TODO multiplayer
            // TODO only if event has been defeated
            Main.StopSlimeRain(false);
            Main.bloodMoon = false;
            Main.eclipse = false;
            Terraria.GameContent.Events.DD2Event.StopInvasion(false);
            Main.invasionType = 0;
            Main.stopMoonEvent();
            if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(Language.GetTextValue($"{LocKeys.Chat}.eventCancelled"), Colors.RarityGreen.R, Colors.RarityGreen.G, Colors.RarityGreen.B);
            else if (Main.netMode == NetmodeID.Server) Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey($"{LocKeys.Chat}.eventCancelled"), Colors.RarityGreen);
            return true;
        case ItemID.Compass or ItemID.DepthMeter:
            SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
            spymPlayer.biomeLockPosition = player.Center;
            return true;
        }
        return null;
    }
    public static bool? UseItem_Alt(Item item) {
        switch (item.type) {
        case ItemID.WeatherRadio: // TODO multiplayer
            Main.windSpeedTarget = Main.rand.NextBool() ? -0.8f : 0.8f;
            Main.ResetWindCounter(true);
            return true;
        }
        return null;
    }
    public static void UseStyle(Item item, Player player) {
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            // TODO multiplayer
            spymPlayer.timeMult *= 10;
            break;
        case ItemID.CellPhone:
            if (player.itemTime == player.itemTimeMax / 2 + 1)
                player.DoPotionOfReturnTeleportationAndSetTheComebackPoint();
            break;
        }
    }

    public static void UpdateEquip(Item item, Player player){
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            spymPlayer.timeMult *= 0.9f;
            break;
        case ItemID.Radar:
            spymPlayer.spawnRateMult += 1.5f;
            break;
        case ItemID.TallyCounter:
            spymPlayer.lootMult += 0.25f;
            break;
        case ItemID.LifeformAnalyzer:
            spymPlayer.npcExtraRerolls += 19;
            break;
        case ItemID.Stopwatch:
            spymPlayer.speedMult += 0.5f;
            break;
        case ItemID.DPSMeter:
            spymPlayer.fixDamage = true;
            player.GetDamage(DamageClass.Generic) *= 1.05f;
            break;
        case ItemID.MetalDetector:
            spymPlayer.orePriority = true;
            if (player.HeldItem.type == ItemID.SpelunkerGlowstick) break;
            player.spelunkerTimer++;
            if (player.spelunkerTimer++ < 10) break;
            player.spelunkerTimer = 0;
            Main.instance.SpelunkerProjectileHelper.AddSpotToCheck(player.Center);
            break;
        case ItemID.WeatherRadio:
            spymPlayer.forcedSeasons = true;
            break;
        case ItemID.FishermansGuide:
            spymPlayer.maxFishingPower = true; // TODO >>> redo
            break;
        case ItemID.Sextant:
            spymPlayer.eventsMult += 0.5f;
            break;
        case ItemID.Compass or ItemID.DepthMeter:
            spymPlayer.biomeLock = true;
            break;
        }
    }

    public static bool ForcedUnityPotion(Player player) => player.HasItem(ItemID.CellPhone);

    public static void PostAddRecipes() {
        foreach (Recipe recipe in Main.recipe) {
            if (recipe.createItem.type != ItemID.CellPhone || recipe.requiredItem.Find(i => i.type == ItemID.PDA) == null) continue;
            recipe.requiredItem.Add(new(ItemID.PotionOfReturn, 15));
            recipe.requiredItem.Add(new(ItemID.WormholePotion, 15));
        }
    }
}