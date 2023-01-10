using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SPYM.Globals;

public class SpymInfoDisplay : GlobalInfoDisplay {

    public static readonly FieldInfo OreFinderTileLocationsField = typeof(SceneMetrics).GetField("_oreFinderTileLocations", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public override void Load() {
        On.Terraria.SceneMetrics.UpdateOreFinderData += HookOreFinderData;
    }

    public override void ModifyDisplayValue(InfoDisplay currentDisplay, ref string displayValue) {
        if(currentDisplay != InfoDisplay.MetalDetector) return;
        if(!Main.LocalPlayer.GetModPlayer<SpymPlayer>().metalDetector) return;
        if (Main.SceneMetrics.bestOre <= 0) return;
        if (Main.SceneMetrics.ClosestOrePosition == null) return;
        Vector2 value = new(Main.SceneMetrics.ClosestOrePosition.Value.X, Main.SceneMetrics.ClosestOrePosition.Value.Y);
        float rawDistance = value.Distance(Main.LocalPlayer.position/16f)*2f;
        displayValue += $" ({Math.Max(20, (int)rawDistance.Snap(20))} ft)";
    }

    private void HookOreFinderData(On.Terraria.SceneMetrics.orig_UpdateOreFinderData orig, SceneMetrics self) {
        if(!Main.LocalPlayer.GetModPlayer<SpymPlayer>().metalDetector) {
            orig(self);
            return;
        }
        Point center = (Main.LocalPlayer.Center/16).ToPoint();
        List<Point> value = (List<Point>)OreFinderTileLocationsField.GetValue(self)!;
        value.Sort((a, b) => (Math.Pow(a.X - center.X, 2) + Math.Pow(a.Y - center.Y, 2)).CompareTo(Math.Pow(b.X - center.X, 2) + Math.Pow(b.Y - center.Y, 2)));
        orig(self);
    }
}