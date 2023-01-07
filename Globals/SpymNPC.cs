using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SPYM.Globals;
class SpymNPC : GlobalNPC {

    public static bool InDropItem { get; private set; }
    public static bool BannerBuff { get; private set; }
    public static Player? ClosestPlayer { get; private set; }

    public override void Load() {
        On.Terraria.NPC.NPCLoot_DropItems += HookDropItem;
        On.Terraria.Utilities.UnifiedRandom.Next_int += HookRngNext_int;
        IL.Terraria.NPC.SpawnNPC += ILSpawnNPC;
        On.Terraria.NPC.NewNPC += HookNewNPC;
    }

    private static int HookNewNPC(On.Terraria.NPC.orig_NewNPC orig, IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target) {
        int s = orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
        if(InSpawnNPC) _ilNewNPC = s;
        return s;
    }

    public static bool InSpawnNPC { get; private set; }

    private static int _ilCurrent = 0;
    private static int _ilNewNPC = 0;
    private static NPC? _ilRarestSpawn = null;
    private static int _ilRerolls = 1;

    private void ILSpawnNPC(ILContext il) {
        ILCursor cursor = new(il);

        byte newNPCIndex = 30;
        byte spawnInfoIndex = 25;

        ILLabel startLoop = il.DefineLabel();
        ILLabel endLoop = il.DefineLabel();
        
        MethodBase chooseSpawnMethod = typeof(NPCLoader).GetMethod(nameof(NPCLoader.ChooseSpawn), BindingFlags.Public | BindingFlags.Static, new System.Type[] {typeof(NPCSpawnInfo)})!;
        FieldInfo netModeField = typeof(Main).GetField(nameof(Main.netMode), BindingFlags.Static | BindingFlags.Public)!;

        if (!cursor.TryGotoNext(i => i.MatchCall(chooseSpawnMethod)) || !cursor.TryGotoNext(i => i.MatchLdsfld(netModeField))) {
            SpikysMod.Instance.Logger.Error($"{nameof(NPC)}.{nameof(NPC.SpawnNPC)} il hook could not be applied");
            return;
        }

        // Loop start detection
        cursor.Index = 0;
        cursor.GotoNext(i => i.MatchCall(chooseSpawnMethod));
        cursor.GotoPrev();

        // Loop init
        cursor.Emit(OpCodes.Ldloc_S, spawnInfoIndex);
        cursor.EmitDelegate<System.Action<NPCSpawnInfo>>(spawnInfo => {
            InSpawnNPC = true;
            _ilNewNPC = 200;
            _ilRerolls = 1 + spawnInfo.Player.GetModPlayer<SpymPlayer>().npcExtraRerolls;
            _ilCurrent = 0;
            _ilRarestSpawn = null;
        });
        cursor.Emit(OpCodes.Br, endLoop);

        // Loop start
        cursor.MarkLabel(startLoop);
        cursor.Emit(OpCodes.Ldc_I4, 200);
        cursor.Emit(OpCodes.Stloc_S, newNPCIndex);
        cursor.EmitDelegate<System.Action>(() => _ilNewNPC = 200);

        // loop end detection
        cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode), BindingFlags.Static | BindingFlags.Public)!));
        cursor.GotoNext();

        // Loop pre-end
        cursor.EmitDelegate((int netMode) => {
            if(_ilRarestSpawn is null || Main.npc[_ilNewNPC].rarity > _ilRarestSpawn.rarity) _ilRarestSpawn = Main.npc[_ilNewNPC];
            Main.npc[_ilNewNPC] = new();
            _ilCurrent++;
        });

        // Loop end
        cursor.MarkLabel(endLoop);
        cursor.EmitDelegate(() => _ilCurrent < _ilRerolls);
        cursor.Emit(OpCodes.Brtrue, startLoop);

        // Post loop
        cursor.EmitDelegate(() => {
            InSpawnNPC = false;
            Main.npc[_ilNewNPC] = _ilRarestSpawn;
        });

        cursor.Emit(OpCodes.Ldsfld, netModeField);
    }


    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
        float mult = player.GetModPlayer<SpymPlayer>().spawnRateBoost;
        spawnRate = (int)(spawnRate/mult);
        maxSpawns = (int)(spawnRate*mult);
    }

    private static void HookDropItem(On.Terraria.NPC.orig_NPCLoot_DropItems orig, NPC self, Player closestPlayer) {
        InDropItem = true;
        ClosestPlayer = closestPlayer;
        int banner = Item.NPCtoBanner(self.BannerID());
        BannerBuff = closestPlayer.HasNPCBannerBuff(banner);
        orig(self, closestPlayer);
        InDropItem = false;
        ClosestPlayer = null;
    }

    private static int HookRngNext_int(On.Terraria.Utilities.UnifiedRandom.orig_Next_int orig, Terraria.Utilities.UnifiedRandom self, int maxValue) {
        if (!InDropItem || ClosestPlayer is null) return orig(self, maxValue);
        return orig(self, AlterDropRate(maxValue));
    }

    private static int AlterDropRate(int chanceDenominator) {
        float tallyMult = ClosestPlayer!.GetModPlayer<SpymPlayer>().tallyMult;
        if(BannerBuff) tallyMult += 0.1f;

        if (tallyMult <= 1f) return chanceDenominator;
        chanceDenominator = (int)System.MathF.Ceiling(System.MathF.Pow(2, System.MathF.Pow(System.MathF.Log2(chanceDenominator), 1/tallyMult)));
        return chanceDenominator;
    }
}

