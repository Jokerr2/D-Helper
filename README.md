# D-Helper

Lightweight tray app for **Dell Pro 16 PC16250** — a clean-room, G-Helper-*lookalike* remote control for Dell Optimizer / Command Configure.

> Not affiliated with ASUS G-Helper or Dell. Does **not** copy G-Helper source (GPL-3.0). Layout/behavior matched by reimplementation; reference clone stays in `_ref/` (gitignored).

## Feature matrix (PC16250)

| Area | Status |
|---|---|
| Silent / Balanced / Cool / Turbo | Working (CCTK / Optimizer thermal) |
| Battery charge limit | Working (CCTK `PrimaryBattChargeCfg` only) |
| Auto mode + refresh on AC/DC | Working |
| Tray left-click + toasts | Working |
| Hotkeys (mode cycle, open) | Working (on by default) |
| Sensors + metrics overlay | Working |
| Fn-Lock | Working when CCTK exposes it |
| Fans + Power window | Partial — Windows power + thermal; fan curves / PPT **disabled** |
| GPU Eco / Ultimate / Optimized | UI present, **disabled** (iGPU-only) |
| RGB / Anime Matrix / peripherals | UI present, **disabled** |
| Visual / overdrive / MiniLED | UI present, **disabled** |
| BIOS / driver Updates | Working — Dell Support links for PC16250 |

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

Close any running D-Helper first (the exe is locked while elevated). Then:

```powershell
Get-Process DHelper -ErrorAction SilentlyContinue | Stop-Process -Force
cd app
dotnet publish DHelper\DHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ..\publish
..\publish\DHelper.exe
```

One output: `publish\DHelper.exe` only.

See [docs/SETUP.md](docs/SETUP.md), [docs/CAPABILITIES.md](docs/CAPABILITIES.md), [docs/GHELPER_UI_MAP.md](docs/GHELPER_UI_MAP.md).

## Probe this PC

```powershell
powershell -ExecutionPolicy Bypass -File tools\ProbeDell.ps1
```
