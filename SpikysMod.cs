using Terraria.ModLoader;

namespace SPYM;

public class SpikysMod : Mod {

    public static SpikysMod Instance { get; private set; } = null!;
    public static ModKeybind PrioritizeOre = null!;

    public override void Load() {
        Instance = this;

        InventoryManagement.Actions.Load();
        PrioritizeOre = KeybindLoader.RegisterKeybind(this, "Prioritize ore", Microsoft.Xna.Framework.Input.Keys.LeftControl);
    }

    public override void Unload() {
        Instance = null!;
    }
}
