using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine.EventSystems;

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
    // One hook gives exact vanilla-inventory parity for the cursor and input.
    //
    // Vanilla doesn't know about a mod panel's text fields, so while one has focus
    // PlayerController.TakeInput() is forced false as well, as it is for vanilla's
    // own text entry; otherwise typing W would walk the player.
    //
    // A panel that closes on Esc should keep reporting itself open for the rest
    // of that frame, or a vanilla Update running later in the same frame sees no
    // panel and opens the game menu on the same key press.
    //
    // Each panel registers once with a function that says whether it's open.
    // The patch class is nested, so the mod's Harmony.PatchAll() picks it up.
    // Every mod using this library compiles its own copy; each patch only ever
    // pushes its result toward "a panel is open", so the copies combine safely.
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

        // True while a text field inside any UI has keyboard focus.
        public static bool TextFieldFocused
        {
            get
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selected == null) return false;
                var input = selected.GetComponent<TMP_InputField>();
                return input != null && input.isFocused;
            }
        }

        // PatchAll() only looks at classes that carry [HarmonyPatch] themselves.
        [HarmonyPatch]
        private static class Patches
        {
            [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsVisible))]
            [HarmonyPostfix]
            private static void IsVisiblePostfix(ref bool __result)
            {
                if (!__result && AnyOpen) __result = true;
            }

            // The inventory opens from its own animator state, not IsVisible(), so its key
            // would still open it underneath a mod panel. Consume the key first, the same
            // way vanilla consumes it (ZInput.ResetButtonStatus).
            [HarmonyPatch(typeof(InventoryGui), "Update")]
            [HarmonyPrefix]
            private static void UpdatePrefix()
            {
                if (!AnyOpen) return;
                ZInput.ResetButtonStatus("Inventory");
                ZInput.ResetButtonStatus("JoyButtonY");
            }

            [HarmonyPatch(typeof(PlayerController), "TakeInput")]
            [HarmonyPostfix]
            private static void TakeInputPostfix(ref bool __result)
            {
                if (__result && AnyOpen && TextFieldFocused) __result = false;
            }
        }
    }
}
