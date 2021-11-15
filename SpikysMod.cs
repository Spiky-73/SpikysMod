using Terraria.ModLoader;

namespace SPYM
{
	public class SpikysMod : Mod {

		internal static ModKeybind FavoritedBuff;

		public override void Load() {

			FavoritedBuff = KeybindLoader.RegisterKeybind(this, "Favorited Quick buff", "N");

		}
	}
}