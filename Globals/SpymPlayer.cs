using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.UI;
using System;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;
using Terraria.GameContent.UI;

namespace SPYM.Globals;

public class SpymPlayer : ModPlayer {

    public static SpymPlayer? InCalledItemCheckOf { get; private set; }
    public static int InProjDamagePlayer { get; private set; }

    private readonly HashSet<int> hiddenBuffs = new();

    public bool fixDamage;
    public bool forcedSeasons;
    public bool maxFishingPower;

    public bool orePriority;
    public int prioritizedOre = -1;
    public bool biomeLock;
    public Vector2? biomeLockPosition;

    public float timeMult; // TODO sync
    public float speedMult;
    public float spawnRateMult;
    public float lootMult;
    public float eventsMult;
    public int npcExtraRerolls;

    public override void Load() {
        On.Terraria.Projectile.GetFishingPondState += HookGetFishingPondState;

        On.Terraria.Player.HasUnityPotion += HookHasUnityPotion;
        On.Terraria.Player.TakeUnityPotion += HookTakeUnityPotion;

        On.Terraria.Main.GetBuffTooltip += HookBuffTooltip;
        
        On.Terraria.Player.Fishing_GetPowerMultiplier += HookGetPowerMultiplier;
        On.Terraria.UI.ItemSlot.RightClick_FindSpecialActions += HookRightClickPlus;
        On.Terraria.Main.DamageVar += HookDamageVar;
        On.Terraria.Projectile.Damage += HookProjDamage;
        On.Terraria.Player.GetMinecartDamage += HookMinecartDamage;

        On.Terraria.Player.UpdateBiomes += HookUpdateBiomes;
        IL.Terraria.SceneMetrics.ScanAndExportToMain += ILScanAndExportToMain;

        On.Terraria.UI.ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        ItemSlot.OnItemTransferred += InventoryFeatures.OnItemTranfer;
        On.Terraria.Player.GetItem += HookGetItem;

        On.Terraria.Player.OpenChest += HookOpenChest;
    }

    public override void Initialize() => ResetEffects();

    private static void HookOpenChest(On.Terraria.Player.orig_OpenChest orig, Player self, int x, int y, int newChest) {
        orig(self, x, y, newChest);
        InventoryFeatures.OnOpenChest(self);
    }

    private void HookLeftClick(On.Terraria.UI.ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        InventoryFeatures.OnSlotLeftClick(slot);
        int type = Main.mouseItem.type, stack = Main.mouseItem.stack, prefix = Main.mouseItem.prefix;
        bool fav = Main.mouseItem.favorited;
        orig(inv, context, slot);
        if(Configs.ServerConfig.Instance.favoritedItemsInChest && fav && context == 3 && inv[slot].type == type && inv[slot].stack == stack && inv[slot].prefix == prefix) inv[slot].favorited = true;
    }

    private static Item HookGetItem(On.Terraria.Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        if(InventoryFeatures.SmartPickupEnabled(newItem) && InventoryFeatures.SmartGetItem(plr, self, ref newItem, settings)) return new();
        return orig(self, plr, newItem, settings);
    }


    private static bool _ilRedo;
    private static Vector2? _ilOriginalScanPosition;
    private static SpymPlayer? _ilSpymPlayer;
    private static bool _ilScanOreFinderData;

