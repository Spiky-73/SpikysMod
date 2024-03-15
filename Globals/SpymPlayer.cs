using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.UI;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;
using Terraria.Audio;
using System.Collections.Generic;
using System;

namespace SPYM.Globals;

public class SpymPlayer : ModPlayer {

    // TODO sync player data
    public bool fixedDamage;
    public bool forcedSeasons;
    
    public float minFishingPower;

    public static ModKeybind PrioritizeOre = null!;
    public bool orePriority;
    public int prioritizedOre = -1;

    public bool biomeLock;
    public Vector2? biomeLockPosition;

    public float timeMult;
    public float speedMult;
    public float spawnRateMult;
    public float lootBoost;
    public float eventsBoost;

    public int npcExtraRerolls;

    private static readonly HashSet<int> _hiddenBuffs = new();

    public override void Load() {
        PrioritizeOre = KeybindLoader.RegisterKeybind(Mod, "PrioritizeOre", Microsoft.Xna.Framework.Input.Keys.LeftControl);

        On_Projectile.GetFishingPondState += HookGetFishingPondState;
        On_Player.Fishing_GetPowerMultiplier += HookGetPowerMultiplier;

        On_Player.HasUnityPotion += HookHasUnityPotion;
        On_Player.TakeUnityPotion += HookTakeUnityPotion;
        
        On_Projectile.Damage += HookProjDamage;
        On_Player.GetMinecartDamage += HookMinecartDamage;

        On_Player.UpdateBiomes += HookUpdateBiomes;
        IL_SceneMetrics.ScanAndExportToMain += ILScanAndExportToMain;

        On_ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        ItemSlot.OnItemTransferred += OnItemTranfer;

        On_Main.GetBuffTooltip += HookBuffTooltip;

        On_ChestUI.LootAll += HookLootAll;
        On_ChestUI.Restock += HookRestock;
    }

    public override void Initialize() => ResetEffects();

