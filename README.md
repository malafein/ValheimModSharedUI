# ValheimModSharedUI

UI source shared by malafein's Valheim mods that build their own panels. It's added to a mod repo
as a git submodule at `SharedUI/` and compiles straight into that mod's DLL, so players install
nothing extra. It's kept apart from [ValheimModShared](https://github.com/malafein/ValheimModShared)
so mods without UI don't need the TextMeshPro and uGUI references or the `InventoryGui` patch.

- `VanillaUI.cs`: borrows the game's own styling at runtime (Trophy panel frame, Craft button
  look and hover states, title, button and body fonts, the Compendium scrollbar). Lazy, cached,
  and falls back to flat styling if something can't be found.
- `UIBuilder.cs`: `MakeChildRect`, `AddText` (TMP set up without the missing-font warning), and
  `BuildScrollableList` (clipped, auto-sizing, with a vanilla scrollbar).
- `UIPalette.cs`: text colours matched to the Compendium, plus rich-text helpers.
- `ModalPanels.cs`: register a panel's "is open" check and the game treats it like the inventory
  while it's open: cursor freed, look and attack blocked, walking still allowed.

## Rules for this repo

- Plain `.cs` files only. A `.csproj` here would be swept into every mod's build.
- Everything is `internal` and lives in `malafein.Valheim.SharedUI`.
- Don't reference a mod's `Plugin` class. Mods pass in what the library needs.
- Depends on ValheimModShared (`Log`), so a mod using this also needs that submodule at `Shared/`.
- Targets `net48` mods built against the game's own assemblies (Unity 6 Mono), like the core library.

## Using it in a mod

```bash
git submodule add git@github.com:malafein/ValheimModSharedUI.git SharedUI
```

The mod's `.csproj` needs references to the game's `Unity.TextMeshPro.dll` and
`UnityEngine.UI.dll` (the `UnityEngine.*Module.dll` wildcard doesn't cover them).

```csharp
using malafein.Valheim.SharedUI;

// Once, e.g. when the panel is created. Harmony.PatchAll() applies the patch.
ModalPanels.Register(() => MyPanel.IsOpen);
```

Clone a mod with `git clone --recursive`, or run `git submodule update --init` in an existing
clone. To pick up a newer version of this library in a mod:

```bash
git submodule update --remote SharedUI
git commit -am "update shared UI library"
```

Push this repo before pushing a mod that points at a new commit of it.

## License

GPL-3.0, same as the mods.