    private static  void ILScanAndExportToMain(ILContext il) {
        const byte ArgSettings = 1;

        ILCursor cursor = new(il);
        ILLabel? ifLabel = null;
        FieldInfo BiomeScanCenterPositionInWorldField = typeof(SceneMetricsScanSettings).GetField(nameof(SceneMetricsScanSettings.BiomeScanCenterPositionInWorld), BindingFlags.Public | BindingFlags.Instance)!;
        if (!cursor.TryGotoNext(i => i.MatchLdflda(BiomeScanCenterPositionInWorldField)) || !cursor.TryGotoNext(MoveType.After, i => i.MatchBrfalse(out ifLabel))) {
            SpikysMod.Instance.Logger.Error($"\"{nameof(SceneMetrics)}.{nameof(SceneMetrics.ScanAndExportToMain)}\" il hook could not be applied");
            return;
        }

        ILLabel redoLabel = cursor.DefineLabel();

        cursor.Emit(OpCodes.Ldarga_S, ArgSettings);
        cursor.EmitDelegate((ref SceneMetricsScanSettings settings) => {
            _ilOriginalScanPosition = null;
            _ilSpymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
            _ilRedo = Main.netMode != NetmodeID.Server
                && settings.BiomeScanCenterPositionInWorld == Main.LocalPlayer.Center
                && _ilSpymPlayer.biomeLock && _ilSpymPlayer.biomeLockPosition.HasValue;
        });
        cursor.MarkLabel(redoLabel);
        cursor.Emit(OpCodes.Ldarga_S, ArgSettings);
        cursor.EmitDelegate((ref SceneMetricsScanSettings settings) => {
            if (_ilRedo) {
                _ilOriginalScanPosition = settings.BiomeScanCenterPositionInWorld;
                settings.BiomeScanCenterPositionInWorld = _ilSpymPlayer!.biomeLockPosition;
                _ilScanOreFinderData = settings.ScanOreFinderData;
                settings.ScanOreFinderData = false;
            } else if (_ilOriginalScanPosition.HasValue) {
                settings.BiomeScanCenterPositionInWorld = _ilOriginalScanPosition;
                settings.ScanOreFinderData = _ilScanOreFinderData;

            }
        });

        cursor.GotoLabel(ifLabel!, MoveType.Before);
        cursor.EmitDelegate(() =>{
            if (!_ilRedo) return false;
            _ilRedo = false;
            return true;
        });
        cursor.Emit(OpCodes.Brtrue, redoLabel);
    }
    private static void HookUpdateBiomes(On.Terraria.Player.orig_UpdateBiomes orig, Player self) {
        SpymPlayer spymPlayer = self.GetModPlayer<SpymPlayer>();
        if(!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) {
            orig(self);
            return;
        }
        Vector2 center = self.Center;
        self.Center = spymPlayer.biomeLockPosition.Value;
        orig(self);
        self.Center = center;
    }

    private static string HookBuffTooltip(On.Terraria.Main.orig_GetBuffTooltip orig, Player player, int buffType)
        => buffType == BuffID.MonsterBanner && Configs.ServerConfig.Instance.bannerBuff ? Language.GetTextValue($"{LocKeys.Buffs}.Banner") : orig(player, buffType);

    private void HookMinecartDamage(On.Terraria.Player.orig_GetMinecartDamage orig, Player self, float currentSpeed, out int damage, out float knockback){
        InCalledItemCheckOf = self.GetModPlayer<SpymPlayer>();
        orig(self, currentSpeed, out damage, out knockback);
        InCalledItemCheckOf = null;
    }

    private static void HookProjDamage(On.Terraria.Projectile.orig_Damage orig, Projectile self) {
        InProjDamagePlayer = self.owner;
        orig(self);
        InProjDamagePlayer = -1;
    }

    private static int HookDamageVar(On.Terraria.Main.orig_DamageVar orig, float dmg, float luck){
        SpymPlayer? player;
        if(InCalledItemCheckOf != null) player = InCalledItemCheckOf;
        else if(InProjDamagePlayer != -1) player = Main.player[InProjDamagePlayer].GetModPlayer<SpymPlayer>();
        else player = null;

        if(player is null || !player.fixDamage) return orig(dmg, luck);
        return (int)Math.Round(dmg);
    }

    public override void ResetEffects() {
        forcedSeasons = false;
        maxFishingPower = false;
        fixDamage = false;
        biomeLock = false;
        orePriority = false;
        npcExtraRerolls = 0;
        eventsMult = 1f;
        timeMult = 1;
        lootMult = 1f;
        spawnRateMult = 1;
        speedMult = 1;

        foreach (int buff in hiddenBuffs) Main.buffNoTimeDisplay[buff] = false;
        hiddenBuffs.Clear();
    }

