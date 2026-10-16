# D-Helper capabilities — Dell laptops

App: GPL fork of G-Helper with Dell Optimizer / CCTK bridge  
Target: any Dell laptop with Optimizer (`do-cli`) and/or Command | Configure (`cctk`)  
Validated example: **Dell Pro 16 PC16250** (Intel Core Ultra 7 155U)

## Control surfaces

| Surface | Status | Notes |
|--------|--------|-------|
| Dell Optimizer `do-cli.exe` | Used when installed | Thermal (+ charging fallback) |
| Dell Command Configure `cctk.exe` | Preferred for battery / Fn-Lock / thermal / Extra | Requires Administrator |
| ASUS ATKACPI | Absent | Expected on Dell |
| Dell WMI identity | Required | `AppConfig.IsDell()` gate |

## Backend priority

1. **CCTK** for thermal + battery + FnLock + Extra when available  
2. **Optimizer** for thermal (and charging only if CCTK missing)  
3. **Windows APIs** for sensors, AC/DC, refresh rate, overlay, power plan  

## Mode mapping

| UI mode | Dell thermal (aliases) |
|---------|------------------------|
| Quiet | Quiet / Silent |
| Balanced | Optimized / Balanced / Default |
| Cool | Cool |
| Turbo | UltraPerformance / Performance / Ultra |

Unsupported aliases soft-fail with a toast.

## Extra / Fn-Lock

Extra CCTK options (keyboard illumination, WoL, USB PowerShare, lid, camera, AutoOn, BlockSleep, charge mode) are shown only when a get returns a value for that option. Fn-Lock button is hidden when `FnLock` is not exposed.

## Updates

Dell Support links use this PC’s **Service Tag** (`Win32_BIOS.SerialNumber`), not a fixed product slug.

## Unsupported

GPU Eco/Ultimate/MUX, Aura RGB, Anime Matrix/Slash, Ally, ACPI fan curves, PPT limits, Pawn undervolt, ASUS driver scrape. Dells without Optimizer **and** CCTK cannot use D-Helper.
