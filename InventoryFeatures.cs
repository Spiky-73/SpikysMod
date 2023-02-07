using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace SPYM;

public static class InventoryFeatures {

    public static bool SmartAmmo => Configs.ClientConfig.Instance.smartAmmo;
    public static bool SmartConsumption => Configs.ClientConfig.Instance.smartConsumption;
    public static bool SmartPickupEnabled(Item item) => Configs.ClientConfig.Instance.smartPickup switch {
        Configs.SmartPickupLevel.AllItems => true,
        Configs.SmartPickupLevel.FavoriteOnly => item.favorited,
        Configs.SmartPickupLevel.Off or _ => false
    };
    public static bool ItemSwap => Configs.ClientConfig.Instance.itemSwap;
    public static bool FastRightClick => Configs.ClientConfig.Instance.fastRightClick;
    public static bool ItemRightClick => Configs.ClientConfig.Instance.itemRightClick;
    public static bool FilterRecipes => Configs.ClientConfig.Instance.filterRecipes;
    public static ModKeybind FavoritedBuffKb { get; private set; } = null!;


    public static void Load(){
        FavoritedBuffKb = KeybindLoader.RegisterKeybind(SpikysMod.Instance, "Favorited Quick buff", Microsoft.Xna.Framework.Input.Keys.N);
    }

    public static void SmartConsume(Item consumed, Player player, bool lastStack = false) {
        Item? smartStack = lastStack ? player.LastStack(consumed, true) : player.SmallestStack(consumed, true);
        if (smartStack == null) return;
        consumed.stack++;
        smartStack.stack--;
    }


    public static void OnOpenChest(Player player) => _lastTypeOnChest = new int[player.Chest()!.Length];
    public static void PostUpdate(Player player) => _chest = player.chest;

    public static void OnSlotLeftClick(int slot) => _leftClickedSlot = slot;
    public static void OnItemTranfer(ItemSlot.ItemTransferInfo info) {
        if (info.FromContenxt != 21 || !info.ToContext.InRange(0, 4)) return;
        for (int i = 0; i < _lastTypeOnInv.Length; i++) {
            if (_lastTypeOnInv[i] == info.ItemType) _lastTypeOnInv[i] = 0;
        }
        for (int i = 0; i < _lastTypeOnChest.Length; i++) {
            if (_lastTypeOnInv[i] == info.ItemType) _lastTypeOnInv[i] = 0;
        }
        if (info.ToContext.InRange(0, 2)) _lastTypeOnInv[_leftClickedSlot] = info.ItemType;
        else _lastTypeOnChest[_leftClickedSlot] = info.ItemType;
    }

    public static bool SmartGetItem(int plr, Player player, ref Item newItem, GetItemSettings settings) {
        int i;
        bool gotItems = false;
        if ((i = System.Array.IndexOf(_lastTypeOnInv, newItem.type)) != -1) {
            object[] args = new object[] { plr, newItem, settings, newItem, i };
            if (player.inventory[i].type == ItemID.None) gotItems = (bool)FillEmptyMethod.Invoke(player, args)!;
            else if (player.inventory[i].type == newItem.type && newItem.maxStack > 1) gotItems = (bool)FillOccupiedMethod.Invoke(player, args)!;
            else if (newItem.favorited || !player.inventory[i].favorited) {
                (newItem, player.inventory[i]) = (player.inventory[i], newItem);
            }
        } else if (_chest != -1 && (i = System.Array.IndexOf(_lastTypeOnChest, newItem.type)) != -1) {
            Item[] currentChest = player.Chest(_chest);
            object[] args = new object[] { plr, currentChest, newItem, settings, newItem, i };
            if (player.inventory[i].type == ItemID.None) gotItems = (bool)FillEmptVoidMethod.Invoke(player, args)!;
            else if (player.inventory[i].type == newItem.type && newItem.maxStack > 1) gotItems = (bool)FillOccupiedVoidMethod.Invoke(player, args)!;
            else if (newItem.favorited || !player.inventory[i].favorited) (player.inventory[i], newItem) = (newItem, player.inventory[i]); // dupplicates the item
            if (Main.netMode == NetmodeID.MultiplayerClient && player.chest > -1) NetMessage.SendData(MessageID.SyncChestItem, number: _chest, number2: i);
        }
        return gotItems;
    }


