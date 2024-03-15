using Terraria.ModLoader;

namespace SPYM;

// BUG crash when unloading

public class SpikysMod : Mod {

    public static SpikysMod Instance { get; private set; } = null!;

    public override void Load() {
        Instance = this;
    }

    public override void Unload() {
        Instance = null!;
    }
}
