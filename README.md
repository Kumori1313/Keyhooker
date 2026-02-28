# Windows Startup Keybinding Utility - Project Specification

## Project Overview

A lightweight Windows application that provides global keyboard shortcuts to launch processes with custom arguments. The utility runs on startup and offers a simple configuration system with an easy-to-use interface.

## Technology Stack

- **Language**: C#
- **UI Framework**: WPF
- **Runtime**: .NET
- **Configuration**: JSON

## Core Features

1. **Global Keyboard Hooks**
   - Low-level keyboard hook access
   - Support for complex key combinations (e.g., Ctrl+Alt+T)

2. **Startup Integration**
   - Toggleable auto-start functionality
   - Persistent across system reboots

3. **Lightweight UI**
   - Minimal resource footprint
   - Simple configuration interface

4. **Process Launching**
   - Execute processes with custom arguments
   - Shell execution support

5. **Long-term Maintainability**
   - Clean architecture
   - JSON-based configuration
   - Modular component design

## Architecture

```
Startup App
 ├─ Config loader (JSON)
 ├─ Keyboard hook manager
 ├─ Action dispatcher
 └─ UI window (Optional)
```

### Component Responsibilities

- **Config Loader**: Reads and parses JSON configuration files
- **Keyboard Hook Manager**: Handles low-level keyboard events and hotkey detection
- **Action Dispatcher**: Executes configured actions based on triggered hotkeys
- **UI Window**: Provides user interface for configuration and settings

## Configuration Format

The application uses JSON for storing keybindings:

```json
{
  "bindings": [
    {
      "keys": "Ctrl+Alt+T",
      "command": "wt.exe",
      "args": "-d ."
    }
  ]
}
```

### Configuration Schema

- `keys`: Key combination string (e.g., "Ctrl+Alt+T")
- `command`: Executable or command to run
- `args`: Command-line arguments (optional)

## Process Launching Implementation

The application launches processes using `Process.Start` with shell execution:

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = "python.exe",
    Arguments = "script.py --mode dev",
    UseShellExecute = true
});
```

## Startup Integration Methods

### Option 1: Registry-based

Add application to Windows startup via registry:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

**Pros**: Centralized, easy to manage programmatically  
**Cons**: Requires registry permissions

### Option 2: Startup Folder

Place shortcut in Windows startup folder:

```
shell:startup
```

**Pros**: No registry modification needed, user-visible  
**Cons**: Requires file system access

## Release Strategy

The application will be released in two distinct builds:

### Build 1: Hotkey Registration Model
- Uses Windows API `RegisterHotKey`
- Lighter weight
- System-level hotkey registration
- May have conflicts with other applications

### Build 2: Full Keyboard Interception
- Uses low-level keyboard hooks
- Complete control over key combinations
- No conflicts with other applications
- Higher resource usage

## Development Roadmap

1. **Phase 1: Core Infrastructure**
   - JSON config loader
   - Basic process launcher
   - Simple UI shell

2. **Phase 2: Keyboard Hook Implementation**
   - Hotkey Registration Model (Build 1)
   - Full Keyboard Interception (Build 2)
   - Action dispatcher integration

3. **Phase 3: Startup Integration**
   - Registry-based startup (primary)
   - Startup folder fallback
   - Toggle UI control

4. **Phase 4: UI Refinement**
   - Configuration editor
   - Hotkey conflict detection
   - System tray integration (optional)

5. **Phase 5: Testing & Release**
   - Build both variants
   - Performance testing
   - Documentation

## Technical Considerations

- **Permissions**: May require elevated privileges for low-level hooks
- **Performance**: Keyboard hooks should be lightweight to avoid input lag
- **Error Handling**: Graceful handling of missing executables or invalid configurations
- **Conflicts**: Detection and notification of hotkey conflicts
- **Uninstallation**: Clean removal of startup entries

## Future Enhancement Ideas

- Import/export configurations
- Hotkey recording UI
- Process monitoring and restart
- Multiple profiles
- Conditional actions based on active window
- Macro support (key sequences)