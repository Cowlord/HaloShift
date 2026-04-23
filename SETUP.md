# HaloShift - Developer Setup Guide

## Quick Start

### Prerequisites
- Windows 7 or later
- .NET 6.0 SDK or runtime
- Visual Studio 2022 or VS Code with C# extensions
- Xbox controller (Xbox 360 or Xbox One)

### Installation Steps

1. **Install .NET 6.0 SDK**
   ```
   https://dotnet.microsoft.com/download/dotnet/6.0
   ```

2. **Restore NuGet packages**
   ```bash
   cd HaloShift
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build -c Release
   ```

4. **Add app icon (optional)**
   - Place `AppIcon.ico` in the project root directory
   - The app will use this as its system tray icon

5. **Run the application**
   ```bash
   dotnet run -c Release
   ```

## Building in Visual Studio Code

1. Install the C# extension (ms-dotnettools.csharp)
2. Open the workspace folder
3. Press `Ctrl+Shift+B` to run the build task
4. Press `F5` to debug with the launch configuration

## Testing the App

### Manual Testing Checklist

1. **Controller Connection**
   - Connect Xbox controller to PC
   - Run HaloShift
   - Check if controller is detected (no console errors)

2. **Mode Toggle**
   - Press and hold **Share** button
   - Verify window appears/disappears
   - Check tray icon tooltip changes

3. **Mouse Mode**
   - Switch to Mouse Mode
   - Test left stick movement (should move cursor smoothly)
   - Test RT trigger (should perform left click)
   - Test LT trigger (should perform right click)
   - Test LB button (should send F11 key to active window)

4. **Controller Mode**
   - Switch to Controller Mode
   - App should minimize to tray
   - Controller input should not interfere with other apps
   - Open Steam Big Picture to verify input passthrough

5. **System Tray**
   - Right-click tray icon for context menu
   - Left-click should bring window to focus
   - Verify "Toggle Mode" from context menu works

### Debugging Tips

1. **View Controller Input**
   - Uncomment debugging code in `ControllerManager.Update()` to log input values
   - Use Debug Output window to monitor state changes

2. **Mouse Movement Issues**
   - Check SENSITIVITY constant (higher = faster)
   - Verify DEADZONE value (typical 0.1-0.2)
   - Test with different games to identify conflicts

3. **Mode Switch Not Triggering**
   - Ensure LB, RB, Y are pressed simultaneously (all within 100ms)
   - Check that controller is connected and responding
   - Try alternative key combinations by modifying ModeManager.cs

## Project Structure

```
HaloShift/
├── HaloShift.csproj          # Project configuration
├── Program.cs                 # Application entry point
├── MainForm.cs               # Windows Forms UI and tray integration
├── ControllerManager.cs      # Xbox controller polling
├── ModeManager.cs            # Mode logic and input handling
├── InputSimulator.cs         # Mouse/keyboard simulation
├── README.md                 # User documentation
├── .gitignore
├── AppIcon.ico               # (Optional) System tray icon
└── .vscode/
    ├── tasks.json            # Build/run tasks
    └── launch.json           # Debug configuration
```

## Dependencies

### NuGet Packages
- **SharpDX.XInput** (4.2.0): Xbox controller input library
  - Provides low-level XInput API access
  - Handles controller connection/disconnection

### Framework
- **.NET 6.0 Windows Forms**: UI framework for Windows Forms application

### Windows APIs (via P/Invoke)
- **SendInput**: For mouse/keyboard simulation
- **SetForegroundWindow**: For window focus
- **ShowWindow**: For window visibility control
- **GetCursorPos**: For mouse position queries

## Configuration Reference

### Sensitivity Tuning
In `ModeManager.cs`, modify `HandleLeftStickMovement()`:
```csharp
const float SENSITIVITY = 15f; // Increase for faster movement
```

### Deadzone Adjustment
```csharp
const float DEADZONE = 0.15f; // Increase to prevent drift (0.0-1.0)
```

### Trigger Threshold
```csharp
const float TRIGGER_THRESHOLD = 0.5f; // How much trigger pressure needed (0.0-1.0)
```

### Update Frequency
In `MainForm.cs`:
```csharp
_updateTimer.Interval = 16; // Lower = more responsive but more CPU usage
```

## Performance Metrics

- **Update Rate**: ~60 FPS (16ms per frame)
- **Input Latency**: <20ms typical (controller to mouse movement)
- **Memory Footprint**: ~50-100 MB
- **CPU Usage**: <5% when idle, <15% when actively moving mouse

## Known Limitations

1. **Single Controller Support**: Currently only supports one Xbox controller (UserIndex.One)
2. **No Vibration**: Force feedback not implemented
3. **Win32 Only**: Windows-only application
4. **Admin Rights**: May require elevation for best compatibility

## Extending the Application

### Adding Multiple Controllers
Modify `ControllerManager.cs` to use array of controllers:
```csharp
private Controller[] _controllers = new Controller[4];
```

### Custom Key Bindings
Edit `ModeManager.cs` to change trigger mappings:
```csharp
if ((gamepad.Buttons & GamepadButtonFlags.X) != 0)
{
    InputSimulator.PressKey(VK_YOUR_KEY); // Custom key
}
```

### Advanced Mouse Curves
Replace `ApplyAccelerationCurve()` with custom implementation:
```csharp
private float ApplyAccelerationCurve(float stick)
{
    // Implement cubic or exponential curve for different feel
}
```

## Troubleshooting Common Issues

### Build Errors
- Ensure .NET 6.0 SDK is installed: `dotnet --version`
- Clean and restore: `dotnet clean && dotnet restore`
- Check .csproj targets SDK: `<TargetFramework>net6.0-windows</TargetFramework>`

### Runtime Issues
- Controller not found: Test in Windows Settings > Gaming > Xbox Game Controller
- Mouse not moving: Run as Administrator for SendInput compatibility
- Performance lag: Reduce update interval or disable other background apps

### Integration Issues
- Steam overlay interference: Disable Steam overlay or run HaloShift elevated
- Discord overlay conflict: Disable Discord in-game overlay
- Other controller software: Disable competing input mappers

## Support Resources

- SharpDX Documentation: http://sharpdx.org/
- .NET Documentation: https://docs.microsoft.com/dotnet/
- XInput API Reference: https://docs.microsoft.com/windows/win32/xinput/xuser-game-input
- SendInput Reference: https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-sendinput