    public override void ResetEffects() {
        forcedSeasons = false;
        fixedDamage = false;
        biomeLock = false;
        orePriority = false;
        minFishingPower = 0;
        npcExtraRerolls = 0;
        eventsBoost = 1f;
        timeMult = 1;
        lootBoost = 1f;
        spawnRateMult = 1;
        speedMult = 1;

        foreach (int buff in _hiddenBuffs) Main.buffNoTimeDisplay[buff] = false;
        _hiddenBuffs.Clear();
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {

        if (orePriority && PrioritizeOre.JustPressed && Player.HeldItem.pick > 0 && Player.IsTargetTileInItemRange(Player.HeldItem)) {
            prioritizedOre = Main.IsTileSpelunkable(Player.tileTargetX, Player.tileTargetY) ? Main.tile[Player.tileTargetX, Player.tileTargetY].TileType : -1;
            SoundEngine.PlaySound(SoundID.Tink);
        }
    }

    public override bool PreItemCheck() {
        SpymSystem.FixedDamage = fixedDamage;
        return true;
    }
    public override void PostItemCheck() {
        SpymSystem.FixedDamage = false;
    }

    public override void PreUpdateBuffs() {
        if (Configs.VanillaImprovements.Instance.frozenBuffs && (Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts())){
            for (int i = 0; i < Player.buffType.Length; i++) {
                int buff = Player.buffType[i];
                if (!_hiddenBuffs.Contains(buff) && (Main.debuff[buff] || Main.buffNoTimeDisplay[buff])) continue;

                _hiddenBuffs.Add(buff);
                Main.buffNoTimeDisplay[buff] = true;
                Player.buffTime[i] += 1;
            }
        }
    }

    public override void PostUpdateRunSpeeds() {
        Player.maxRunSpeed *= speedMult;
        Player.accRunSpeed *= speedMult;

        Player.jumpSpeed *= speedMult;
        Player.jumpHeight = (int)(Player.jumpHeight/System.MathF.Pow(speedMult, 2));
        Player.jumpSpeedBoost *= speedMult;
        
        Player.maxFallSpeed *= speedMult;
        Player.gravity *= speedMult;
    }

    private static void HookLeftClick(On_ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        DepositedFavItem = Main.mouseItem.favorited;
        orig(inv, context, slot);
        if(Configs.VanillaImprovements.Instance.favoriteInChest && context == ItemSlot.Context.ChestItem && DepositedFavItem) inv[slot].favorited = true;
    }

    private static void HookRestock(On_ChestUI.orig_Restock orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (sync && Configs.VanillaImprovements.Instance.favoriteInChest) Utility.RunWithHiddenItems(items, i => i.favorited, () => orig());
        else orig();
    }
    private static void HookLootAll(On_ChestUI.orig_LootAll orig) {
        ChestUI.GetContainerUsageInfo(out bool sync, out Item[] items);
        if (sync && Configs.VanillaImprovements.Instance.favoriteInChest) Utility.RunWithHiddenItems(items, i => i.favorited, () => orig());
        else orig();
    }

    private static void HookUpdateBiomes(On_Player.orig_UpdateBiomes orig, Player self) {
        SpymPlayer spymPlayer = self.GetModPlayer<SpymPlayer>();
        if (!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) {
            orig(self);
            return;
        }
        Vector2 center = self.Center;
        self.Center = spymPlayer.biomeLockPosition.Value;
        orig(self);
        self.Center = center;
    }


    private static bool _ilRedo;
    private static Vector2? _ilOriginalScanPosition;
    private static SpymPlayer? _ilSpymPlayer;
    private static bool _ilScanOreFinderData;
    private static void ILScanAndExportToMain(ILContext il) {
        const byte ArgSettings = 1;

        ILCursor cursor = new(il);
        ILLabel? ifLabel = null;
        FieldInfo BiomeScanCenterPositionInWorldField = typeof(SceneMetricsScanSettings).GetField(nameof(SceneMetricsScanSettings.BiomeScanCenterPositionInWorld), BindingFlags.Public | BindingFlags.Instance)!;
        if (!cursor.TryGotoNext(i => i.MatchLdflda(BiomeScanCenterPositionInWorldField)) || !cursor.TryGotoNext(MoveType.After, i => i.MatchBrfalse(out ifLabel))) {
            SpikysMod.Instance.Logger.Error($"\"{nameof(SceneMetrics)}.{nameof(SceneMetrics.ScanAndExportToMain)}\" il hook could not be applied");
            return;
        }

        ILLabel redoLabel = cursor.DefineLabel();

        cursor.Emit(OpCodes.Ldarg_S, ArgSettings);
        cursor.EmitDelegate((SceneMetricsScanSettings settings) => {
            _ilOriginalScanPosition = null;
            _ilSpymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
            _ilRedo = Main.netMode != NetmodeID.Server
                && settings.BiomeScanCenterPositionInWorld == Main.LocalPlayer.Center
                && _ilSpymPlayer.biomeLock && _ilSpymPlayer.biomeLockPosition.HasValue;
        });
        cursor.MarkLabel(redoLabel);
        cursor.Emit(OpCodes.Ldarg_S, ArgSettings);
        cursor.EmitDelegate((SceneMetricsScanSettings settings) => {
            if (_ilRedo) {
                _ilOriginalScanPosition = settings.BiomeScanCenterPositionInWorld;
                settings.BiomeScanCenterPositionInWorld = _ilSpymPlayer!.biomeLockPosition;
                _ilScanOreFinderData = settings.ScanOreFinderData;
                settings.ScanOreFinderData = false;
            } else if (_ilOriginalScanPosition.HasValue) {
                settings.BiomeScanCenterPositionInWorld = _ilOriginalScanPosition;
                settings.ScanOreFinderData = _ilScanOreFinderData;
            }
            return settings;
        });
        cursor.Emit(OpCodes.Starg_S, ArgSettings);

        cursor.GotoLabel(ifLabel!, MoveType.Before);
        cursor.EmitDelegate(() => {
            if (!_ilRedo) return false;
            _ilRedo = false;
            return true;
        });
        cursor.Emit(OpCodes.Brtrue, redoLabel);
    }

    private static void HookMinecartDamage(On_Player.orig_GetMinecartDamage orig, Player self, float currentSpeed, out int damage, out float knockback) {
        SpymSystem.FixedDamage = self.GetModPlayer<SpymPlayer>().fixedDamage;
        orig(self, currentSpeed, out damage, out knockback);
        SpymSystem.FixedDamage = false;
    }
    private static void HookProjDamage(On_Projectile.orig_Damage orig, Projectile self) {
        SpymSystem.FixedDamage = Main.player[self.owner].GetModPlayer<SpymPlayer>().fixedDamage;
        orig(self);
        SpymSystem.FixedDamage = false;
    }

    private static float HookGetPowerMultiplier(On_Player.orig_Fishing_GetPowerMultiplier orig, Player self, Item pole, Item bait)
        => System.MathF.Max(Main.LocalPlayer.GetModPlayer<SpymPlayer>().minFishingPower, orig(self, pole, bait));
    private void HookGetFishingPondState(On_Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
        orig(x, y, out lava, out honey, out numWaters, out chumCount);
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().minFishingPower >= 1) numWaters = 1000;
    }

    private static bool HookHasUnityPotion(On_Player.orig_HasUnityPotion orig, Player self) => (Configs.VanillaImprovements.Instance.infoAccPlus && ForcedUnityPotion(self)) || orig(self);
    private static void HookTakeUnityPotion(On_Player.orig_TakeUnityPotion orig, Player self) {
        if (Configs.VanillaImprovements.Instance.infoAccPlus && ForcedUnityPotion(self)) return;
        orig(self);
    }

    public static bool ForcedUnityPotion(Player player) => player.HasItem(ItemID.CellPhone);

    private static string HookBuffTooltip(On_Main.orig_GetBuffTooltip orig, Player player, int buffType)
        => buffType == BuffID.MonsterBanner && Configs.VanillaImprovements.Instance.bannerBuff ? Language.GetTextValue($"{Localization.Keys.Buffs}.Banner") : orig(player, buffType);

    public static void OnItemTranfer(ItemSlot.ItemTransferInfo info) => DepositedFavItem &= info.FromContenxt == 21 && info.ToContext.InRange(0, 4);
    public static bool DepositedFavItem { get; private set; }
}
