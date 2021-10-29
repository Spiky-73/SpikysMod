using Terraria.ModLoader;

namespace SPYM
{
	public class SPYM : Mod {

		internal static ModHotKey FavoritedBuff;

		public override void Load() {

			FavoritedBuff = RegisterHotKey("Favorited Quick buff", "N");

		}
	}
}