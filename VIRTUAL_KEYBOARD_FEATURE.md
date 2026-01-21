# Virtual Keyboard Feature

## Overview
A custom on-screen virtual keyboard designed specifically for use with fullscreen games and applications. The keyboard stays visible on top of all windows and can be fully controlled with an Xbox controller's d-pad.

## How to Open
While in **Mouse Mode**, press the **Y button** (without LB or RB) to open the virtual keyboard.

## Features

### Always-On-Top Design
- Uses `HWND_TOPMOST` and `WS_EX_TOPMOST` flags to stay above fullscreen games
- Persists through game alt-tabs and fullscreen transitions
- Designed with Xbox-themed dark interface matching HaloShift aesthetic

### Controller Navigation
- **D-Pad Up/Down/Left/Right**: Navigate between keys
- **A Button**: Select and type the highlighted character
- **X Button**: Backspace - delete last character
- **B Button**: Close the keyboard
- **Done Button**: Alternative way to close keyboard

### Smart Integration
- **Automatically disables sensitivity adjustment** while keyboard is active
- Prevents d-pad from changing mouse sensitivity during navigation
- Resumes normal controller input after keyboard closes

### Character Support
- Numbers: 1234567890
- Letters: QWERTYUIOP, ASDFGHJKL, ZXCVBNM
- Space bar (wide key at bottom)
- Backspace and Done buttons

### Visual Feedback
- Selected key highlighted in bright green (Xbox accent color)
- Unselected keys shown in dark gray
- Text input displayed in real-time at top of keyboard
- Xbox-themed color scheme matching main application

## Technical Implementation

### Window Properties
```csharp
this.TopMost = true;
this.FormBorderStyle = FormBorderStyle.None;
this.ShowInTaskbar = false;
SetWindowPos(this.Handle, HWND_TOPMOST, ...);
```

### Input Handling
The keyboard intercepts controller input during the main update loop:
```csharp
if (_virtualKeyboard?.Visible == true)
{
    _virtualKeyboard.HandleInput(currentState);
    return; // Skip other input processing
}
```

### Key Transmission
Each key press is transmitted to the system using `InputSimulator`:
- Converts characters to virtual key codes via `VkKeyScan()`
- Handles shift modifier automatically for uppercase letters
- Sends complete key press (down + up) to active application

## Use Cases

1. **Game Chat**: Type messages in fullscreen games without Alt+Tab
2. **Search Fields**: Enter search terms in game menus or Steam overlay
3. **Password Entry**: Input credentials in game launchers
4. **Player Names**: Enter custom names in character creation screens
5. **Console Commands**: Type debug commands in game consoles

## Files Modified/Created

### New Files
- `VirtualKeyboard.cs`: Main keyboard form and logic (446 lines)

### Modified Files
- `MainForm.cs`: Added keyboard instance, event handler, and integration
- `ModeManager.cs`: Added Y button trigger and ShowKeyboardRequested event
- `ControlsWindow.cs`: Added keyboard controls documentation
- `README.md`: Updated with virtual keyboard documentation

## Architecture

```
MainForm
  ├── ModeManager (detects Y button press)
  │   └── ShowKeyboardRequested event → MainForm
  ├── VirtualKeyboard (handles d-pad navigation)
  │   └── KeyboardClosed event → MainForm
  └── UpdateTimer
      └── Routes input to VirtualKeyboard when visible
          or to ModeManager when keyboard closed
```

## Future Enhancements

Potential improvements:
- Add special characters row (!@#$%^&*)
- Lowercase/uppercase toggle
- Shift key for alternate characters
- Tab key support
- Enter key for multi-line input
- Customizable keyboard layouts
- Sound effects for key presses
- Animation transitions
- Remember last cursor position
