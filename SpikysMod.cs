using Terraria.ModLoader;

namespace SPYM;

public class SpikysMod : Mod {

	public static SpikysMod Instance => s_instance.TryGetTarget(out SpikysMod? instance) ? instance : null!;
#nullable disable
	internal static ModKeybind FavoritedBuff;
	internal static ModKeybind MetalDetectorTarget;
#nullable restore
	public override void Load() {
        s_instance.SetTarget(this);

        FavoritedBuff = KeybindLoader.RegisterKeybind(this, "Favorited Quick buff", Microsoft.Xna.Framework.Input.Keys.N);
        MetalDetectorTarget = KeybindLoader.RegisterKeybind(this, "Prioritize ore", Microsoft.Xna.Framework.Input.Keys.LeftControl);

	}

    private static readonly System.WeakReference<SpikysMod> s_instance = new(null!);

}
