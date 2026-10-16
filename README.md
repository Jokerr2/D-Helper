# D-Helper

**GPL-3.0 fork of [G-Helper](https://github.com/seerge/g-helper)** for **Dell laptops** that have **Dell Optimizer** and/or **Dell Command | Configure**.

Uses the G-Helper-style UI and tray. Thermal modes, battery charge limit, Extra BIOS options, and Fn-Lock (when exposed) are driven through `do-cli` / `cctk`. ASUS-only features (GPU MUX, Aura, Anime Matrix, Ally, ACPI fan curves / PPT) are removed or hidden.

Validated on **Dell Pro 16 PC16250**; designed to work on any Dell with the Dell tools installed (Latitude, XPS, Precision, Pro, Inspiron, etc.). Unsupported BIOS options are hidden automatically.

> Not affiliated with ASUS, G-Helper upstream, or Dell.

## License

**GNU General Public License v3.0** — see [LICENSE](LICENSE) and [NOTICE](NOTICE).

This is a modified GPL work based on [seerge/g-helper](https://github.com/seerge/g-helper). If you distribute binaries, you must also provide corresponding source under GPL-3.0.

## Feature matrix

| Area | Status |
|---|---|
| Quiet / Balanced / Cool / Turbo | Working when CCTK/Optimizer exposes thermal |
| Battery charge limit | Working (`PrimaryBattChargeCfg` via CCTK) |
| Auto mode + refresh on AC/DC | Working |
| Tray, toast, hotkeys, Extra | Working; Extra rows hide if BIOS lacks the option |
| Sensors + metrics overlay | Working |
| Fn-Lock | Working when CCTK exposes `FnLock` |
| Fans + Power | Windows power plan; ASUS fan curves / PPT **disabled** |
| GPU Eco / Ultimate / Optimized | **Hidden** |
| RGB / Anime Matrix / peripherals | **Removed** |
| BIOS / driver Updates | Dell Support links for **this** Service Tag |

## Requirements

- Windows 10/11 **Dell** laptop, run **as Administrator** (UAC)
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
