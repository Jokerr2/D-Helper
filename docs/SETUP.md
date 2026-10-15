# D-Helper setup

## Prerequisites

1. **Windows 10/11** on a supported Dell (validated: Dell Pro 16 PC16250)
2. **.NET 8 Desktop Runtime** (or SDK to build)
3. At least one of:
   - [Dell Optimizer](https://www.dell.com/support) (`winget install Dell.Optimizer`)
   - [Dell Command \| Configure](https://www.dell.com/support/kbdoc/en-us/000178000/dell-command-configure) (`winget install Dell.CommandConfigure`)

## Build

```powershell
cd app
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

Output: `app\bin\Release\net8.0-windows\win-x64\publish\DHelper.exe`

## Run

Right-click **Run as administrator** (required for thermal/battery writes), or accept the UAC prompt when launched.

## First-run tip

Close or ignore Dell Optimizer’s own UI while using D-Helper so the two apps do not fight over thermal mode.
