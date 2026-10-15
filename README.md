# D-Helper

Lightweight tray app for **Dell Pro 16 PC16250** (and similar business Dells) — a clean-room, G-Helper-inspired remote control for Dell Optimizer / Command Configure thermal modes and battery charge limits.

> Not affiliated with ASUS G-Helper or Dell. Does not copy G-Helper source (GPL-3.0). UX/architecture inspired only.

## Features

- Quiet / Balanced / Cool / Turbo thermal modes
- Battery charge modes + custom charge window (e.g. 50–80%)
- Auto thermal + refresh rate on AC / battery
- System tray app (left-click icon to open — no shortcut required)
- Optional metrics overlay and Start with Windows
- `--charge` CLI for boot apply

## Requirements

- Windows 10/11, run **as Administrator**
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Dell Optimizer](https://www.dell.com/support) and/or [Dell Command \| Configure](https://www.dell.com/support/kbdoc/en-us/000178000/dell-command-configure)

```powershell
winget install Dell.Optimizer
winget install Dell.CommandConfigure
winget install Microsoft.DotNet.DesktopRuntime.8
```

## Build & run

```powershell
cd app
dotnet build -c Release
dotnet publish DHelper\DHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ..\publish
..\publish\DHelper.exe
```

See [docs/SETUP.md](docs/SETUP.md) and [docs/CAPABILITIES.md](docs/CAPABILITIES.md).

## Probe this PC

```powershell
powershell -ExecutionPolicy Bypass -File tools\ProbeDell.ps1
```
