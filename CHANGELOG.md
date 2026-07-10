# Changelog

## [1.0.0] - 2026-07-10

Initial release — the in-game debug menu extracted from an in-production match-3 game
(DebugMenuManager / DebugRowScript + DebugMenu / DebugRow prefabs) as a reusable UPM
package. Same MonoBehaviour-first design and the same prefab UI; game-specific cheats
removed and known bugs fixed.

### Fixed (relative to the in-game implementation)
- **Debug menu visible in production**: the release branch of `Start()` activated the menu
  and the hide call was commented out. The kit has an explicit `hideOnStart` option instead
  of build-define branches; reveal with a 4-finger tap or F1.
- **Row-list leak**: rows spawned for the second-level menu were added to the first-level
  list, so the list grew forever with destroyed references and category dedup iterated dead
  objects. Menus are now rebuilt cleanly on open.
- `ClearPlayerPrefs` / `ClearGameData` / `ClearCache` caught exceptions and then re-threw
  them; they now log a warning and continue.
- `Log()` guarded against the unassigned `debugLogText` (was an NRE waiting to happen).

### Changed
- Generic and project-independent: the manager no longer hardcodes 40+ game cheats. It scans
  itself and every MonoBehaviour on the prefab instance (and children) for
  `[DebugMethod("Category", "Name")]` methods — add one component with your game's cheats
  and they all appear. `AddButton(category, name, action)` covers runtime registration.
- `DebugMethodAttribute` promoted from a nested class to a top-level type in the
  `DebugMenuKit` namespace.
- Prefab: game-specific pieces removed (server-environment input field, dead inactive input
  field, tile-debug-text and cheat-panel prefab references), Canvas switched from the
  project-specific nested setup to standalone Screen Space – Overlay (sorting order 1000),
  project sorting layer cleared. All remaining references are engine/uGUI/TMP assets that
  exist in every project.
- Built-in buttons kept generic only: HideDebugMenu, ShowHideFPS, ShowDebugConsole (optional
  hook), ClearUserData (PlayerPrefs + cache + persistentDataPath), CopyDeviceInfo.
- Version label auto-fills from `Application.version`; F1 toggle works in the Editor and
  development builds (no custom build defines required).

### Added
- `ShowExtraMenu(GameObject prefab)` public API for custom panels (level selector, amount
  input, ...) in the extra-menu area.
- `RefreshEntries()` to rescan after adding cheat components at runtime.
- `categoryOrder` serialized list to pin favorite categories to the top.
