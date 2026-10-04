using System;
using System.Collections.Generic;
using HarmonyLib;

namespace malafein.Valheim.SharedUI
{
    // Makes the game treat an open mod panel as a visible inventory-class modal.
    // InventoryGui.IsVisible() is the single lever the vanilla input pipeline
    // reads to produce "menu open" behaviour:
    //   • GameCamera.UpdateMouseCapture frees the cursor (so the player can
    //     click the panel) when !InventoryGui.IsVisible() is false.
    //   • PlayerController.LateUpdate zeroes mouse-look via InInventoryEtc().
    //   • PlayerController.FixedUpdate suppresses attack/block/dodge/crouch
    //     via InInventoryEtc(), while WASD movement still flows to SetControls
    //     — matching vanilla, where the inventory leaves you free to walk.
    //   • Player.TakeInput() returns false (it ANDs in !InventoryGui.IsVisible()),
    //     suppressing interact/use/hotbar in Player.Update.
    // One hook gives exact vanilla-inventory parity, so no separate input or
    // cursor patch is needed.
    //
    // Each panel registers once with a function that says whether it's open.
    // The patch class is nested, so the mod's Harmony.PatchAll() picks it up.
    // Every mod using this library compiles its own copy; their postfixes only
    // ever set the result to true, so they combine safely.
    internal static class ModalPanels
    {
        private static readonly List<Func<bool>> s_panels = new List<Func<bool>>();

        public static void Register(Func<bool> isOpen)
        {
            if (isOpen != null && !s_panels.Contains(isOpen)) s_panels.Add(isOpen);
        }

        public static void Unregister(Func<bool> isOpen) => s_panels.Remove(isOpen);

        public static bool AnyOpen
        {
            get
            {
                foreach (var isOpen in s_panels)
                    if (isOpen()) return true;
                return false;
            }
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsVisible))]
            [HarmonyPostfix]
            private static void IsVisiblePostfix(ref bool __result)
            {
                if (!__result && AnyOpen) __result = true;
            }
        }
    }
}
