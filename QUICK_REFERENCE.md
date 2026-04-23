# HaloShift - Quick Reference

## Installation & Running

### Quick Build & Run
```bash
cd HaloShift
dotnet build -c Release
dotnet run
```

## Control Mapping

### Mode Toggle
**Share** (single press) → Switch between modes

### Mouse Mode Controls
| Control | Action |
|---------|--------|
| **Left Stick** | Move mouse cursor (smooth acceleration) |
| **RT Trigger** | Left click (>50% pressure) |
| **LT Trigger** | Right click (>50% pressure) |
| **LB Button** | Send F11 (full-screen toggle) |
| **RB Button** | (reserved for mode toggle combo) |
| **Y Button** | (reserved for mode toggle combo) |

### Controller Mode
- App minimized to system tray
- All input released to active application (Steam, games, etc.)
- App continues running in background

## System Tray Menu

Right-click or left-click the HaloShift icon in system tray:
- **Show** - Bring window to foreground
- **Toggle Mode** - Manually switch modes
- **Exit** - Close the application

## Features at a Glance

✅ **High Performance**
- 60 FPS update loop
- <20ms input latency
- Minimal CPU/memory footprint

✅ **Smart Input Processing**
- 15% deadzone on analog sticks
- Quadratic acceleration curve
- Configurable sensitivity (default 15 px/frame)

✅ **Seamless Integration**
- System tray operation
- Doesn't interfere with Steam
- Windows Forms UI
- Graceful window management

✅ **Professional Input Simulation**
- Direct SendInput API usage
- Supports mouse and keyboard
- Compatible with all Windows applications

## Configuration Quick Edit

### Edit Mouse Sensitivity
**File**: `ModeManager.cs` → Method: `HandleLeftStickMovement()`
```csharp
const float SENSITIVITY = 15f; // Increase for faster, decrease for slower
```

### Edit Stick Deadzone
**File**: `ModeManager.cs` → Method: `HandleLeftStickMovement()`
```csharp
const float DEADZONE = 0.15f; // 0.1 = more sensitive, 0.2 = less sensitive
```

### Edit Update Rate (Responsiveness)
**File**: `MainForm.cs` → Method: `SetupUpdateTimer()`
```csharp
_updateTimer.Interval = 16; // Lower = more responsive, higher = less CPU
```

## Troubleshooting

### App won't start
1. Install .NET 6.0 runtime
2. Ensure Xbox controller is connected
3. Try running as Administrator

### Controller not detected
1. Check Windows Settings > Gaming > Xbox Game Controller
2. Verify controller shows in Device Manager
3. Restart HaloShift

### Mouse movement is jerky
1. Increase `SENSITIVITY` constant
2. Decrease `_updateTimer.Interval` (higher FPS)
3. Check for conflicting input software

### Mode toggle doesn't work
1. Press LB, RB, and Y all at the same time
2. Hold briefly (~100ms) before releasing
3. Ensure controller is connected

## File Structure Quick Reference

```
HaloShift/
├── Program.cs              ← Entry point
├── MainForm.cs             ← UI & app lifecycle
├── ControllerManager.cs    ← Xbox input polling
├── ModeManager.cs          ← Mode switching & input handling
├── InputSimulator.cs       ← Mouse/keyboard simulation
├── HaloShift.csproj        ← Project file
├── README.md               ← Full user documentation
├── SETUP.md                ← Developer setup guide
└── PROJECT_SUMMARY.md      ← Technical overview
```

## API Dependencies

- **SharpDX.XInput**: Xbox controller input (NuGet package)
- **Windows SendInput**: Mouse/keyboard automation (built-in)
- **.NET 6.0 Windows Forms**: UI framework (built-in)

## Running from Command Line

### Debug Mode
```bash
dotnet run -c Debug
```

### Release Mode (Optimized)
```bash
dotnet run -c Release
```

### Direct Executable
```bash
bin/Release/net6.0-windows/HaloShift.exe
```

## Advanced: Custom Key Bindings

Edit `ModeManager.cs` in `HandleMouseModeInput()` method:

```csharp
// Add new binding (example: A button = send E key)
if ((gamepad.Buttons & GamepadButtonFlags.A) != 0)
{
    InputSimulator.PressKey(0x45); // E key
}
```

Common key codes:
- F11 = 0x7A
- W = 0x57
- A = 0x41
- S = 0x53
- D = 0x44

## Performance Tuning

For maximum responsiveness:
1. Reduce `_updateTimer.Interval` to 8ms (125 FPS)
2. Reduce `DEADZONE` to 0.05
3. Increase `SENSITIVITY` to 20+
4. Close unnecessary background applications

For lower CPU usage:
1. Increase `_updateTimer.Interval` to 32ms (30 FPS)
2. Increase `DEADZONE` to 0.25
3. Decrease `SENSITIVITY` to 10

## Support & Resources

- **.NET Docs**: https://docs.microsoft.com/dotnet/
- **SharpDX**: http://sharpdx.org/
- **Windows XInput**: https://docs.microsoft.com/windows/win32/xinput/
- **SendInput API**: https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-sendinput

---
**Version**: 1.0
**Framework**: .NET 6.0 Windows Forms
**License**: Free for personal use
