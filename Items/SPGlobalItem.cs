
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;

using Terraria.UI;

namespace SPYM.Items {

	public class SPGlobalItem : GlobalItem {

		public override void SetDefaults(Item item) {
			
			switch (item.type) {
			case ItemID.GoldWatch:
			case ItemID.PlatinumWatch:
				item.autoReuse = true;
				item.useTurn = true;
				item.useStyle = ItemUseStyleID.HoldingUp;
				item.useTime = 2;
				item.useAnimation = 2;
				break;
			}
		}

		public override bool UseItem(Item item, Player player) {
			switch (item.type) {
			case ItemID.GoldWatch:
			case ItemID.PlatinumWatch:
				Main.time += 28;  // 30-2
				return true;
			}
			return base.UseItem(item, player);
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			List<string> lines = new List<string>();
			switch (item.type) {
			case ItemID.GoldWatch:
			case ItemID.PlatinumWatch:
				lines.Add(SPTooltip.Watch);
				AddTooltip(tooltips, lines);
				break;
			}
		}

		private void AddTooltip(List<TooltipLine> tooltips, List<string> toAdd) {
			TooltipLine line;
			int startIndex = tooltips.Count;
			int startTooltip = 0;
			for (int i = tooltips.Count - 1; i >= 0; i--) {
				if (tooltips[i].Name.Length < 7) continue;
				string st = tooltips[i].Name.Substring(7);
				if (st != "Tooltip") continue;
				if (!int.TryParse(st, out startTooltip)) continue;
				startTooltip++;
				startIndex = i + 1;
				break;
			}
			for (int i = 0; i < toAdd.Count; i++) {
				line = new TooltipLine(mod, $"Tooltip{startTooltip+ i}", toAdd[i]);
				tooltips.Insert(startIndex+i, line);
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
	}
}

