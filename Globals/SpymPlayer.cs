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

namespace SPYM.Globals;

public class SpymPlayer : ModPlayer {

    public bool fixedDamage;
    public bool forcedSeasons;
    
    public float minFishingPower;

    public bool orePriority;
    public int prioritizedOre = -1;

    public bool biomeLock;
    public Vector2? biomeLockPosition;

    public float timeMult; // TODO sync
    public float speedMult;
    public float spawnRateMult;
    public float lootBoost;
    public float eventsBoost;

    public int npcExtraRerolls;


    public override void Load() {
        On.Terraria.Projectile.GetFishingPondState += HookGetFishingPondState;
        On.Terraria.Player.Fishing_GetPowerMultiplier += HookGetPowerMultiplier;

        On.Terraria.Player.HasUnityPotion += HookHasUnityPotion;
        On.Terraria.Player.TakeUnityPotion += HookTakeUnityPotion;
        
        On.Terraria.Projectile.Damage += HookProjDamage;
        On.Terraria.Player.GetMinecartDamage += HookMinecartDamage;

        On.Terraria.Player.UpdateBiomes += HookUpdateBiomes;
        IL.Terraria.SceneMetrics.ScanAndExportToMain += ILScanAndExportToMain;

        On.Terraria.UI.ItemSlot.RightClick_FindSpecialActions += HookRightClick;
        
        On.Terraria.Player.OpenChest += HookOpenChest;
        On.Terraria.UI.ItemSlot.LeftClick_ItemArray_int_int += HookLeftClick;
        ItemSlot.OnItemTransferred += InventoryFeatures.Items.OnItemTranfer;
        On.Terraria.Player.GetItem += HookGetItem;

        On.Terraria.Main.GetBuffTooltip += HookBuffTooltip;

        On.Terraria.UI.ChestUI.LootAll += HookLootAll;
        On.Terraria.UI.ChestUI.Restock += HookRestock;
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

        InventoryFeatures.InventoryManagement.UnhideBuffs();
    }

    public override void SetControls() {
        if (Configs.ClientConfig.Instance.fastRightClick) InventoryFeatures.InventoryManagement.AttemptFastRightClick();
    }
    public override void ProcessTriggers(TriggersSet triggersSet) {
        InventoryFeatures.InventoryManagement.ProcessShortcuts(Player);
        if (Configs.ClientConfig.Instance.itemSwap) InventoryFeatures.InventoryManagement.AttemptItemSwap(Player, triggersSet);

        if (orePriority && SpikysMod.PrioritizeOre.JustPressed && Player.HeldItem.pick > 0 && Player.IsTargetTileInItemRange(Player.HeldItem))
            prioritizedOre = Main.tile[Player.tileTargetX, Player.tileTargetY].TileType;
    }

    public override bool PreItemCheck() {
        if (Configs.ClientConfig.Instance.itemRightClick && InventoryFeatures.InventoryManagement.AttemptItemRightClick(Player)) return false;
        SpymSystem.FixedDamage = fixedDamage;
        return true;
    }
    public override void PostItemCheck() {
        SpymSystem.FixedDamage = false;
    }

    public override void PostUpdate() {
        InventoryFeatures.Items.PostUpdate(Player);
    }

    public override void PreUpdateBuffs() {
        if (Configs.ServerConfig.Instance.frozenBuffs) InventoryFeatures.InventoryManagement.FreezeBuffs(Player);
    }

    public override void PostUpdateRunSpeeds() {
        Player.maxRunSpeed *= speedMult;
        Player.accRunSpeed *= speedMult;

        Player.jumpSpeed *= System.MathF.Sqrt(speedMult); // ? exact formula
        Player.jumpSpeedBoost *= speedMult;
        
        Player.maxFallSpeed *= speedMult;
        Player.gravity *= speedMult;
    }


    private static bool HookRightClick(On.Terraria.UI.ItemSlot.orig_RightClick_FindSpecialActions orig, Item[] inv, int context, int slot, Player player) {
        int stackSplit = Main.stackSplit;
        bool res = orig(inv, context, slot, player);
        if (Configs.ClientConfig.Instance.fastRightClick) InventoryFeatures.InventoryManagement.FastRightClick(stackSplit);
        return res;
    }


