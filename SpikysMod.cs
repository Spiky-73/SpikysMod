using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;

namespace SPYM;

public class SpikysMod : Mod {

	public static SpikysMod Instance => s_instance.TryGetTarget(out SpikysMod? instance) ? instance : null!;
#nullable disable
	public static ModKeybind FavoritedBuff;
	public static ModKeybind MetalDetectorTarget;
    public static List<(ModKeybind, BuilderAccTogglesUI.GetIsAvailablemethod, BuilderAccTogglesUI.PerformClickMethod)> BuilderAccToggles;
#nullable restore

	private static void CycleAccState(Player player, int index, int cycle = 2) => player.builderAccStatus[index] = (player.builderAccStatus[index]+1) % cycle;
    public override void Load() {
        s_instance.SetTarget(this);

        FavoritedBuff = KeybindLoader.RegisterKeybind(this, "Favorited Quick buff", Microsoft.Xna.Framework.Input.Keys.N);
        MetalDetectorTarget = KeybindLoader.RegisterKeybind(this, "Prioritize ore", Microsoft.Xna.Framework.Input.Keys.LeftControl);



        BuilderAccToggles = new() {
			(
				KeybindLoader.RegisterKeybind(this, "Ruler Line", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.rulerLine,
				(Player player) => CycleAccState(player, 0)
			), (
				KeybindLoader.RegisterKeybind(this, "Ruler Grid", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.rulerLine,
				(Player player) => CycleAccState(player, 1)
			), (
				KeybindLoader.RegisterKeybind(this, "Auto Paint", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.rulerLine,
				(Player player) => CycleAccState(player, 2)
			), (
				KeybindLoader.RegisterKeybind(this, "Auto Actuator", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.rulerLine,
				(Player player) => CycleAccState(player, 3)
			), (
				KeybindLoader.RegisterKeybind(this, "Wire display", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.InfoAccMechShowWires,
				(Player player) =>
					{
						CycleAccState(player, 4, 3);
						for (int i = 5; i < 8; i++) player.builderAccStatus[i] = player.builderAccStatus[4];
						player.builderAccStatus[9] = player.builderAccStatus[4];
					}
			), (
				KeybindLoader.RegisterKeybind(this, "Forced Wires", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.InfoAccMechShowWires,
				(Player player) => CycleAccState(player, 8)
			), (
				KeybindLoader.RegisterKeybind(this, "Block Swap", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => true,
				(Player player) => CycleAccState(player, 10)
			), (
				KeybindLoader.RegisterKeybind(this, "Biome Torches", Microsoft.Xna.Framework.Input.Keys.None),
				(Player player) => player.unlockedBiomeTorches,
				(Player player) => CycleAccState(player, 11)
			)
        };

    }

    private static readonly System.WeakReference<SpikysMod> s_instance = new(null!);

}