    public override void PreUpdateBuffs() {
        if (Configs.ServerConfig.Instance.frozenBuffs && (Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts())) {
            for (int i = 0; i < Player.buffType.Length; i++) {
                int buff = Player.buffType[i];
                if (Main.debuff[buff] || Main.buffNoTimeDisplay[buff]) continue;

                hiddenBuffs.Add(buff);
                Main.buffNoTimeDisplay[buff] = true;
                Player.buffTime[i] += 1;
            }
        }
    }

    public override void PostUpdateRunSpeeds() {
        Player.maxRunSpeed *= speedMult;
        Player.accRunSpeed *= speedMult;

        Player.jumpSpeed *= MathF.Sqrt(speedMult); // ? exact formula
        Player.jumpSpeedBoost *= speedMult;
        
        Player.maxFallSpeed *= speedMult;
        Player.gravity *= speedMult;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {

        if (InventoryFeatures.FavoritedBuffKb.JustPressed) InventoryFeatures.FavoritedBuff(Player);
        if (orePriority && SpikysMod.PrioritizeOre.JustPressed && Player.HeldItem.pick > 0 && Player.IsTargetTileInItemRange(Player.HeldItem))
            prioritizedOre = Main.tile[Player.tileTargetX, Player.tileTargetY].TileType;

        foreach((ModKeybind kb, BuilderAccTogglesUI.GetIsAvailablemethod isAvailable, BuilderAccTogglesUI.PerformClickMethod onClick) in SpikysMod.BuilderAccToggles){
            if(kb.JustPressed && isAvailable(Player)) onClick(Player);
        }

        if (InventoryFeatures.ItemSwap) InventoryFeatures.AttemptItemSwap(Player, triggersSet);
    }

    public override void SetControls() {
        if (InventoryFeatures.FastRightClick) InventoryFeatures.AttemptFastRightClick();
    }

    private void FavoritedBuff() => Utility.RunWithHiddenItems(Player.inventory, i => !i.favorited, Player.QuickBuff);


    public override bool PreItemCheck(){
        if (InventoryFeatures.ItemRightClick && InventoryFeatures.AttemptItemRightClick(Player)) return false;
        InCalledItemCheckOf = this;
        return true;
    }

    public override void PostItemCheck() {
        InCalledItemCheckOf = null;
    }
    
    public override void PostUpdate(){
        InventoryFeatures.PostUpdate(Player);
    }


    private static bool HookRightClickPlus(On.Terraria.UI.ItemSlot.orig_RightClick_FindSpecialActions orig, Item[] inv, int context, int slot, Player player) {
        int stackSplit = Main.stackSplit;
        bool res = orig(inv, context, slot, player);
        if (InventoryFeatures.FastRightClick) InventoryFeatures.OnSlotRightClick(stackSplit);
        return res;
    }

    private static float HookGetPowerMultiplier(On.Terraria.Player.orig_Fishing_GetPowerMultiplier orig, Player self, Item pole, Item bait) {
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().maxFishingPower) return 1.2f * 1.1f * 1.3f * 1.1f; // Not done with the tml hook to prevent other global items to edit the value (+/-)
        return orig(self, pole, bait);
    }

    private void HookGetFishingPondState(On.Terraria.Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
        orig(x, y, out lava, out honey, out numWaters, out chumCount);
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().maxFishingPower) numWaters = 1000;
    }

    private static bool HookHasUnityPotion(On.Terraria.Player.orig_HasUnityPotion orig, Player self) => (ImprovedInfoAcc.Enabled && ImprovedInfoAcc.ForcedUnityPotion(self)) || orig(self);
    private static void HookTakeUnityPotion(On.Terraria.Player.orig_TakeUnityPotion orig, Player self) {
        if (ImprovedInfoAcc.Enabled && ImprovedInfoAcc.ForcedUnityPotion(self)) return;
        orig(self);
    }

}
