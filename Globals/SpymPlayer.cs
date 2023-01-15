using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using SPYM.Configs;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using System;
using Terraria.Graphics.Capture;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System.Reflection;
using Mono.Cecil.Cil;

namespace SPYM.Globals;

public class SpymPlayer : ModPlayer {

    public static SpymPlayer? InCalledItemCheckOf { get; private set; }
    public static int InProjDamagePlayer { get; private set; }

    private bool swappedHotBar;

    private readonly HashSet<int> hiddenBuffs = new();

    public bool dpsMeter;

    public float timeWarp;

    public float speedMult;

    public float spawnRateBoost;
    public float tallyMult;

    public bool weatherRadio;

    public float eventsBoost;
    
    public bool orePriority;
    public int prioritizedOre = -1;

    public bool fishGuide;

    public int rightClickedSlot;

    public int npcExtraRerolls;

    public bool biomeLock;
    public Vector2? biomeLockPosition;

    public override void Load() {
        On.Terraria.Player.HasUnityPotion += HookHasUnityPotion;
        On.Terraria.Player.TakeUnityPotion += HookTakeUnityPotion;

        On.Terraria.Projectile.GetFishingPondState += HookGetFishingPondState;

        On.Terraria.Main.GetBuffTooltip += HookBuffTooltip;
        
        On.Terraria.Player.Fishing_GetPowerMultiplier += HookGetPowerMultiplier;
        On.Terraria.UI.ItemSlot.RightClick_FindSpecialActions += HookRightClickPlus;
        On.Terraria.Main.DamageVar += HookDamageVar;
        On.Terraria.Projectile.Damage += HookProjDamage;
        On.Terraria.Player.GetMinecartDamage += HookMinecartDamage;

        On.Terraria.Player.UpdateBiomes += HookUpdateBiomes;
        IL.Terraria.SceneMetrics.ScanAndExportToMain += ILScanAndExportToMain;
    }

    private static bool _ilRedo;
    private static Vector2? _ilOriginalScanPosition;
    private static SpymPlayer? _ilSpymPlayer;
    private static bool _ilScanOreFinderData;


    private static  void ILScanAndExportToMain(ILContext il) {
        const byte ArgsSettings = 1;

        ILCursor cursor = new(il);
        ILLabel? ifLabel = null;
        FieldInfo BiomeScanCenterPositionInWorldField = typeof(SceneMetricsScanSettings).GetField(nameof(SceneMetricsScanSettings.BiomeScanCenterPositionInWorld), BindingFlags.Public | BindingFlags.Instance)!;
        if (!cursor.TryGotoNext(i => i.MatchLdflda(BiomeScanCenterPositionInWorldField)) || !cursor.TryGotoNext(MoveType.After, i => i.MatchBrfalse(out ifLabel))) {
            SpikysMod.Instance.Logger.Error($"\"{nameof(SceneMetrics)}.{nameof(SceneMetrics.ScanAndExportToMain)}\" il hook could not be applied");
            return;
        }

        ILLabel redoLabel = cursor.DefineLabel();

        cursor.Emit(OpCodes.Ldarga_S, ArgsSettings);
        cursor.EmitDelegate((ref SceneMetricsScanSettings settings) => {
            _ilOriginalScanPosition = null;
            _ilSpymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
            _ilRedo = Main.netMode != NetmodeID.Server
                && settings.BiomeScanCenterPositionInWorld == Main.LocalPlayer.Center
                && _ilSpymPlayer.biomeLock && _ilSpymPlayer.biomeLockPosition.HasValue;
        });
        cursor.MarkLabel(redoLabel);
        cursor.Emit(OpCodes.Ldarga_S, ArgsSettings);
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
        => buffType == BuffID.MonsterBanner && ServerConfig.Instance.bannerBuff ? Language.GetTextValue("Mods.SPYM.Tooltips.bannerBuff") : orig(player, buffType);

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

        if(player is null || !player.dpsMeter) return orig(dmg, luck);
        return (int)Math.Round(dmg);
    }

