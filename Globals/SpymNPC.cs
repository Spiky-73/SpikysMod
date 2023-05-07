using System.Reflection;
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
        On.Terraria.NPC.NewNPC += HookNewNPC;
        IL.Terraria.NPC.SpawnNPC += ILSpawnNPC;

        IL.Terraria.GameContent.ItemDropRules.ItemDropResolver.ResolveRule += ILResolveRule;
        
        On.Terraria.NPC.NPCLoot_DropItems += HookDropItem;
    }

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
        float mult = player.GetModPlayer<SpymPlayer>().spawnRateMult;
        if (Utility.BossAlive() && Configs.VanillaImprovements.Instance.betterCalming && player.calmed || player.HasBuff(BuffID.PeaceCandle)) mult = 0;

        if (mult == 0) maxSpawns = 0;
        else {
            spawnRate = (int)(spawnRate / mult);
            maxSpawns = (int)(spawnRate * mult);
        }
    }

    private static int _lastNpcSpawned;
    private static int _ilAttempts, _ilCurrentTry;
    private static NPC? _ilRarestSpawn;
    private static int _ilRarestSpawnIndex;
    private static int HookNewNPC(On.Terraria.NPC.orig_NewNPC orig, IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target) {
        return _lastNpcSpawned = orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
    }
    private void ILSpawnNPC(ILContext il) {
        ILCursor cursor = new(il);

        const byte LocNewNPC = 30;
        const byte LocSpawnInfo = 25;

        ILLabel startLoop = il.DefineLabel();
        ILLabel endLoop = il.DefineLabel();

        MethodBase chooseSpawnMethod = typeof(NPCLoader).GetMethod(nameof(NPCLoader.ChooseSpawn), BindingFlags.Public | BindingFlags.Static, new System.Type[] { typeof(NPCSpawnInfo) })!;
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
            _ilAttempts = 1 + spawnInfo.Player.GetModPlayer<SpymPlayer>().npcExtraRerolls;
            _ilCurrentTry = 0;
            _ilRarestSpawn = null;
            _ilRarestSpawnIndex = -1;
        });
        cursor.Emit(OpCodes.Br, endLoop);

        // Loop start
        cursor.MarkLabel(startLoop);
        cursor.Emit(OpCodes.Ldc_I4, 200);
        cursor.Emit(OpCodes.Stloc_S, LocNewNPC);
        cursor.EmitDelegate<System.Action>(() => _lastNpcSpawned = 200);

        // Loop end detection
        cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode), BindingFlags.Static | BindingFlags.Public)!));
        cursor.GotoNext();

        // Loop pre-end
        cursor.EmitDelegate((int netMode) => {
            if (_ilRarestSpawn is null || Main.npc[_lastNpcSpawned].rarity > _ilRarestSpawn.rarity) {
                _ilRarestSpawnIndex = _lastNpcSpawned;
                _ilRarestSpawn = Main.npc[_ilRarestSpawnIndex];
            }
            Main.npc[_lastNpcSpawned] = new();
            _ilCurrentTry++;
        });

        // Loop end
        cursor.MarkLabel(endLoop);
        cursor.EmitDelegate(() => _ilCurrentTry < _ilAttempts);
        cursor.Emit(OpCodes.Brtrue, startLoop);

        // Post loop
        cursor.EmitDelegate(() => {
            Main.npc[_ilRarestSpawnIndex] = _ilRarestSpawn;
        });

        cursor.Emit(OpCodes.Ldsfld, netModeField);
    }

    private static Vector2? _ilNpcPosition;
    private static void ILResolveRule(ILContext il) {

        ILCursor cursor = new(il);
        MethodInfo candropMethod = typeof(IItemDropRule).GetMethod(nameof(IItemDropRule.CanDrop), BindingFlags.Public | BindingFlags.Instance)!;
        if(!cursor.TryGotoNext(i => i.MatchCallvirt(candropMethod))){
            SpikysMod.Instance.Logger.Error($"\"{nameof(ItemDropResolver)}.ResolveRule\" il hook could not be applied");
            return;
        }
        cursor.GotoPrev().GotoPrev();
        cursor.Emit(OpCodes.Ldarg_2);
        cursor.EmitDelegate((DropAttemptInfo info) => {
            _ilNpcPosition = null;
            SpymPlayer spymPlayer = info.player.GetModPlayer<SpymPlayer>();
            if (!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) return;
            _ilNpcPosition = info.npc.Center;
            info.npc.Center = spymPlayer.biomeLockPosition.Value;
        });
        cursor.GotoNext(MoveType.After, i => i.MatchCallvirt(candropMethod));
        cursor.Emit(OpCodes.Ldarg_2);
        cursor.EmitDelegate((bool res, DropAttemptInfo info) => {
            if (_ilNpcPosition.HasValue) info.npc.Center = _ilNpcPosition.Value;
            return res;
        });
    }


    private static void HookDropItem(On.Terraria.NPC.orig_NPCLoot_DropItems orig, NPC self, Player closestPlayer) {
        bool bannerBuff = Configs.VanillaImprovements.Instance.bannerBuff && closestPlayer.HasNPCBannerBuff(Item.NPCtoBanner(self.BannerID()));
        SpymSystem.AlteredRngRates = closestPlayer.GetModPlayer<SpymPlayer>().lootBoost + (bannerBuff ? 0.1f : 0f);
        orig(self, closestPlayer);
        SpymSystem.AlteredRngRates = null;
    }
}

