using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYM.Globals;

public class SpymInfoDisplay : GlobalInfoDisplay {

    public static readonly FieldInfo OreFinderTileLocationsField = typeof(SceneMetrics).GetField("_oreFinderTileLocations", BindingFlags.Instance | BindingFlags.NonPublic)!;


    public override void Load() {
        On.Terraria.SceneMetrics.UpdateOreFinderData += HookOreFinderData;
    }


    public override void ModifyDisplayValue(InfoDisplay currentDisplay, ref string displayValue) {
        if(currentDisplay == InfoDisplay.MetalDetector) ModifyDisplay_MetalDetector(ref displayValue);
        else if(currentDisplay == InfoDisplay.Compass) ModifyDisplay_Compass(ref displayValue);
        else if(currentDisplay == InfoDisplay.DepthMeter) ModifyDisplay_DepthMeter(ref displayValue);
    }

    public static void ModifyDisplay_MetalDetector(ref string displayValue) {
        if (!Main.LocalPlayer.GetModPlayer<SpymPlayer>().orePriority || Main.SceneMetrics.bestOre <= 0 || Main.SceneMetrics.ClosestOrePosition == null) return;
        
        float rawDistance = Main.SceneMetrics.ClosestOrePosition.Value.ToWorldCoordinates().Distance(Main.LocalPlayer.position);
        displayValue += Language.GetTextValue($"{Localization.Keys.InfoDisplays}.MetalDetectorRange", System.Math.Max(20, (int)(rawDistance / 8).Snap(20)));
    }

    public static void ModifyDisplay_DepthMeter(ref string displayValue) {
        SpymPlayer spymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        if (!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) return;
        int depth = (int)((double)((spymPlayer.biomeLockPosition.Value.Y + Main.LocalPlayer.height) * 2f / 16f) - Main.worldSurface * 2.0);
        float worldScale = System.MathF.Pow(Main.maxTilesX / 4200,2);
        int worldHeight = 1200;
        float height = (float)((double)(Main.LocalPlayer.Center.Y / 16f - (65f + 10f * worldScale)) / (Main.worldSurface / 5.0));
        string text5 = (spymPlayer.biomeLockPosition.Value.Y > (Main.maxTilesY - 204) * 16) ? Language.GetTextValue("GameUI.LayerUnderworld") :
            ((spymPlayer.biomeLockPosition.Value.Y > Main.rockLayer * 16.0 + worldHeight / 2 + 16.0) ? Language.GetTextValue("GameUI.LayerCaverns") :
            ((depth > 0) ? Language.GetTextValue("GameUI.LayerUnderground") :
            ((height < 1f) ? Language.GetTextValue("GameUI.LayerSpace") :
            Language.GetTextValue("GameUI.LayerSurface"))));

        depth = System.Math.Abs(depth);
        
        string recorded = ((depth != 0) ? Language.GetTextValue("GameUI.Depth", depth) : Language.GetTextValue("GameUI.DepthLevel")) + " " + text5;
        displayValue += Language.GetTextValue(Localization.Keys.InfoDisplays+".RecordedPosition", recorded);
    }
    public static void ModifyDisplay_Compass(ref string displayValue) {
        SpymPlayer spymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        if (!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) return;
        int position = (int)((spymPlayer.biomeLockPosition.Value.X + Main.LocalPlayer.width / 2) * 2f / 16f - Main.maxTilesX);
        string recorded = position switch {
            > 0 => Language.GetTextValue("GameUI.CompassEast", position),
            < 0 => Language.GetTextValue("GameUI.CompassWest", -position),
            0 or _ => Language.GetTextValue("GameUI.CompassCenter")
        };
        displayValue += Language.GetTextValue(Localization.Keys.InfoDisplays+".RecordedPosition", recorded);
    }


    private static void HookOreFinderData(On.Terraria.SceneMetrics.orig_UpdateOreFinderData orig, SceneMetrics self) {
        SpymPlayer spymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        if (!spymPlayer.orePriority || spymPlayer.prioritizedOre == -1) {
            orig(self);
            return;
        }
        short oreRarity = Main.tileOreFinderPriority[spymPlayer.prioritizedOre];
        Main.tileOreFinderPriority[spymPlayer.prioritizedOre] = short.MaxValue;
    
        Point center = Main.LocalPlayer.Center.ToTileCoordinates();
        List<Point> value = (List<Point>)OreFinderTileLocationsField.GetValue(self)!;
        value.Sort((a, b) => (System.Math.Pow(a.X - center.X, 2) + System.Math.Pow(a.Y - center.Y, 2)).CompareTo(System.Math.Pow(b.X - center.X, 2) + System.Math.Pow(b.Y - center.Y, 2)));
        
        orig(self);
        
        Main.tileOreFinderPriority[spymPlayer.prioritizedOre] = oreRarity;
    }
}