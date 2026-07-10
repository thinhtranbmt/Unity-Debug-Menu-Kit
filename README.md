# Debug Menu Kit

An in-game two-level **debug/cheat menu** for Unity, packaged as a UPM package with
**ready-made prefabs** and zero third-party dependencies (only uGUI + TextMeshPro).
Extracted from an in-production match-3 game — same MonoBehaviour-first design and prefab
UI, made project-independent.

Categories on the left, actions on the right — all driven by one attribute:

```csharp
[DebugMethod("Life", "AddLife")]
public void AddLife() => LifeManager.Instance.AddLife(1);
```

## How it works

- **`DebugMenuManager`** (on the `DebugMenu` prefab) scans itself and every MonoBehaviour on
  the prefab instance and its children for `[DebugMethod("Category", "Name")]` methods.
  Each category becomes a first-level button; each method a second-level button.
- Built-in generic buttons: **HideDebugMenu**, **ShowHideFPS**, **ShowDebugConsole**
  (optional hook), **ClearUserData** (PlayerPrefs + cache + persistentDataPath),
  **CopyDeviceInfo** (to clipboard).
- Toggle the menu button with a **4-finger tap** on device, or **F1** in the Editor /
  development builds.
- FPS counter and app version (auto-filled from `Application.version`) in the corner.

## Installation

**Package Manager (git URL)** — `Window ▸ Package Manager ▸ + ▸ Add package from git URL...`:

```
https://github.com/thinhtranbmt/Unity-Debug-Menu-Kit.git
```

Or in `Packages/manifest.json`:

```json
"com.thinhtranbmt.debug-menu-kit": "https://github.com/thinhtranbmt/Unity-Debug-Menu-Kit.git"
```

Requires Unity 2021.3+, TextMeshPro, and an `EventSystem` in the scene (any uGUI game has
one). Uses the legacy Input Manager for the toggle gestures.

## Quick start

1. Drag `Packages/Debug Menu Kit/Prefabs/DebugMenu.prefab` into your boot scene (root
   level — it calls `DontDestroyOnLoad` on itself). The prefab has its own
   Screen Space – Overlay canvas, sorting order 1000.
2. Write one component with your game's cheats and add it to the prefab instance:

```csharp
using DebugMenuKit;
using UnityEngine;

public class MyGameDebugActions : MonoBehaviour
{
    [DebugMethod("Gameplay", "ForceWin")]
    public void ForceWin() => GameScene.Instance.ForceWin();

    [DebugMethod("Gameplay", "NextLevel")]
    public void NextLevel() => ProgressManager.Instance.IncrementLevel();

    [DebugMethod("Currency", "Add 1000 Coins")]
    public void AddCoins() => CurrencyManager.Instance.Add(1000);
}
```

That's it — three buttons appear under "Gameplay" and "Currency".

## Runtime registration

No component needed:

```csharp
DebugMenuManager.Instance.AddButton("Server", "Switch to Staging", () => Env.Set("staging"));
```

If you add cheat components after startup, call `DebugMenuManager.Instance.RefreshEntries()`.

## Custom panels (extra menu)

For cheats that need input (level selector, amount field), show your own prefab in the
extra-menu area — the previous panel is hidden automatically:

```csharp
[DebugMethod("LevelMap", "Change Level")]
public void ChangeLevel()
{
    GameObject panel = DebugMenuManager.Instance.ShowExtraMenu(levelSelectorPrefab);
    panel.GetComponent<LevelSelector>().Init();
}
```

## Options (on the DebugMenuManager component)

- **`hideOnStart`** — ship with the button hidden; testers reveal it with a 4-finger tap.
  (The original game shipped with the menu visible in production because a build-define
  branch left the hide call commented out — use this flag instead.)
- **`categoryOrder`** — categories to pin to the top of the first-level list, in order;
  the rest follow alphabetically.
- **`debugLogConsole`** — optional: assign any console GameObject (e.g. IngameDebugConsole)
  and the built-in "ShowDebugConsole" button toggles it.
- **`debugLogText`** — optional TMP text; `DebugMenuManager.Instance.Log("...")` appends to
  it when visible.

## What's in the package

```
Runtime/
  DebugMenuManager.cs      // menu logic, attribute scan, built-in cheats
  DebugMethodAttribute.cs  // [DebugMethod("Category", "Name")]
  DebugRowScript.cs        // one button row
Prefabs/
  DebugMenu.prefab         // full menu UI (own overlay canvas), wired and ready
  DebugRow.prefab          // row template used by both levels
```

## License

MIT — see `LICENSE`.
