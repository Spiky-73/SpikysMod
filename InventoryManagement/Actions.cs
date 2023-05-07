using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace SPYM.InventoryManagement;

public static class Actions {

    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;
    public static List<(ModKeybind, BuilderAccTogglesUI.GetIsAvailablemethod, BuilderAccTogglesUI.PerformClickMethod)> BuilderAccToggles { get; private set; } = null!;

    public static void Load() {
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Favorited Quick buff", Microsoft.Xna.Framework.Input.Keys.N);

        BuilderAccToggles = new() { (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Ruler Line", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.rulerLine,
            (Player player) => CycleAccState(player, 0)
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Ruler Grid", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.rulerLine,
            (Player player) => CycleAccState(player, 1)
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Auto Paint", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.rulerLine,
            (Player player) => CycleAccState(player, 2)
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Auto Actuator", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.rulerLine,
            (Player player) => CycleAccState(player, 3)
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Wire display", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.InfoAccMechShowWires,
            (Player player) => {
                    CycleAccState(player, 4, 3);
                    for (int i = 5; i < 8; i++) player.builderAccStatus[i] = player.builderAccStatus[4];
                    player.builderAccStatus[9] = player.builderAccStatus[4];
                }
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Forced Wires", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.InfoAccMechShowWires,
            (Player player) => CycleAccState(player, 8)
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Block Swap", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => true,
            (Player player) => CycleAccState(player, 10)
        ), (
            KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Biome Torches", Microsoft.Xna.Framework.Input.Keys.None),
            (Player player) => player.unlockedBiomeTorches,
            (Player player) => CycleAccState(player, 11)
        )};
    }

    
    public static void ProcessShortcuts(Player player){
        if (FavoritedBuffKb.JustPressed) FavoritedBuff(player);

        foreach ((ModKeybind kb, BuilderAccTogglesUI.GetIsAvailablemethod isAvailable, BuilderAccTogglesUI.PerformClickMethod onClick) in BuilderAccToggles) {
            if (kb.JustPressed && isAvailable(player)) onClick(player);
        }
    }

    public static void FavoritedBuff(Player player) => Utility.RunWithHiddenItems(player.inventory, i => !i.favorited, player.QuickBuff);
    public static void CycleAccState(Player player, int index, int cycle = 2) => player.builderAccStatus[index] = (player.builderAccStatus[index] + 1) % cycle;


    public static void SwapHeldItem(Player player, int destSlot) {
        int sourceSlot = !Main.mouseItem.IsAir ? 58 : System.Array.FindIndex(player.inventory, i => i.type == Main.HoverItem.type && i.stack == Main.HoverItem.stack && i.prefix == Main.HoverItem.prefix);
        (player.inventory[destSlot], player.inventory[sourceSlot]) = (player.inventory[sourceSlot], player.inventory[destSlot]);
        if (sourceSlot == 58) Main.mouseItem = player.inventory[sourceSlot].Clone();
        SoundEngine.PlaySound(SoundID.Grab);
    }
    public static void AttemptItemSwap(Player player, TriggersSet triggersSet) {
        if (!Main.playerInventory || Main.HoverItem.IsAir && Main.mouseItem.IsAir) return;

        int slot = -1;
        if (triggersSet.Hotbar1) slot = 0;
        else if (triggersSet.Hotbar2) slot = 1;
        else if (triggersSet.Hotbar3) slot = 2;
        else if (triggersSet.Hotbar4) slot = 3;
        else if (triggersSet.Hotbar5) slot = 4;
        else if (triggersSet.Hotbar6) slot = 5;
        else if (triggersSet.Hotbar7) slot = 6;
        else if (triggersSet.Hotbar8) slot = 7;
        else if (triggersSet.Hotbar9) slot = 8;
        else if (triggersSet.Hotbar10) slot = 9;
        else _swapped = false;
        if (slot == -1 || _swapped) return;

        _swapped = true;
        SwapHeldItem(player, slot);
    }

    public static bool AttemptItemRightClick(Player player) {
        if (!player.controlUseTile || !player.releaseUseItem || player.controlUseItem || player.tileInteractionHappened
                || player.mouseInterface || Terraria.Graphics.Capture.CaptureManager.Instance.Active || Main.HoveringOverAnNPC || Main.SmartInteractShowingGenuine
                || !Main.HoverItem.IsAir || player.altFunctionUse != 0 || player.selectedItem >= 10)
            return false;
        int split = Main.stackSplit;
        Main.stackSplit = 2;
        ItemSlot.RightClick(player.inventory, 0, player.selectedItem);
        if(Main.stackSplit == 2) Main.stackSplit = split;
        return true;
    }


    public static void AttemptFastRightClick() {
        if (Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }

    public static void FastRightClick(int stackSplit) {
        if (Main.stackSplit == stackSplit) return;
        Main.stackSplit = stackSplit;
        ItemSlot.RefreshStackSplitCooldown();
    }


    private static bool _swapped;
}