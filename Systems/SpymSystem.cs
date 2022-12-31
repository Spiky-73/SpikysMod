using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYM.Systems;

public class SpymSystem : ModSystem {
    public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate) {
        // // TODO multiplayer
        float mult = Main.LocalPlayer.GetModPlayer<Globals.SpymPlayer>().timeWarp;
        timeRate *= mult;
        tileUpdateRate *= mult;
        eventUpdateRate *= mult;
    }

    public override void PostAddRecipes(){
        foreach(Recipe recipe in Main.recipe){
            if(recipe.createItem.type != ItemID.CellPhone || recipe.requiredItem.Find(i => i.type == ItemID.PDA) == null) continue;
            recipe.requiredItem.Add(new(ItemID.PotionOfReturn, 15));
            recipe.requiredItem.Add(new(ItemID.WormholePotion, 15));
        }
    }
}