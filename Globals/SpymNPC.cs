﻿using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;
using Microsoft.Xna.Framework;

namespace SPYM.Globals;
public class SpymNPC : GlobalNPC {

    public override void Load() {
        On.Terraria.NPC.NPCLoot_DropItems += HookDropItem;
        On.Terraria.Utilities.UnifiedRandom.Next_int += HookRngNext_int;
        IL.Terraria.NPC.SpawnNPC += ILSpawnNPC;
        On.Terraria.NPC.NewNPC += HookNewNPC;
        IL.Terraria.GameContent.ItemDropRules.ItemDropResolver.ResolveRule += ILResolveRule;
    }

    private static Vector2? _ilNPCPosition;

    private static void ILResolveRule(ILContext il) {
        const byte ArgDropAttemptInfo = 2;

        ILCursor cursor = new(il);
        MethodInfo candropMethod = typeof(IItemDropRule).GetMethod(nameof(IItemDropRule.CanDrop), BindingFlags.Public | BindingFlags.Instance)!;
        if(!cursor.TryGotoNext(i => i.MatchCallvirt(candropMethod))){
            SpikysMod.Instance.Logger.Error($"\"{nameof(ItemDropResolver)}.ResolveRule\" il hook could not be applied");
            return;
        }
        cursor.GotoPrev().GotoPrev();
        cursor.Emit(OpCodes.Ldarga_S, ArgDropAttemptInfo);
        cursor.EmitDelegate((ref DropAttemptInfo info) => {
            SpymPlayer spymPlayer = info.player.GetModPlayer<SpymPlayer>();
            if (!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) return;
            _ilNPCPosition = info.npc.Center;
            info.npc.Center = spymPlayer.biomeLockPosition.Value;
        });
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(candropMethod));
        cursor.Emit(OpCodes.Ldarga_S, ArgDropAttemptInfo);
        cursor.EmitDelegate((bool res, ref DropAttemptInfo info) => {
            if (!_ilNPCPosition.HasValue) return res;
            info.npc.Center = _ilNPCPosition.Value;
            _ilNPCPosition = null;
            return res;
        });
    }

    private static bool _inSpawnNPC;
    private static int _ilNewNPC;
    private static int _ilAttempts, _ilCurrentTry;
    private static NPC? _ilRarestSpawn;
    private static int HookNewNPC(On.Terraria.NPC.orig_NewNPC orig, IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target) {
        int s = orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
        if(_inSpawnNPC) _ilNewNPC = s;
        return s;
    }
    private void ILSpawnNPC(ILContext il) {
        ILCursor cursor = new(il);

        const byte LocNewNPC = 30;
        const byte LocSpawnInfo = 25;

        ILLabel startLoop = il.DefineLabel();
        ILLabel endLoop = il.DefineLabel();
        
        MethodBase chooseSpawnMethod = typeof(NPCLoader).GetMethod(nameof(NPCLoader.ChooseSpawn), BindingFlags.Public | BindingFlags.Static, new System.Type[] {typeof(NPCSpawnInfo)})!;
        FieldInfo netModeField = typeof(Main).GetField(nameof(Main.netMode), BindingFlags.Static | BindingFlags.Public)!;

        if (!cursor.TryGotoNext(i => i.MatchCall(chooseSpawnMethod)) || !cursor.TryGotoNext(i => i.MatchLdsfld(netModeField))) {
            SpikysMod.Instance.Logger.Error($"\"{nameof(NPC)}.{nameof(NPC.SpawnNPC)}\" il hook could not be applied");
            return;
        }

        // Loop start detection
        cursor.Index = 0;
        cursor.GotoNext(i => i.MatchCall(chooseSpawnMethod));
        cursor.GotoPrev();

        // Loop init
        cursor.Emit(OpCodes.Ldloc_S, LocSpawnInfo);
        cursor.EmitDelegate((NPCSpawnInfo spawnInfo) => {
            _inSpawnNPC = true;
            _ilNewNPC = 200;
            _ilAttempts = 1 + spawnInfo.Player.GetModPlayer<SpymPlayer>().npcExtraRerolls;
            _ilCurrentTry = 0;
            _ilRarestSpawn = null;
        });
        cursor.Emit(OpCodes.Br, endLoop);

        // Loop start
        cursor.MarkLabel(startLoop);
        cursor.Emit(OpCodes.Ldc_I4, 200);
        cursor.Emit(OpCodes.Stloc_S, LocNewNPC);
        cursor.EmitDelegate<System.Action>(() => _ilNewNPC = 200);

        // Loop end detection
        cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode), BindingFlags.Static | BindingFlags.Public)!));
        cursor.GotoNext();

        // Loop pre-end
        cursor.EmitDelegate((int netMode) => {
            if(_ilRarestSpawn is null || Main.npc[_ilNewNPC].rarity > _ilRarestSpawn.rarity) _ilRarestSpawn = Main.npc[_ilNewNPC];
            Main.npc[_ilNewNPC] = new();
            _ilCurrentTry++;
        });

        // Loop end
        cursor.MarkLabel(endLoop);
        cursor.EmitDelegate(() => _ilCurrentTry < _ilAttempts);
        cursor.Emit(OpCodes.Brtrue, startLoop);

        // Post loop
        cursor.EmitDelegate(() => {
            _inSpawnNPC = false;
            Main.npc[_ilNewNPC] = _ilRarestSpawn;
        });

        cursor.Emit(OpCodes.Ldsfld, netModeField);
    }


    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
        float mult = player.GetModPlayer<SpymPlayer>().spawnRateMult;
        if(Utility.BossAlive() && Configs.ServerConfig.Instance.betterCalming && player.calmed || player.HasBuff(BuffID.PeaceCandle)) mult = 0;

        if(mult == 0) maxSpawns = 0;
        else {
            spawnRate = (int)(spawnRate / mult);
            maxSpawns = (int)(spawnRate * mult);
        }
    }


    private static bool _inDropItem;
    private static bool _bannerBuff;
    private static Player? _closestPlayer;
    private static void HookDropItem(On.Terraria.NPC.orig_NPCLoot_DropItems orig, NPC self, Player closestPlayer) {
        _inDropItem = true;
        _closestPlayer = closestPlayer;
        _bannerBuff = Configs.ServerConfig.Instance.bannerBuff && closestPlayer.HasNPCBannerBuff(Item.NPCtoBanner(self.BannerID()));
        orig(self, closestPlayer);
        _inDropItem = false;
        _closestPlayer = null;
    }
    private static int HookRngNext_int(On.Terraria.Utilities.UnifiedRandom.orig_Next_int orig, Terraria.Utilities.UnifiedRandom self, int maxValue){
        if (_inDropItem && _closestPlayer is not null) return orig(self, Utility.AlterRate(maxValue, _closestPlayer.GetModPlayer<SpymPlayer>().lootMult + (_bannerBuff ? 0.1f : 0f)));
        if (Systems.SpymSystem.InUpdateTime && Main.netMode == NetmodeID.SinglePlayer) return orig(self, Utility.AlterRate(maxValue, Main.LocalPlayer.GetModPlayer<SpymPlayer>().eventsMult));
        return orig(self, maxValue);
    }
}