    public override void ResetEffects() {
        npcExtraRerolls = 0;
        eventsBoost = 1f;
        timeWarp = 1;
        tallyMult = 1f;
        spawnRateBoost = 1;
        speedMult = 1;
        weatherRadio = false;
        fishGuide = false;
        dpsMeter = false;
        biomeLock = false;
        orePriority = false;

        foreach (int buff in hiddenBuffs) Main.buffNoTimeDisplay[buff] = false;
        hiddenBuffs.Clear();
    }

    public override void PreUpdateBuffs() {
        if (ServerConfig.Instance.frozenBuffs && (Utility.BossAlive() || NPC.BusyWithAnyInvasionOfSorts())) {
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

        Player.jumpSpeed *= MathF.Sqrt(speedMult);
        Player.jumpSpeedBoost *= speedMult;
        
        Player.maxFallSpeed *= speedMult;
        Player.gravity *= speedMult;
    }

    public override void ProcessTriggers(TriggersSet triggersSet) {

        if (SpikysMod.FavoritedBuff.JustPressed) FavoritedBuff();
        if (orePriority && SpikysMod.MetalDetectorTarget.JustPressed && Player.HeldItem.pick > 0 && Player.IsTargetTileInItemRange(Player.HeldItem))
            prioritizedOre = Main.tile[Player.tileTargetX, Player.tileTargetY].TileType;
        
        if (Main.playerInventory && (!Main.HoverItem.IsAir || !Main.mouseItem.IsAir)) {
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
            else if(!swappedHotBar) {
                swappedHotBar = true;
                SwapHeld(slot);
            }
        }
    }

    public override void SetControls() {
        if (Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    private void SwapHeld(int destSlot) {
        int sourceSlot = !Main.mouseItem.IsAir ? 58 : Array.FindIndex(Player.inventory, i => i.type == Main.HoverItem.type && i.stack == Main.HoverItem.stack && i.prefix == Main.HoverItem.prefix);
        (Player.inventory[destSlot], Player.inventory[sourceSlot]) = (Player.inventory[sourceSlot], Player.inventory[destSlot]);
        if (sourceSlot == 58) Main.mouseItem = Player.inventory[sourceSlot].Clone();
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


    public override bool PreItemCheck(){
        bool canRightClick = Player.controlUseTile && !Player.tileInteractionHappened && Player.releaseUseItem && !Player.controlUseItem && !Player.mouseInterface && !CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine;
        if (canRightClick && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && (rightClickedSlot != -1 || Player.selectedItem < 10)) {
            if (rightClickedSlot == -1) rightClickedSlot = Player.selectedItem;
            ItemSlot.RightClick(Player.inventory, 0, rightClickedSlot);
            if (!Main.mouseItem.IsAir) Player.DropSelectedItem();
            return false;
        }

        rightClickedSlot = -1;
        InCalledItemCheckOf = this;
        return true;
    }

    public override void PostItemCheck() {
        InCalledItemCheckOf = null;
    }


    private static bool HookRightClickPlus(On.Terraria.UI.ItemSlot.orig_RightClick_FindSpecialActions orig, Item[] inv, int context, int slot, Player player) {
        bool mRR = Main.mouseRightRelease;
        int stack = Main.stackSplit;
        bool res = orig(inv, context, slot, player);
        if (Main.mouseRightRelease != mRR && mRR) {
            Main.stackSplit = stack;
            ItemSlot.RefreshStackSplitCooldown();
        }
        return res;
    }


    private static bool HookHasUnityPotion(On.Terraria.Player.orig_HasUnityPotion orig, Player self) => (ServerConfig.Instance.infoAccPlus && self.HasItem(ItemID.CellPhone)) || orig(self);
    private static void HookTakeUnityPotion(On.Terraria.Player.orig_TakeUnityPotion orig, Player self) {
        if (ServerConfig.Instance.infoAccPlus && !self.HasItem(ItemID.CellPhone)) orig(self);
    }


    private static float HookGetPowerMultiplier(On.Terraria.Player.orig_Fishing_GetPowerMultiplier orig, Player self, Item pole, Item bait) {
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().fishGuide) return 1.2f * 1.1f * 1.3f * 1.1f; // Not done with the tml hook to prevent other global items to edit the value (+/-)
        return orig(self, pole, bait);
    }

    private void HookGetFishingPondState(On.Terraria.Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
        orig(x, y, out lava, out honey, out numWaters, out chumCount);
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().fishGuide) numWaters = 300;
    }

}
