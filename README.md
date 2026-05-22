# HaloShift - Xbox Controller to Mouse/Keyboard Bridge

A background Windows app that maps Xbox controller input to mouse and keyboard actions. HaloShift uses **Avalonia** for UI (tray icon, virtual keyboard, controls reference) and **SharpDX.XInput** for controller polling.

## Features

### Dual mode
- **Controller mode**: Input passes through to games and Steam; HaloShift stays in the tray.
- **Mouse mode**: Controller drives the cursor, clicks, scroll, and keyboard shortcuts.

### Mode switching
- Hold **View** for **1.5 seconds** to toggle Controller ↔ Mouse mode.
- Tray menu **Toggle Mode** does the same manually.

### Mouse mode (keyboard closed)

| Input | Action |
|--------|--------|
| **Left stick** | Move cursor (quadratic acceleration, 15% deadzone) |
| **RT (hold)** | Left mouse button |
| **LT (hold)** | Right mouse button |
| **D-Pad Up / Down** | Increase / decrease mouse sensitivity |
| **Y** (alone) | Open virtual keyboard |
| **X** | Escape |
| **B** | Browser Back (closes About window when it is open) |
| **LB** (alone) | F11 fullscreen |
| **LB + View** | Show controls (About) window |
| **LS click** | Windows key |
| **RS click** | F5 |
| **LT + RT + X** | Middle click |
| **LT + RT + A** | Ctrl+W |
| **LT + RT + B** | Alt+F4 |

While the virtual keyboard is open, **LT/RT and sticks still move the mouse**; D-Pad navigation is handled by the keyboard instead of sensitivity.

### Virtual keyboard (mouse mode)

Open with **Y** (no LB/RB). Stays topmost over fullscreen apps.

| Input | Action |
|--------|--------|
| **D-Pad** | Move selection (wraps on rows/columns; TAB row ↔ function row rules apply) |
| **A** | Press selected key |
| **X** | Backspace |
| **B** | Close keyboard |
| **LB** | Toggle symbol layer (SYM ↔ letters) |
| **Hold RB** (LB not held) | Arrow keys (Up/Left/Down/Right) with brief key highlight |
| **LB + RB** (together) | Toggle nav-cluster mode: PRTSC, SCRLK, PAUSE, INS, HOME, PGUP, DEL, END, PGDN |
| **LB + RB** again | Return to main typing keys |

Modifiers (Shift, Caps, Ctrl, Alt, Win) toggle on selection and show active state on keys.

### System tray
- **Show Controls** — mapping reference window (also **LB + View** in mouse mode)
- **Toggle Mode**
- **Show Keyboard**
- **Exit**

## Requirements

- Windows 10 1809+ (for .NET 8)
- Xbox 360 / Xbox One / compatible XInput controller
- .NET 8 SDK to build; published build is self-contained (`win-x64`)

## Build and run

```bash
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

Publish single-file (optional):

```bash
dotnet publish -c Release
```

Output: `bin/Release/net8.0-windows/win-x64/publish/HaloShift.exe`

Sound files (`sound_*.wav`) and `assets/*_button.png` copy to the output directory automatically.

## Configuration

Persisted settings: mouse sensitivity (`AppSettings.json` via `AppSettings.cs`).

Tunable constants in source:

| Setting | Location | Default |
|---------|----------|---------|
| Stick deadzone | `ModeManager.HandleLeftStickMovement` | `0.15f` |
| Trigger threshold | `ModeManager` | `0.5f` |
| Mode toggle | `ModeManager.Update` | Hold View 1.5s (one toggle per hold) |
| Poll interval | `App.axaml.cs` | `8` ms (~125 Hz) |
| Sensitivity range | `ModeManager` | `0.5` – `3.0` |

## Architecture

```
Program.cs          → Avalonia host, single-instance mutex
App.axaml.cs        → Poll loop, wires tray / modes / overlays
ControllerManager.cs → XInput polling
ModeManager.cs      → Mode toggle, mouse-mode mappings, keyboard/controls events
InputSimulator.cs   → SendInput mouse/keyboard
UI/
  MainWindow        → Hidden host window
  TrayIconManager   → Native tray menu
  ControlsWindow    → Controller mapping reference (ABXY assets)
  VirtualKeyboardWindow → On-screen keyboard + navigation zones
  SensitivityOverlayWindow → Brief sensitivity feedback
Win32Sound.cs       → Mode-change sounds
```

## Troubleshooting

**Controller not detected** — Check Windows controller settings and USB/wireless connection; restart HaloShift.

**Mode will not switch** — Hold **View** for at least **1.5 seconds** (release and hold again if needed).

**Controls window opens unexpectedly** — Use **LB + View**, not View alone (a long View hold toggles mode).

**Virtual keyboard: nav cluster** — Press **LB + RB at the same time**; D-Pad moves the 3×3 cluster; **A** types the key.

**Jerky mouse** — Raise sensitivity with D-Pad Up or adjust `BASE_SENSITIVITY` in `ModeManager.cs`.

## Dependencies

- Avalonia 11 (desktop UI)
- SharpDX.XInput 4.2.0

## License

Personal use on Windows with Xbox controllers.

---

## Appendix: Changes from earlier documentation

The repo previously shipped several overlapping `.md` files (`START_HERE.md`, `INDEX.md`, `PROJECT_SUMMARY.md`, `QUICK_REFERENCE.md`, `SETUP.md`, `DELIVERABLES.md`, `IMPLEMENTATION_COMPLETE.md`, `VIRTUAL_KEYBOARD_FEATURE.md`). Those described an older **Windows Forms** stack and outdated controls. They have been **removed**; this README is the single reference.

| Topic | Old docs | Current behavior |
|--------|----------|------------------|
| UI framework | Windows Forms (`MainForm.cs`) | **Avalonia 11** (`App.axaml.cs`, `UI/*`) |
| .NET version | .NET 6 | **.NET 8** (`net8.0-windows`) |
| Mode toggle | Share button, or LB+RB+Y | **Hold View** 1.5s |
| Open controls / About | START alone (brief regression) | **LB + View**; **B** closes About |
| Virtual keyboard code | `VirtualKeyboard.cs` | `UI/VirtualKeyboardWindow.axaml(.cs)` |
| Keyboard navigation | Simple row wrap only | Wrap + function row access; **nav cluster** (LB+RB); **hold RB** for arrows |
| D-Pad in mouse mode | Up/Down = sensitivity only | Same when keyboard closed; D-Pad navigates keyboard when open |
| Help UI | `ControlsWindow.cs` text only | `ControlsWindow.axaml` with sections + **ABXY** images from `assets/` |
| Architecture entry | `Program` + `MainForm` timer | `App` **DispatcherTimer** poll + tray-driven lifetime |
| Docs maintenance | 9 markdown files | **README.md** only |

If you need historical detail, use git history on the deleted files.
