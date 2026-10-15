# D-Helper capabilities — Dell Pro 16 PC16250

Probe date: 2026-10-15  
Model: **Dell Pro 16 PC16250** (SKU 0D64, board 06RX6T)  
BIOS: 1.18.0  
CPU: Intel Core Ultra 7 155U  
OS: Windows 11 Pro

## Control surfaces

| Surface | Status | Notes |
|--------|--------|-------|
| Dell Optimizer `do-cli.exe` | **Installed** | `C:\Program Files\Dell\DellOptimizer\do-cli.exe` — requires **Administrator** |
| Dell Optimizer WMI | Not registered | `root\dcim\sysman\DellOptimizer` invalid until/unless WMI provider loads |
| Dell Command Configure `cctk.exe` | **Installed** | `C:\Program Files (x86)\Dell\Command Configure\X86_64\cctk.exe` — requires **Administrator** |
| `root\dcim\sysman` provider classes | Empty | Namespace exists; Command \| Monitor classes not present |
| AWCC WMI | Absent | Expected — not Alienware/G-Series |
| Win32_Battery | OK | Charge %, status, AC line via Windows Forms PowerStatus |

## Backend priority (locked)

1. **Primary:** Dell Optimizer CLI (`do-cli`) for thermal + charging  
2. **Fallback:** Dell Command Configure (`cctk`) for thermal + battery + FnLock  
3. **Always:** Windows APIs for sensors, AC/DC events, refresh rate, Windows power mode sync  

D-Helper runs elevated (app.manifest) so Optimizer/CCTK calls succeed.

## Feature mapping

| D-Helper UI | Dell API |
|-------------|----------|
| Quiet | Optimizer `ThermalMode=Quiet` / CCTK `ThermalManagement=Quiet` |
| Balanced | `Optimized` |
| Cool | `Cool` |
| Turbo | Optimizer `Ultra` / CCTK `UltraPerformance` |
| Battery Custom | Optimizer `ChargingMode=Custom` + start/stop / CCTK `PrimaryBattChargeCfg=Custom:start-stop` |
| Primarily AC | `PrimarilyAC` / `PrimAcUse` |
| Fan curves / GPU MUX / RGB | Unsupported on this platform |

## IT / security notes

- Secure Boot: On (from msinfo)  
- Writes require admin; BIOS setup password (if set by IT) will cause CCTK failures — surface clear errors  
- Re-run `tools\ProbeDell.ps1` as Administrator after BIOS changes to refresh `docs\probe-output.txt`

## Re-probe

```powershell
powershell -ExecutionPolicy Bypass -File tools\ProbeDell.ps1
```