    private static void HookOpenChest(On.Terraria.Player.orig_OpenChest orig, Player self, int x, int y, int newChest) {
        orig(self, x, y, newChest);
        InventoryFeatures.Items.OnOpenChest(self);
    }
    private static void HookLeftClick(On.Terraria.UI.ItemSlot.orig_LeftClick_ItemArray_int_int orig, Item[] inv, int context, int slot) {
        InventoryFeatures.Items.OnSlotLeftClick(slot);
        bool fav = Main.mouseItem.favorited;
        orig(inv, context, slot);
        if (context == 3 && Configs.ServerConfig.Instance.favoritedItemsInChest && fav) inv[slot].favorited = true;
    }

    private static Item HookGetItem(On.Terraria.Player.orig_GetItem orig, Player self, int plr, Item newItem, GetItemSettings settings) {
        if (InventoryFeatures.Items.SmartPickupEnabled(newItem) && InventoryFeatures.Items.OnGetItem(plr, self, ref newItem, settings)) return new();
        return orig(self, plr, newItem, settings);
    }


    private static void HookRestock(On.Terraria.UI.ChestUI.orig_Restock orig) => Utility.RunWithHiddenItems(Main.LocalPlayer.Chest()!, i => i.favorited, () => orig());
    private static void HookLootAll(On.Terraria.UI.ChestUI.orig_LootAll orig) => Utility.RunWithHiddenItems(Main.LocalPlayer.Chest()!, i => i.favorited, () => orig());


    private static void HookUpdateBiomes(On.Terraria.Player.orig_UpdateBiomes orig, Player self) {
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
        cursor.EmitDelegate(() => {
            if (!_ilRedo) return false;
            _ilRedo = false;
            return true;
        });
        cursor.Emit(OpCodes.Brtrue, redoLabel);
    }


    private static void HookMinecartDamage(On.Terraria.Player.orig_GetMinecartDamage orig, Player self, float currentSpeed, out int damage, out float knockback) {
        SpymSystem.FixedDamage = self.GetModPlayer<SpymPlayer>().fixedDamage;
        orig(self, currentSpeed, out damage, out knockback);
        SpymSystem.FixedDamage = false;
    }
    private static void HookProjDamage(On.Terraria.Projectile.orig_Damage orig, Projectile self) {
        SpymSystem.FixedDamage = Main.player[self.owner].GetModPlayer<SpymPlayer>().fixedDamage;
        orig(self);
        SpymSystem.FixedDamage = false;
    }


    private static float HookGetPowerMultiplier(On.Terraria.Player.orig_Fishing_GetPowerMultiplier orig, Player self, Item pole, Item bait)
        => System.MathF.Max(Main.LocalPlayer.GetModPlayer<SpymPlayer>().minFishingPower, orig(self, pole, bait));
    private void HookGetFishingPondState(On.Terraria.Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount) {
        orig(x, y, out lava, out honey, out numWaters, out chumCount);
        if (Main.LocalPlayer.GetModPlayer<SpymPlayer>().minFishingPower >= 1) numWaters = 1000;
    }


    private static bool HookHasUnityPotion(On.Terraria.Player.orig_HasUnityPotion orig, Player self) => (Configs.ServerConfig.Instance.infoAccPlus && VanillaImprovements.InfoAccessories.ForcedUnityPotion(self)) || orig(self);
    private static void HookTakeUnityPotion(On.Terraria.Player.orig_TakeUnityPotion orig, Player self) {
        if (Configs.ServerConfig.Instance.infoAccPlus && VanillaImprovements.InfoAccessories.ForcedUnityPotion(self)) return;
        orig(self);
    }


    private static string HookBuffTooltip(On.Terraria.Main.orig_GetBuffTooltip orig, Player player, int buffType)
        => buffType == BuffID.MonsterBanner && Configs.ServerConfig.Instance.bannerBuff ? Language.GetTextValue($"{Localization.Keys.Buffs}.Banner") : orig(player, buffType);
}