    public static void AttemptItemSwap(Player player, TriggersSet triggersSet) {
        if (!Main.playerInventory || Main.HoverItem.IsAir && Main.mouseItem.IsAir) return;

        int slot = -1;
        if (triggersSet.Hotbar1) slot = 0;
        else if (triggersSet.Hotbar2) slot = 1;
        else if (triggersSet.Hotbar3) slot = 2;
        else if (triggersSet.Hotbar4) slot = 3;
        else if (triggersSet.Hotbar5) slot = 4;
        else if (triggersSet.Hotbar6) slot = 5;
        else if (triggersSet.Hotbar7) slot = 6;
        else if (triggersSet.Hotbar8) slot = 7;
        else if (triggersSet.Hotbar9) slot = 8;
        else if (triggersSet.Hotbar10) slot = 9;
        else _swapped = false;
        if (slot == -1 || _swapped) return;

        _swapped = true;
        SwapHeldItem(player, slot);
    }
    public static void SwapHeldItem(Player player, int destSlot) {
        int sourceSlot = !Main.mouseItem.IsAir ? 58 : System.Array.FindIndex(player.inventory, i => i.type == Main.HoverItem.type && i.stack == Main.HoverItem.stack && i.prefix == Main.HoverItem.prefix);
        (player.inventory[destSlot], player.inventory[sourceSlot]) = (player.inventory[sourceSlot], player.inventory[destSlot]);
        if (sourceSlot == 58) Main.mouseItem = player.inventory[sourceSlot].Clone();
        SoundEngine.PlaySound(SoundID.Grab);
    }


    public static void OnSlotRightClick(int stackSplit) {
        if(Main.stackSplit == stackSplit) return;
        Main.stackSplit = stackSplit;
        ItemSlot.RefreshStackSplitCooldown();
    }
    public static void AttemptFastRightClick(){
        if (Main.mouseRight && Main.stackSplit == 1) Main.mouseRightRelease = true;
    }


    public static bool AttemptItemRightClick(Player player){
        if (!player.controlUseTile || !player.releaseUseItem || player.controlUseItem || player.tileInteractionHappened
                || player.mouseInterface || Terraria.Graphics.Capture.CaptureManager.Instance.Active || Main.HoveringOverAnNPC || Main.SmartInteractShowingGenuine
                || !Main.HoverItem.IsAir || player.altFunctionUse != 0 || player.selectedItem >= 10)
            return false;
        ItemSlot.RightClick(player.inventory, 0, player.selectedItem);
        if (!Main.mouseItem.IsAir) player.DropSelectedItem();
        return true;
    }


    public static void AddCratingMaterials(Dictionary<int, int> materials){
        if(!Main.mouseItem.IsAir) materials[Main.mouseItem.netID] = materials.GetValueOrDefault(Main.mouseItem.netID) + Main.mouseItem.stack;
    }

    public static bool HideRecipe(Recipe recipe){
        if(Main.mouseItem.IsAir) return false;
        int filterType = Main.mouseItem.type;
        if (recipe.createItem.type == filterType || recipe.requiredItem.Exists(i => i.type == filterType) || recipe.acceptedGroups.Exists(g => RecipeGroup.recipeGroups[g].ContainsItem(filterType))) return false;
        return true;
    }


    public static void FavoritedBuff(Player player) => Utility.RunWithHiddenItems(player.inventory, i => !i.favorited, player.QuickBuff);


    private static int _leftClickedSlot;
    private static readonly int[] _lastTypeOnInv = new int[58];
    private static int _chest; // reseted after the player update, later than player.chest and after dropItemCheck is called
    private static int[] _lastTypeOnChest = new int[40];


    private static bool _swapped;

    public static readonly MethodInfo FillEmptyMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillOccupiedMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillEmptVoidMethod = typeof(Player).GetMethod("GetItem_FillEmptyInventorySlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
    public static readonly MethodInfo FillOccupiedVoidMethod = typeof(Player).GetMethod("GetItem_FillIntoOccupiedSlot_VoidBag", BindingFlags.Instance | BindingFlags.NonPublic, new System.Type[] { typeof(int), typeof(Item[]), typeof(Item), typeof(GetItemSettings), typeof(Item), typeof(int) })!;
}