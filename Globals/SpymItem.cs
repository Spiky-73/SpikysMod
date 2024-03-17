using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYM.Globals;

public class SpymItem : GlobalItem {

    public override bool InstancePerEntity => true;

    public int prioritizedOre = -1;
    public Vector2? recoredPosition = null;

    public override void SaveData(Item item, TagCompound tag) {
        if(prioritizedOre != -1) tag[PrioritizedOreTag] = prioritizedOre;
        if(recoredPosition.HasValue) tag[RecoredPositionTag] = recoredPosition;
    }
    public override void LoadData(Item item, TagCompound tag) {
        if(tag.TryGet(PrioritizedOreTag, out int ore)) prioritizedOre = ore;
        if(tag.TryGet(RecoredPositionTag, out Vector2 position)) recoredPosition = position;
    }

    public override void SetDefaults(Item item) {
        if (!Configs.VanillaImprovements.Instance.infoAccPlus) return;
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
        case ItemID.MetalDetector:
            item.useStyle = ItemUseStyleID.Thrust;
            item.useTime = 30;
            item.useAnimation = 30;
            item.UseSound = SoundID.Tink;
            break;
        }
    }
    public override void OnSpawn(Item item, IEntitySource source) { // TODO multiplayer
        if(source is not EntitySource_TileBreak kill || !Main.IsTileSpelunkable(kill.TileCoords.X, kill.TileCoords.Y)) return;
        SpymPlayer player = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        (int extra, float rand) = ((int)(player.oreBoost / 0.5f), player.oreBoost % 0.5f);
        if (rand > 0.001f && player.Player.RollLuck((int)(1/rand)) == 0) item.stack++;
        if (extra > 0) item.stack += extra - player.Player.RollLuck(extra+1);
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (!Configs.VanillaImprovements.Instance.infoAccPlus) return;
        void AddItemTooltip(string key, params object[] args) {
            tooltips.AddLine(
                new(SpikysMod.Instance, key.ToUpperInvariant(),
                    args is null ? Language.GetTextValue($"{Localization.Keys.Items}.{key}.Tooltip") : Language.GetTextValue($"{Localization.Keys.Items}.{key}.Tooltip", args)
                ),
                TooltipLineID.Tooltip
            );
        }

        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            AddItemTooltip(nameof(ItemID.GoldWatch));
            break;
        case ItemID.Radar:
            AddItemTooltip(nameof(ItemID.Radar));
            break;
        case ItemID.TallyCounter:
            AddItemTooltip(nameof(ItemID.TallyCounter));
            break;
        case ItemID.LifeformAnalyzer:
            AddItemTooltip(nameof(ItemID.LifeformAnalyzer));
            break;
        case ItemID.Stopwatch:
            AddItemTooltip(nameof(ItemID.Stopwatch));
            break;
        case ItemID.MetalDetector:
            AddItemTooltip(nameof(ItemID.MetalDetector));
            break;
        case ItemID.DPSMeter:
            AddItemTooltip(nameof(ItemID.DPSMeter), 7.5f);
            break;
        case ItemID.WeatherRadio:
            AddItemTooltip(nameof(ItemID.WeatherRadio));
            break;
        case ItemID.FishermansGuide:
            AddItemTooltip(nameof(ItemID.FishermansGuide));
            break;
        case ItemID.Sextant:
            AddItemTooltip(nameof(ItemID.Sextant));
            break;
        case ItemID.CellPhone:
            AddItemTooltip(nameof(ItemID.CellPhone));
            break;
        case ItemID.Compass or ItemID.DepthMeter:
            AddItemTooltip(nameof(ItemID.Compass));
            break;
        }
    }

    public override bool CanUseItem(Item item, Player player) {
        if (Configs.VanillaImprovements.Instance.infoAccPlus) {
            switch (item.type) {
            case ItemID.Sextant: return NPC.BusyWithAnyInvasionOfSorts();
            case ItemID.MetalDetector: return player.IsTargetTileInItemRange(player.GetBestPickaxe());
            }
        }
        return true;
    }

    public override bool AltFunctionUse(Item item, Player player) {
        if (Configs.VanillaImprovements.Instance.infoAccPlus && item.type == ItemID.WeatherRadio) return true;
        return false;
    }

    public override bool? UseItem(Item item, Player player){
        if(!Configs.VanillaImprovements.Instance.infoAccPlus) return null;
        if (player.altFunctionUse != 2) {
            switch (item.type) {
            case ItemID.GoldWatch or ItemID.PlatinumWatch:
                return true;
            case ItemID.WeatherRadio: // TODO multiplayer
                if (Main.raining) Main.StopRain();
                else Main.StartRain();
                SoundEngine.PlaySound(SoundID.Item66);
                return true;
            case ItemID.Sextant: // TODO multiplayer
                Main.StopSlimeRain(false);
                Main.bloodMoon = false;
                Main.eclipse = false;
                Terraria.GameContent.Events.DD2Event.StopInvasion(false);
                Main.invasionType = 0;
                Main.stopMoonEvent();
                if (Main.netMode == NetmodeID.SinglePlayer) Main.NewText(Language.GetTextValue($"{Localization.Keys.Chat}.eventCancelled"), Colors.RarityGreen.R, Colors.RarityGreen.G, Colors.RarityGreen.B);
                else if (Main.netMode == NetmodeID.Server) Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey($"{Localization.Keys.Chat}.eventCancelled"), Colors.RarityGreen);
                return true;
            case ItemID.Compass or ItemID.DepthMeter:
                recoredPosition = player.Center;
                return true;
            case ItemID.MetalDetector:
                prioritizedOre = Main.IsTileSpelunkable(Player.tileTargetX, Player.tileTargetY) ? Main.tile[Player.tileTargetX, Player.tileTargetY].TileType : -1;
                return true;
            }
        } else {
            switch (item.type) {
            case ItemID.WeatherRadio: // TODO multiplayer
                if (Main.windSpeedTarget == 0f) Main.windSpeedTarget = Main.rand.NextBool() ? -0.8f : 0.8f;
                else Main.windSpeedTarget = 0;
                Main.ResetWindCounter(true);
                SoundEngine.PlaySound(SoundID.Item43);
                return true;
            }
        }
        return null;
    }

    public override void HoldItem(Item item, Player player) {
        if (Configs.VanillaImprovements.Instance.infoAccPlus) {
            switch (item.type) {
            case ItemID.MetalDetector:
                if (ItemLoader.CanUseItem(item, player)) player.cursorItemIconEnabled = true;
                break;
            }
        }
    }

    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame) {
        if (!Configs.VanillaImprovements.Instance.infoAccPlus) return;
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            spymPlayer.timeMult *= 10;
            break;
        case ItemID.CellPhone:
            if (player.itemTime == player.itemTimeMax / 2 + 1)
                player.DoPotionOfReturnTeleportationAndSetTheComebackPoint();
            break;
        }
    }

    public override void UpdateEquip(Item item, Player player) {
        if (!Configs.VanillaImprovements.Instance.infoAccPlus) return;
        SpymPlayer spymPlayer = player.GetModPlayer<SpymPlayer>();
        switch (item.type) {
        case ItemID.GoldWatch or ItemID.PlatinumWatch:
            spymPlayer.timeMult *= 0.9f;
            break;
        case ItemID.Radar:
            spymPlayer.spawnRateMult *= 1.5f;
            break;
        case ItemID.TallyCounter:
            spymPlayer.lootBoost += 0.25f;
            break;
        case ItemID.LifeformAnalyzer:
            spymPlayer.npcExtraRerolls += 19;
            break;
        case ItemID.Stopwatch:
            spymPlayer.speedMult += 0.20f;
            break;
        case ItemID.DPSMeter:
            spymPlayer.fixedDamage = true;
            player.GetDamage(DamageClass.Generic) += 0.075f;
            break;
        case ItemID.MetalDetector:
            spymPlayer.oreBoost += 0.2f;
            player.pickSpeed -= 0.1f;
            spymPlayer.oreHighlight = true;
            break;
        case ItemID.WeatherRadio:
            spymPlayer.forcedSeasons = true;
            break;
        case ItemID.FishermansGuide:
            spymPlayer.minFishingPower = 1 + (1.2f * 1.1f * 1.3f * 1.1f - 1) / 2;
            break;
        case ItemID.Sextant:
            spymPlayer.eventsBoost += 0.5f;
            break;
        case ItemID.Compass or ItemID.DepthMeter:
            if (!recoredPosition.HasValue) recoredPosition = player.Center;
            spymPlayer.biomeLock = this;
            break;
        }
    }

    public const string PrioritizedOreTag = "ore";
    public const string RecoredPositionTag = "position";
}


