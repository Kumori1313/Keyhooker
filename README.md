# Keyhooker

> A lightweight Windows system-tray utility that maps global hotkeys to process launches — configure once, trigger anything from anywhere.

## Overview

Keyhooker is a Windows Forms application that runs silently in the system tray and listens for user-defined global keyboard shortcuts. When a registered hotkey fires, Keyhooker launches the configured executable with optional command-line arguments. It uses the Windows `RegisterHotKey` API for reliable, system-wide hotkey capture that works regardless of which application has focus.

Key highlights:

- **System-tray resident** — zero taskbar clutter; right-click the tray icon to configure or exit
- **Live key recorder** — click any "Keys" cell in the configuration dialog and press your desired combination to record it instantly
- **Registry-backed configuration** — bindings survive reboots without any separate config file to manage
- **Optional auto-start** — one checkbox toggles the `HKCU\...\Run` entry so Keyhooker starts with Windows
- **Single-instance enforcement** — a named mutex prevents duplicate instances

## Prerequisites

| Requirement | Minimum version |
|---|---|
| Windows | Windows 7 or later (x64 recommended) |
| .NET SDK | .NET 10 (for building from source) |
| .NET Runtime | .NET 10 Windows Desktop Runtime (for running a published build) |

The .NET 10 SDK can be downloaded from [dot.net](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

## Installation

### Option A — Build from source

1. Clone or download the repository.

```bash
git clone https://github.com/your-username/Keyhooker.git
cd Keyhooker
```

2. Restore and build the project.

```bash
dotnet build "Keyhooker V2.csproj" -c Release
```

3. Run the compiled binary directly.

```bash
dotnet run --project "Keyhooker V2.csproj"
```

### Option B — Publish a self-contained executable

To produce a single `.exe` that does not require the .NET runtime to be pre-installed on the target machine:

```bash
dotnet publish "Keyhooker V2.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output executable will be placed in:

```
bin\Release\net10.0-windows\win-x64\publish\
```

Double-click `Keyhooker V2.exe` to start the application. A blue "K" icon will appear in the system tray.

## Configuration

All configuration is handled through the in-app graphical interface — there is no config file to edit manually.

### Opening the configuration dialog

- **Double-click** the tray icon, or
- **Right-click** the tray icon and choose **Configure...**

### Managing bindings

The dialog presents a three-column grid:

| Column | Description |
|---|---|
| **Keys** | The hotkey combination (e.g. `Ctrl+Alt+T`). Click the cell and press your desired key combination to record it. |
| **Command** | The executable or file to launch (e.g. `wt.exe`, `notepad.exe`, `C:\Scripts\run.bat`). |
| **Arguments** | Optional command-line arguments passed to the executable (e.g. `-d .` or `--mode dev`). |

Use the **Add** button to create a new row and **Remove** to delete the selected row. Click **Save** to persist all bindings and immediately reload hotkeys; click **Cancel** to discard changes.

### Supported key syntax

Modifiers and keys are joined with `+`. All of the following formats are accepted:

```
Ctrl+Alt+T
Ctrl+Shift+F5
Alt+Space
Win+R
Ctrl+Alt+Shift+Delete
F12
```

**Supported modifiers:** `Ctrl` / `Control`, `Alt`, `Shift`, `Win` / `Windows`

**Supported trigger keys:**

| Category | Keys |
|---|---|
| Letters | `A` – `Z` |
| Digits | `0` – `9` |
| Function keys | `F1` – `F12` |
| Navigation | `Up`, `Down`, `Left`, `Right`, `Home`, `End`, `PageUp` / `PgUp`, `PageDown` / `PgDn` |
| Editing | `Insert` / `Ins`, `Delete` / `Del`, `Backspace` |
| Control | `Space`, `Enter` / `Return`, `Tab`, `Escape` / `Esc` |
| Numpad | `Numpad0` – `Numpad9` |
| System | `PrintScreen` / `PrtSc`, `ScrollLock`, `Pause` |

### Auto-start on Windows login

Right-click the tray icon and check **Run on Startup**. This adds an entry to:

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

Uncheck the same menu item to remove the entry. No elevated (administrator) privileges are required because the entry is written to the current user's registry hive.

### Registry storage

Bindings are stored in the current user's registry under:

```
HKEY_CURRENT_USER\Software\KeyhookerV2\Bindings\
```

Each binding occupies a numbered sub-key (`0`, `1`, `2`, …) with three string values: `Keys`, `Command`, and `Args`. You can inspect or export these keys using `regedit.exe` if manual editing is ever needed.

## Usage Examples

| Goal | Keys | Command | Arguments |
|---|---|---|---|
| Open Windows Terminal in the current directory | `Ctrl+Alt+T` | `wt.exe` | `-d .` |
| Launch Notepad | `Ctrl+Alt+N` | `notepad.exe` | |
| Run a Python script | `Ctrl+Shift+P` | `python.exe` | `C:\Scripts\monitor.py` |
| Open a URL in the default browser | `Ctrl+Alt+B` | `https://github.com` | |
| Open File Explorer at a specific path | `Win+E` | `explorer.exe` | `C:\Projects` |

Processes are launched with `UseShellExecute = true`, which means shell verbs (open, run as, etc.) and file associations are fully supported. If a launch fails, a balloon notification appears above the tray icon with the error message.

## Project Structure

```
Keyhooker/
├── Program.cs                # Entry point; single-instance mutex + Application.Run
├── TrayApplicationContext.cs # System-tray icon, menu, hotkey dispatch loop
├── HotkeyManager.cs          # P/Invoke wrapper for RegisterHotKey / UnregisterHotKey
├── ConfigForm.cs             # Configuration dialog with live key-recorder
├── Keybinding.cs             # Plain data model (Id, Keys, Command, Args)
├── RegistryConfig.cs         # Registry read/write for bindings and auto-start
└── Keyhooker V2.csproj       # .NET 10 Windows Forms project file
```

## Development

### Building in debug mode

```bash
dotnet build "Keyhooker V2.csproj"
```

### Running tests

There is no automated test suite at this time. Manual testing involves:

1. Adding a binding in the configuration dialog.
2. Pressing the hotkey combination from a different foreground application.
3. Verifying the correct process launches.

### Hotkey conflict behaviour

If another application or Windows itself has already registered the same hotkey combination, `RegisterHotKey` will fail silently and Keyhooker will show a balloon tip warning stating how many bindings could not be registered. Conflicting bindings will not fire until the competing registration is released.

### Limitations

- Windows only — the project targets `net10.0-windows` and relies on Win32 APIs (`user32.dll`).
- `RegisterHotKey` does not support modifier-only hotkeys (e.g. pressing `Ctrl` alone).
- Some hotkeys reserved by Windows (e.g. `Win+L`, `Ctrl+Alt+Del`) cannot be intercepted by user-mode applications.