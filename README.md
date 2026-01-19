# HaloShift - Xbox Controller to Mouse/Keyboard Bridge

A high-performance C# application that runs in the background and listens for Xbox controller input, allowing you to control your PC using an Xbox controller with smooth acceleration and low-latency input handling.

## Features

### Dual Mode System
- **Controller Mode**: App minimized, input released back to Steam/other applications
- **Mouse Mode**: App takes foreground, controller drives mouse and keyboard inputs

### Mode Switching
- Press **LB + RB + Y** simultaneously to toggle between modes
- Smooth transition with visual feedback in system tray

### Mouse Mode Controls
- **Left Stick**: Moves mouse cursor with smooth quadratic acceleration and deadzone
- **RT (Right Trigger)**: Left click
- **LT (Left Trigger)**: Right click
- **LB (Left Bumper)**: Sends F11 keystroke (full-screen toggle)

### Technical Highlights
- High-performance polling (~60 FPS) for responsive input handling
- Smooth acceleration curve for natural mouse movement
- 15% deadzone on analog sticks
- Quadratic acceleration function for precise control
- System tray integration with context menu
- Minimizes to background in Controller Mode to avoid interference with Steam

## System Requirements

- Windows 7 or later
- .NET 6.0 runtime
- Xbox 360/Xbox One controller connected via USB or wireless adapter
- Administrator privileges (recommended for best compatibility)

## Installation

1. Build the project:
   ```bash
   dotnet build -c Release
   ```

2. Place your `AppIcon.ico` file in the output directory (optional)

3. Run the executable:
   ```bash
   HaloShift.exe
   ```

4. The app appears as a system tray icon. Minimize or close the window - the app will continue running in the background.

## Building from Source

Prerequisites:
- Visual Studio 2022 or .NET SDK 6.0+
- Windows development environment

Build steps:
```bash
cd HaloShift
dotnet restore
dotnet build -c Release
```

Output executable: `bin/Release/net6.0-windows/HaloShift.exe`

## System Tray Menu

Right-click or left-click the tray icon for:
- **Show**: Brings the window to focus
- **Toggle Mode**: Manually switch between modes
- **Exit**: Closes the application

## Configuration

To customize the behavior, edit the following in the source code:

### Input Sensitivity
In `ModeManager.cs`, adjust `SENSITIVITY`:
```csharp
const float SENSITIVITY = 15f; // Pixels per frame
```

### Deadzone
In `ModeManager.cs`, adjust `DEADZONE`:
```csharp
const float DEADZONE = 0.15f; // 15% of stick range
```

### Trigger Threshold
In `ModeManager.cs`, adjust `TRIGGER_THRESHOLD`:
```csharp
const float TRIGGER_THRESHOLD = 0.5f; // 50% pressure
```

### Update Rate
In `MainForm.cs`, adjust timer interval:
```csharp
_updateTimer.Interval = 16; // Milliseconds (~60 FPS)
```

## Architecture

### Main Components

- **Program.cs**: Application entry point
- **MainForm.cs**: Windows Forms window and system tray integration
- **ControllerManager.cs**: Xbox controller input polling and state management
- **ModeManager.cs**: Mode switching logic and input handling
- **InputSimulator.cs**: Mouse and keyboard input simulation via SendInput

### Key Classes

#### ControllerManager
Manages Xbox controller connection and state polling via SharpDX.XInput
- `Update()`: Polls current controller state
- `GetCurrentState()`: Returns latest gamepad state
- `StateChanged` event: Fired when controller input changes

#### ModeManager
Handles mode switching and mode-specific input processing
- `SwitchMode()`: Toggles between Controller and Mouse modes
- `Update()`: Checks for mode switch input combination
- `HandleMouseModeInput()`: Processes mouse/keyboard input when in Mouse Mode
- `ModeChanged` event: Fired when mode changes

#### InputSimulator
Uses Windows SendInput API for mouse and keyboard automation
- `MoveMouse()`: Relative mouse movement
- `LeftClick() / RightClick()`: Mouse clicks
- `PressKey()`: Keyboard key press (down + up)

## Performance Considerations

- Update loop runs at ~60 FPS (16ms interval) for responsive input
- Quadratic acceleration curve provides smooth but controlled movement
- Analog stick deadzone (15%) prevents drift
- Mouse position queries are minimal to reduce latency
- All input is processed on high-priority update thread

## Troubleshooting

### Controller Not Detected
1. Ensure Xbox controller is connected and recognized by Windows
2. Test in Windows Game Controller settings
3. Check Device Manager for driver issues
4. Restart HaloShift application

### Mouse Movement Jerky
1. Increase `SENSITIVITY` value for smoother acceleration
2. Check for other mouse control software that might conflict
3. Reduce update timer interval (lower = more responsive but uses more CPU)

### Mode Toggle Not Working
1. Ensure all three buttons (LB, RB, Y) are pressed simultaneously
2. Try holding for 100ms before releasing
3. Check controller battery level

### AppIcon.ico Not Loading
1. Ensure AppIcon.ico is in the same directory as HaloShift.exe
2. Icon file should be 32x32 or larger
3. App will fall back to default Windows icon if file is missing

## License

Designed for personal use with Xbox controllers on Windows PC.

## Dependencies

- SharpDX.XInput (4.2.0): Xbox controller input
- .NET 6.0 Windows Forms: UI and windowing
- Windows SendInput API: Input simulation
