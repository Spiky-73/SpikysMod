using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Terraria;

namespace SPYM;
public static class Utility {


    public static Item? LastStack(this Player player, Item item, bool notArg = false) {
        for (int i = player.inventory.Length - 1 - 8; i >= 0; i--) {
            if (item.type == player.inventory[i].type && (!notArg || player.inventory[i] != item))
                return player.inventory[i];
        }
        for (int i = player.inventory.Length - 1; i >= 8; i--) {
            if (item.type == player.inventory[i].type && (!notArg || player.inventory[i] != item))
                return player.inventory[i];
        }
        return null;
    }

    public static Item? SmallestStack(this Player player, Item item, bool notArg = false) {
        Item? currentMin = null;
        for (int i = player.inventory.Length - 1; i >= 0; i--) {
            if (item.type == player.inventory[i].type
                    && (currentMin is null || player.inventory[i].stack < currentMin.stack)
                    && (!notArg || player.inventory[i] != item))
                currentMin = player.inventory[i];
        }
        return currentMin;
    }

    public static bool BusyWithInvasion() => Main.invasionType != 0;

    public static bool BossAlive() {
        foreach (NPC npc in Main.npc) {
            if (npc.active && npc.boss) return true;
        }
        return false;
    }

    public static bool IsEquipable(this Item item)
        => item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0 || item.accessory || Main.projHook[item.shoot] || item.mountType != -1 || (item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType]));

    public static int Snap(this int i, int increment) => (int)System.MathF.Round((float)i / increment) * increment;

    public enum SnapMode {
        Round,
        Ceiling,
        FLoor
    }
    public static float Snap(this float f, float increment, SnapMode mode = SnapMode.Round) {
        float val = f / increment;
        return mode switch {
            SnapMode.Ceiling => System.MathF.Ceiling(val),
            SnapMode.FLoor => System.MathF.Floor(val),
            SnapMode.Round or _ => System.MathF.Round(val),
        }* increment;
    }

    public static bool InChest(this Player player, [MaybeNullWhen(false)] out Item[] chest) => (chest = player.Chest()) is not null;
    public static Item[]? Chest(this Player player) => player.chest switch {
        > -1 => Main.chest[player.chest].item,
        -2 => player.bank.item,
        -3 => player.bank2.item,
        -4 => player.bank3.item,
        -6 => player.bank4.item,
        _ => null
    };

    public static void RunWithHiddenItem(Item[] chest, System.Predicate<Item> hidden, System.Action action) {
        Dictionary<int, Item> hiddenItems = new();
        for (int i = 0; i < chest.Length; i++) {
            if (!hidden(chest[i])) continue;
            hiddenItems[i] = chest[i];
            chest[i] = new();
        }
        action();
        foreach ((int slot, Item item) in hiddenItems) {
            chest[slot] = item;
        }
    }
}