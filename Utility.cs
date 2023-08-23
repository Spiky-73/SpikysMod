using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Terraria;

namespace SPYM;
public static class Utility {

    public enum InclusionFlag {
        Min = 0x01,
        Max = 0x10,
        Both = Min|Max
    }

    public static bool InRange<T>(this T self, T min, T max, InclusionFlag flags = InclusionFlag.Both) where T: System.IComparable<T> {
        int l = self.CompareTo(min);
        int r = self.CompareTo(max);
        return (l > 0 || (flags.HasFlag(InclusionFlag.Min) && l == 0)) && (r < 0 || (flags.HasFlag(InclusionFlag.Max) && r == 0));
    }

    public static bool BossAlive() {
        foreach (NPC npc in Main.npc) {
            if (npc.active && npc.boss) return true;
        }
        return false;
    }

    public enum SnapMode {
        Round,
        Ceiling,
        Floor
    }
    public static int Snap(this int i, int increment) => (int)System.MathF.Round((float)i / increment) * increment;
    public static float Snap(this float f, float increment, SnapMode mode = SnapMode.Round) {
        float val = f / increment;
        return mode switch {
            SnapMode.Ceiling => System.MathF.Ceiling(val),
            SnapMode.Floor => System.MathF.Floor(val),
            SnapMode.Round or _ => System.MathF.Round(val),
        }* increment;
    }

    public static bool InChest(this Player player, [MaybeNullWhen(false)] out Item[] chest) => (chest = player.Chest()) is not null;
    [return: NotNullIfNotNull("chest")]
    public static Item[]? Chest(this Player player, int? chest = null) => (chest ?? player.chest) switch {
        > -1 => Main.chest[player.chest].item,
        -2 => player.bank.item,
        -3 => player.bank2.item,
        -4 => player.bank3.item,
        -5 => player.bank4.item,
        _ => null
    };

    public static void RunWithHiddenItems(Item[] chest, System.Predicate<Item> hidden, System.Action action) {
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
    
    public static int BoostRate(int chanceDenominator, float boost) {
        if (boost <= 1f) return chanceDenominator;
        chanceDenominator = (int)System.MathF.Ceiling(System.MathF.Pow(2, System.MathF.Pow(System.MathF.Log2(chanceDenominator), 1 / boost)));
        return chanceDenominator;
    }
}