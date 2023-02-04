using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
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
using Terraria.GameContent.UI;

namespace SPYM.Globals;

public class SpymPlayer : ModPlayer {

    public static SpymPlayer? InCalledItemCheckOf { get; private set; }
    public static int InProjDamagePlayer { get; private set; }

    private bool swappedHotBar;

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

    public int[] lastTypeOnInv = new int[58];
    public int chest; // reseted after the player updadte, later than player.chest
    public int[] lastTypeOnChest = new int[40];

    public SpymPlayer() => ResetEffects();

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

        On.Terraria.UI.ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        ItemSlot.OnItemTransferred += ItemSlotTranfer;
        On.Terraria.Player.GetItem += HookGetItem;

        On.Terraria.Player.OpenChest += HookOpenChest;
    }

    private static void HookOpenChest(On.Terraria.Player.orig_OpenChest orig, Player self, int x, int y, int newChest) {
        orig(self, x, y, newChest);
        SpymPlayer spymPlayer = self.GetModPlayer<SpymPlayer>();
        spymPlayer.lastTypeOnChest = new int[self.Chest()!.Length];
    }

    private int leftClickedSlot;
    private static readonly MethodInfo FillEmptyMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot", BindingFlags.Instance | BindingFlags.NonPublic, new Type[]{typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int)})!;
    private static readonly MethodInfo FillOccupiedMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot", BindingFlags.Instance | BindingFlags.NonPublic, new Type[]{typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int)})!;
    private static readonly MethodInfo FillEmptVoidMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new Type[]{typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int)})!;
    private static readonly MethodInfo FillOccupiedVoidMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new Type[]{typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int)})!;

    private void HookLeftClick(On.Terraria.UI.ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        leftClickedSlot = slot;
        int type = Main.mouseItem.type, stack = Main.mouseItem.stack, prefix = Main.mouseItem.prefix;
        bool fav = Main.mouseItem.favorited;
        orig(inv, context, slot);
        if(Configs.ServerConfig.Instance.favoritedItemsInChest && fav && context == 3 && inv[slot].type == type && inv[slot].stack == stack && inv[slot].prefix == prefix) inv[slot].favorited = true;
    }
    private void ItemSlotTranfer(ItemSlot.ItemTransferInfo info) {
        SpymPlayer spymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        for (int i = 0; i < spymPlayer.lastTypeOnInv.Length; i++) {
            if(spymPlayer.lastTypeOnInv[i] == info.ItemType) spymPlayer.lastTypeOnInv[i] = 0;
        }
        if (!(info.FromContenxt == 21 && info.ToContext.InRange(0,4))) return;
        if(info.ToContext.InRange(3, 4)) spymPlayer.lastTypeOnChest[leftClickedSlot] = info.ItemType;
        else spymPlayer.lastTypeOnInv[leftClickedSlot] = info.ItemType;
    }
    private static Item HookGetItem(On.Terraria.Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        if (Configs.ClientConfig.Instance.smartPickup == Configs.SmartPickupLevel.Off || (Configs.ClientConfig.Instance.smartPickup == Configs.SmartPickupLevel.FavoriteOnly && !newItem.favorited) || newItem.noGrabDelay > 0 || newItem.uniqueStack && self.HasItem(newItem.type)) return orig(self, plr, newItem, settings);
        
        SpymPlayer spymPlayer = self.GetModPlayer<SpymPlayer>();
        int i;
        if((i = Array.IndexOf(spymPlayer.lastTypeOnInv, newItem.type)) != -1) {
            bool gotItem = false;
            object[] args = new object[] { plr, newItem, settings, newItem, i };
            if (spymPlayer.Player.inventory[i].type == ItemID.None) gotItem = (bool)FillEmptyMethod.Invoke(self, args)!;
            else if (spymPlayer.Player.inventory[i].type == newItem.type && newItem.maxStack > 1) gotItem = (bool)FillOccupiedMethod.Invoke(self, args)!;
            else if (newItem.favorited || !spymPlayer.Player.inventory[i].favorited) (spymPlayer.Player.inventory[i], newItem) = (newItem, spymPlayer.Player.inventory[i]);
            return gotItem ? new() : orig(self, plr, newItem, settings);
        }
        else if(spymPlayer.chest != -1 && (i = Array.IndexOf(spymPlayer.lastTypeOnChest, newItem.type)) != -1){
            bool gotItem = false;
            Item[] chest = self.Chest(spymPlayer.chest);
            object[] args = new object[] { plr, chest, newItem, settings, newItem, i };
            if (spymPlayer.Player.inventory[i].type == ItemID.None) gotItem = (bool)FillEmptVoidMethod.Invoke(self, args)!;
            else if (spymPlayer.Player.inventory[i].type == newItem.type && newItem.maxStack > 1) gotItem = (bool)FillOccupiedVoidMethod.Invoke(self, args)!;
            else if (newItem.favorited || !spymPlayer.Player.inventory[i].favorited) (spymPlayer.Player.inventory[i], newItem) = (newItem, spymPlayer.Player.inventory[i]);
            if(Main.netMode == NetmodeID.MultiplayerClient && self.chest > -1) NetMessage.SendData(MessageID.SyncChestItem, number: spymPlayer.chest, number2: i);
            return gotItem ? new() : orig(self, plr, newItem, settings);
        }
        else return orig(self, plr, newItem, settings);
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
        => buffType == BuffID.MonsterBanner && Configs.ServerConfig.Instance.bannerBuff ? Language.GetTextValue("Mods.SPYM.Tooltips.bannerBuff") : orig(player, buffType);

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

        if (SpikysMod.FavoritedBuff.JustPressed) FavoritedBuff();
        if (orePriority && SpikysMod.MetalDetectorTarget.JustPressed && Player.HeldItem.pick > 0 && Player.IsTargetTileInItemRange(Player.HeldItem))
            prioritizedOre = Main.tile[Player.tileTargetX, Player.tileTargetY].TileType;

        foreach((ModKeybind kb, BuilderAccTogglesUI.GetIsAvailablemethod isAvailable, BuilderAccTogglesUI.PerformClickMethod onClick) in SpikysMod.BuilderAccToggles){
            if(kb.JustPressed && isAvailable(Player)) onClick(Player);
        }

        if (Configs.ClientConfig.Instance.itemSwap && Main.playerInventory && (!Main.HoverItem.IsAir || !Main.mouseItem.IsAir)) {
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
            else swappedHotBar = false;

            if(slot != -1 && !swappedHotBar) {
                swappedHotBar = true;
                SwapHeld(slot);
            }
        }
    }

    public override void SetControls() {
        if (Configs.ClientConfig.Instance.fastRightClick && Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    private void SwapHeld(int destSlot) {
        int sourceSlot = !Main.mouseItem.IsAir ? 58 : Array.FindIndex(Player.inventory, i => i.type == Main.HoverItem.type && i.stack == Main.HoverItem.stack && i.prefix == Main.HoverItem.prefix);
        (Player.inventory[destSlot], Player.inventory[sourceSlot]) = (Player.inventory[sourceSlot], Player.inventory[destSlot]);
        if (sourceSlot == 58) Main.mouseItem = Player.inventory[sourceSlot].Clone();
        SoundEngine.PlaySound(SoundID.Grab);
    }

    private void FavoritedBuff() => Utility.RunWithHiddenItems(Player.inventory, i => !i.favorited, Player.QuickBuff);


    public override bool PreItemCheck(){
        if (Configs.ClientConfig.Instance.inventoryRightClick
                && Player.controlUseTile && Player.releaseUseItem && !Player.controlUseItem && !Player.tileInteractionHappened
                && !Player.mouseInterface && !CaptureManager.Instance.Active && !Main.HoveringOverAnNPC && !Main.SmartInteractShowingGenuine 
                && Main.HoverItem.IsAir && Player.altFunctionUse == 0 && Player.selectedItem < 10) {
            ItemSlot.RightClick(Player.inventory, 0, Player.selectedItem);
            if (!Main.mouseItem.IsAir) Player.DropSelectedItem();
            return false;
        }
        InCalledItemCheckOf = this;
        return true;
    }

    public override void PostItemCheck() {
        InCalledItemCheckOf = null;
        chest = Player.chest;
    }


    private static bool HookRightClickPlus(On.Terraria.UI.ItemSlot.orig_RightClick_FindSpecialActions orig, Item[] inv, int context, int slot, Player player) {
        bool mRR = Main.mouseRightRelease;
        int stack = Main.stackSplit;
        bool res = orig(inv, context, slot, player);
        if (Configs.ClientConfig.Instance.fastRightClick && Main.mouseRightRelease != mRR && mRR) {
            Main.stackSplit = stack;
            ItemSlot.RefreshStackSplitCooldown();
        }
        return res;
    }


    private static bool HookHasUnityPotion(On.Terraria.Player.orig_HasUnityPotion orig, Player self) => (Configs.ServerConfig.Instance.infoAccPlus && self.HasItem(ItemID.CellPhone)) || orig(self);
    private static void HookTakeUnityPotion(On.Terraria.Player.orig_TakeUnityPotion orig, Player self) {
        if (Configs.ServerConfig.Instance.infoAccPlus && !self.HasItem(ItemID.CellPhone)) orig(self);
    }


    private static float HookGetPowerMultiplier(On.Terraria.Player.orig_Fishing_GetPowerMultiplier orig, Player self, Item pole, Item bait) {
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().maxFishingPower) return 1.2f * 1.1f * 1.3f * 1.1f; // Not done with the tml hook to prevent other global items to edit the value (+/-)
        return orig(self, pole, bait);
    }

    private void HookGetFishingPondState(On.Terraria.Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
        orig(x, y, out lava, out honey, out numWaters, out chumCount);
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().maxFishingPower) numWaters = 1000;
    }

}
