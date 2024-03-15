using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYM.Globals;

public class SpymInfoDisplay : GlobalInfoDisplay {

    public static readonly FieldInfo OreFinderTileLocationsField = typeof(SceneMetrics).GetField("_oreFinderTileLocations", BindingFlags.Instance | BindingFlags.NonPublic)!;


    public override void Load() {
        On_SceneMetrics.UpdateOreFinderData += HookOreFinderData;
        IL_TileDrawing.DrawSingleTile += IlHighlighOreFinder;
        On_SceneMetrics.ScanAndExportToMain += HookScanAndExportToMain;
    }

    public override void ModifyDisplayParameters(InfoDisplay currentDisplay, ref string displayValue, ref string displayName, ref Color displayColor, ref Color displayShadowColor) {
        if(currentDisplay == InfoDisplay.MetalDetector) ModifyDisplay_MetalDetector(ref displayValue);
        else if(currentDisplay == InfoDisplay.Compass) ModifyDisplay_Compass(ref displayValue);
        else if(currentDisplay == InfoDisplay.DepthMeter) ModifyDisplay_DepthMeter(ref displayValue);
    }

    public const int Increments = 25;
    public static void ModifyDisplay_MetalDetector(ref string displayValue) {
        if (!Main.LocalPlayer.GetModPlayer<SpymPlayer>().orePriority || Main.SceneMetrics.bestOre <= 0 || Main.SceneMetrics.ClosestOrePosition == null) return;
        
        // float rawDistance = Main.SceneMetrics.ClosestOrePosition.Value.ToWorldCoordinates().Distance(Main.LocalPlayer.position);
        // displayValue += " .oO!"[0..(5-Math.Min((int)(rawDistance/(Increments*8)), 5))];
        // Point delta = Main.SceneMetrics.ClosestOrePosition.Value - Main.LocalPlayer.position.ToTileCoordinates();
        // if (delta.X < -2) displayValue += '<';
        // if (delta.Y < -2) displayValue += '^';
        // if (delta.Y > 2) displayValue += 'v';
        // if (delta.X > 2) displayValue += '>';
    }

    public static void ModifyDisplay_DepthMeter(ref string displayValue) {
        SpymPlayer spymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        if (!spymPlayer.biomeLock || !spymPlayer.biomeLockPosition.HasValue) return;
        int depth = (int)((double)((spymPlayer.biomeLockPosition.Value.Y + Main.LocalPlayer.height) * 2f / 16f) - Main.worldSurface * 2.0);
        float worldScale = MathF.Pow(Main.maxTilesX / 4200,2);
        int worldHeight = 1200;
        float height = (float)((double)(Main.LocalPlayer.Center.Y / 16f - (65f + 10f * worldScale)) / (Main.worldSurface / 5.0));
        string text5 = (spymPlayer.biomeLockPosition.Value.Y > (Main.maxTilesY - 204) * 16) ? Language.GetTextValue("GameUI.LayerUnderworld") :
            ((spymPlayer.biomeLockPosition.Value.Y > Main.rockLayer * 16.0 + worldHeight / 2 + 16.0) ? Language.GetTextValue("GameUI.LayerCaverns") :
            ((depth > 0) ? Language.GetTextValue("GameUI.LayerUnderground") :
            ((height < 1f) ? Language.GetTextValue("GameUI.LayerSpace") :
            Language.GetTextValue("GameUI.LayerSurface"))));

        depth = Math.Abs(depth);
        
        string recorded = ((depth != 0) ? Language.GetTextValue("GameUI.Depth", depth) : Language.GetTextValue("GameUI.DepthLevel")) + " " + text5;
        displayValue += Language.GetTextValue(Localization.Keys.InfoDisplays+".RecordedPosition", recorded);
    }
    public static void ModifyDisplay_Compass(ref string displayValue) { // TODO add map icon and tooltip
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


    private void HookScanAndExportToMain(On_SceneMetrics.orig_ScanAndExportToMain orig, SceneMetrics self, SceneMetricsScanSettings settings) {
        SpymPlayer spymPlayer = Main.LocalPlayer.GetModPlayer<SpymPlayer>();
        short oreRarity = -1;
        if (spymPlayer.prioritizedOre != -1) {
            oreRarity = Main.tileOreFinderPriority[spymPlayer.prioritizedOre];
            Main.tileOreFinderPriority[spymPlayer.prioritizedOre] = short.MaxValue;
        }
        orig(self, settings);
        if (oreRarity != -1) Main.tileOreFinderPriority[spymPlayer.prioritizedOre] = oreRarity;

        s_oreHighlights.Clear();
        if (!self.ClosestOrePosition.HasValue) return;
        Queue<Point> toCheck = new();
        toCheck.Enqueue(self.ClosestOrePosition.Value);
        s_oreHighlights.Add(self.ClosestOrePosition.Value);
        while (s_oreHighlights.Count < 1000 && toCheck.TryDequeue(out Point pos)) {
            foreach (Point n in Neigbouhrs.AsSpan()) {
                if (Main.tile[n + pos].TileType == self.bestOre && s_oreHighlights.Add(n + pos)) toCheck.Enqueue(n + pos);
            }
        }
    }

    private static void HookOreFinderData(On_SceneMetrics.orig_UpdateOreFinderData orig, SceneMetrics self) {
        Point center = Main.LocalPlayer.Center.ToTileCoordinates();
        List<Point> value = (List<Point>)OreFinderTileLocationsField.GetValue(self)!;
        value.Sort((a, b) => (Math.Pow(a.X - center.X, 2) + Math.Pow(a.Y - center.Y, 2)).CompareTo(Math.Pow(b.X - center.X, 2) + Math.Pow(b.Y - center.Y, 2)));
        orig(self);
    }

    private void IlHighlighOreFinder(ILContext il) {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld(typeof(Player), nameof(Player.findTreasure)));
        cursor.EmitLdarg0().EmitLdarg(6).EmitLdarg(7);
        cursor.EmitDelegate((bool findTreasure, TileDrawing self, int x, int y) => findTreasure || (s_oreHighlights.Contains(new(x, y)) && Main.LocalPlayer.GetModPlayer<SpymPlayer>().orePriority));
    }

    public static readonly HashSet<Point> s_oreHighlights = new();

    public static readonly Point[] Neigbouhrs = new Point[] {
        new(-1,-1), new(0,-1), new(1,-1),
        new(-1, 0),            new(1, 0),
        new(-1, 1), new(0, 1), new(1, 1)
    };
}