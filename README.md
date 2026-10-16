# D-Helper

**GPL-3.0 fork of [G-Helper](https://github.com/seerge/g-helper)** adapted for **Dell Pro 16 PC16250**.

Uses the real G-Helper UI and tray behavior. On this Dell laptop (no ASUS ATKACPI), thermal modes, battery charge limit, and Fn-Lock are driven through **Dell Optimizer** (`do-cli`) and/or **Dell Command | Configure** (`cctk`). ASUS-only features (GPU MUX, Aura, Anime Matrix, Ally, ACPI fan curves / PPT) are hidden or disabled.

> Not affiliated with ASUS, G-Helper upstream, or Dell.

## License

**GNU General Public License v3.0** — see [LICENSE](LICENSE) and [NOTICE](NOTICE).

This is a modified GPL work based on [seerge/g-helper](https://github.com/seerge/g-helper). If you distribute binaries, you must also provide corresponding source under GPL-3.0.

## Feature matrix (PC16250)

| Area | Status |
|---|---|
| Silent / Balanced / Turbo (+ Cool) | Working (CCTK / Optimizer thermal) |
| Battery charge limit | Working (CCTK `PrimaryBattChargeCfg`) |
| Auto mode + refresh on AC/DC | Working (G-Helper auto modes + Dell apply) |
| Tray, toast, hotkeys, Extra | Working (G-Helper UI; ASUS backlight/services hidden) |
| Sensors + metrics overlay | Working |
| Fn-Lock | Working when CCTK exposes it |
| Fans + Power | Windows power plan; fan curves / PPT / undervolt **disabled** |
| GPU Eco / Ultimate / Optimized | **Hidden** (iGPU-only) |
| RGB / Anime Matrix / peripherals | **Hidden** |
| BIOS / driver Updates | Dell Support links for PC16250 |

## Requirements

- Windows 10/11, run **as Administrator** (UAC)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Dell Optimizer](https://www.dell.com/support) and/or [Dell Command \| Configure](https://www.dell.com/support/kbdoc/en-us/000178000/dell-command-configure)

```powershell
winget install Dell.Optimizer
winget install Dell.CommandConfigure
winget install Microsoft.DotNet.DesktopRuntime.8
```

## Build & run

Close any running D-Helper first. Then:

```powershell
Get-Process DHelper -ErrorAction SilentlyContinue | Stop-Process -Force
cd app
dotnet publish DHelper\DHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ..\publish
..\publish\DHelper.exe
```

One output: `publish\DHelper.exe`.

See [docs/SETUP.md](docs/SETUP.md), [docs/CAPABILITIES.md](docs/CAPABILITIES.md), [docs/DHELPER_UI_MAP.md](docs/DHELPER_UI_MAP.md).

## Probe this PC

```powershell
powershell -ExecutionPolicy Bypass -File tools\ProbeDell.ps1
```
