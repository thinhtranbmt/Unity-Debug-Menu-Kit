# Changelog

## [2.1.0] - 2026-08-31
### Fixed
- Declare `com.unity.inputsystem` in `package.json`. `DebugMenuKit.Runtime` references the
  `Unity.InputSystem` assembly unconditionally, so a project without the Input System package
  failed to compile on a fresh install.

### Changed
- README no longer claims the toggle gestures use the legacy Input Manager only; the code
  has had an `ENABLE_INPUT_SYSTEM` branch for both backends.

## [2.0.0] - 2026-08-31

### Changed
- **Breaking:** package id renamed `com.thinhtranbmt.debug-menu-kit` -> `com.mycore.debugmenukit`, aligning this kit with the rest of
  the MyCore kit family. Update the entry in `Packages/manifest.json` — Unity treats the new
  id as a different package, so remove the old entry rather than editing around it. No API,
  namespace, or assembly-definition names changed.

## [1.1.0] - 2026-07-10

### Fixed
- Extra-menu panels no longer overlap the second-level menu panel: the ExtraView marker is
  now auto-fitted horizontally (like the two menu panels) so the shown panel — whatever its
  width — sits just left of the visible panels with the standard gap.
- Panel auto-size now accounts for the panels' `localScale` (1.5 in the prefab): widths and
  positions are computed on the scaled size, and the max-width clamp is applied in scaled
  units. Previously wide content made the two panels overlap each other.

### Changed
- Tightened the default layout: panels sit flush against the open button and each other
  (`PANEL_RIGHT_EDGE_X` -156 → -71, `PANEL_GAP` 16 → 1, `PANEL_EXTRA_WIDTH` 8 → 0), and the
  row paddings in the prefab were trimmed to match.

### Added
- Built-in **Status → TestExtraView** button: previews the extra-menu area so testers can
  verify on-device where custom panels land relative to the two menu panels. Shows the
  panel assigned to the new `testExtraViewPrefab` field, or a runtime-built red 350×250
  dummy panel when unassigned.

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
