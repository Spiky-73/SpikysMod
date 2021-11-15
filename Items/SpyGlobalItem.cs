
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;

using Terraria.UI;

namespace SPYM.Items {

	public class SPGlobalItem : GlobalItem {

		public static bool lastTickday;
		public override void SetDefaults(Item item) {
			
			switch (item.type) {
			case ItemID.GoldWatch:
			case ItemID.PlatinumWatch:
				item.autoReuse = true;
				item.useTurn = true;
				item.useStyle = ItemUseStyleID.HoldUp;
				item.useTime = 2;
				item.useAnimation = 2;
				break;
			case ItemID.WeatherRadio:
				item.useStyle = ItemUseStyleID.HoldUp;
				item.useTime = 45;
				item.useAnimation = 45;
				break;
			case ItemID.Sextant:
				item.useStyle = ItemUseStyleID.HoldUp;
				item.useTime = 45;
				item.useAnimation = 45;
				break;
			}
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			List<string> lines = new List<string>();
			switch (item.type) {
			case ItemID.GoldWatch:
			case ItemID.PlatinumWatch:
				lines.Add(SPTooltip.Watch);
				break;
			case ItemID.Radar:
				lines.Add(SPTooltip.Radar);
				break;
			case ItemID.TallyCounter:
				lines.Add(SPTooltip.TallyCounter);
				break;
			case ItemID.LifeformAnalyzer:
				lines.Add(SPTooltip.LifeformAnalyzer);
				break;
			case ItemID.Stopwatch:
				lines.Add(SPTooltip.Stopwatch);
				break;
			case ItemID.MetalDetector:
				lines.Add(SPTooltip.MetalDetector);
				break;
			case ItemID.WeatherRadio:
				lines.Add(SPTooltip.WeatherRadio);
				break;
			case ItemID.Sextant:
				lines.Add(SPTooltip.Sextant);
				break;
			case ItemID.CellPhone:
				lines.Add(SPTooltip.CellPhone);
				break;
			}
			AddTooltip(tooltips, lines);
		}
		private void AddTooltip(List<TooltipLine> tooltips, List<string> toAdd) {
			TooltipLine line;
			int startIndex = tooltips.Count;
			int startTooltip = 0;
			for (int i = tooltips.Count - 1; i >= 0; i--) {
				if (tooltips[i].Name.Length < 7) continue;
				string st = tooltips[i].Name[7..];
				if (st != "Tooltip") continue;
				if (!int.TryParse(st, out startTooltip)) continue;
				startTooltip++;
				startIndex = i + 1;
				break;
			}
			for (int i = 0; i < toAdd.Count; i++) {
				line = new TooltipLine(Mod, $"Tooltip{startTooltip + i}", toAdd[i]);
				tooltips.Insert(startIndex + i, line);
			}

		}

		public override bool? UseItem(Item item, Player player) {
			switch (item.type) {
			case ItemID.GoldWatch:
			case ItemID.PlatinumWatch:
				Main.time += 38;  // +19/tick (20 speed)
				Mod.Logger.Debug("3");
				return true;
			case ItemID.WeatherRadio:
				ChangeRain();
				return true;
			case ItemID.Sextant:
				Main.moonType = (Main.moonType + 1) % 3;
				return true;
			}
			return null;
		}

		public override void UpdateEquip(Item item, Player player) {
			SpyPlayer spyPlayer = player.GetModPlayer<SpyPlayer>();
			switch (item.type) {
			case ItemID.Radar:
				spyPlayer.radarLevel = 1;
				break;
			case ItemID.TallyCounter:
				spyPlayer.tallyLevel = 1;
				break;
			case ItemID.LifeformAnalyzer:
				spyPlayer.analyserLevel = 1;
				break;
			case ItemID.Stopwatch:
				player.maxRunSpeed *= 3;
				player.maxFallSpeed *= 1.5f;
				player.runAcceleration *= 1.5f;
				break;
			case ItemID.MetalDetector:
				break;
			case ItemID.WeatherRadio:
				break;
			case ItemID.Sextant:
				if (!lastTickday && Main.dayTime)
					Main.moonPhase = (Main.moonPhase - 1) % 7;
				lastTickday = Main.dayTime;
				break;
			}
		}

		public override void OnConsumeItem(Item item, Player player) {
			ClientConfig config = ModContent.GetInstance<ClientConfig>();
			if(config.smartConsume){
				Item smallestStack = Utility.SmallestStack(item, player, true);
				if(smallestStack == null) return;
				item.stack++;
				smallestStack.stack--;
			}
		}
		
		public override void OnConsumeAmmo(Item ammo, Player player) {
			ClientConfig config = ModContent.GetInstance<ClientConfig>();
			if(config.smartAmmo && ammo.consumable){
				Item smallestAmmo = Utility.SmallestStack(ammo, player, true);
				if(smallestAmmo == null) return;
				ammo.stack++;
				smallestAmmo.stack--;
			}
		}

		public bool IsEquipable(Item item){
			return item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0 || item.accessory || Main.projHook[item.shoot] || item.mountType != -1 || (item.buffType > 0 && (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType]));
		}

		// Needs to be change in 1.4 as new fonctions were created
		// Only stops the rain
		private static void ChangeRain() {
			if (Main.raining) {
				Main.rainTime = 0;
				Main.raining = false;
				Main.maxRaining = 0f;
			}
		}
	}
}

