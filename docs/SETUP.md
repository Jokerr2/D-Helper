# D-Helper setup

## Prerequisites

1. **Windows 10/11** on a supported Dell (validated: Dell Pro 16 PC16250)
2. **.NET 8 Desktop Runtime** (or SDK to build)
3. At least one of:
   - [Dell Optimizer](https://www.dell.com/support) (`winget install Dell.Optimizer`)
   - [Dell Command \| Configure](https://www.dell.com/support/kbdoc/en-us/000178000/dell-command-configure) (`winget install Dell.CommandConfigure`)

## Build

```powershell
Get-Process DHelper -ErrorAction SilentlyContinue | Stop-Process -Force
cd app
dotnet publish DHelper\DHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ..\publish
```

Output: `publish\DHelper.exe`

## Run

Accept the UAC prompt (**Administrator** required for thermal/battery writes).

## First-run tip

Close or ignore Dell Optimizer’s own UI while using D-Helper so the two apps do not fight over thermal mode.

## Source / license

This is a GPL-3.0 modified version of [G-Helper](https://github.com/seerge/g-helper). See root `LICENSE` and `NOTICE`.
