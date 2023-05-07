using System.Collections.Generic;
using Terraria;

namespace SPYM.VanillaImprovements;

public static class Buffs {
    
    public static void FreezeBuffs(Player Player) {
        if (!Utility.BossAlive() && !NPC.BusyWithAnyInvasionOfSorts()) return;

        for (int i = 0; i < Player.buffType.Length; i++) {
            int buff = Player.buffType[i];
            if (!_hiddenBuffs.Contains(buff) && (Main.debuff[buff] || Main.buffNoTimeDisplay[buff])) continue;

            _hiddenBuffs.Add(buff);
            Main.buffNoTimeDisplay[buff] = true;
            Player.buffTime[i] += 1;
        }
    }

    public static void UnhideBuffs() {
        foreach (int buff in _hiddenBuffs) Main.buffNoTimeDisplay[buff] = false;
        _hiddenBuffs.Clear();
    }

    private static readonly HashSet<int> _hiddenBuffs = new();

}