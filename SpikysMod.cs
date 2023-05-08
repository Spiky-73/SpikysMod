using Terraria.ModLoader;

namespace SPYM;

public class SpikysMod : Mod {

    public static SpikysMod Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;

        InventoryManagement.Actions.Load();
    }

    public override void Unload() {
        Instance = null!;
    }
}
