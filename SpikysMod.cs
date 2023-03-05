using Terraria.ModLoader;

namespace SPYM;

public class SpikysMod : Mod {

    public static SpikysMod Instance => s_instance.TryGetTarget(out SpikysMod? instance) ? instance : null!;
	public static ModKeybind PrioritizeOre = null!;

    public override void Load() {
        s_instance.SetTarget(this);

        InventoryFeatures.InventoryManagement.Load();
        PrioritizeOre = KeybindLoader.RegisterKeybind(this, "Prioritize ore", Microsoft.Xna.Framework.Input.Keys.LeftControl);
    }

    private static readonly System.WeakReference<SpikysMod> s_instance = new(null!);

}
