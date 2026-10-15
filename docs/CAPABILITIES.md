# D-Helper capabilities — Dell Pro 16 PC16250

Model: **Dell Pro 16 PC16250**  
CPU: Intel Core Ultra 7 155U (iGPU)  
App: GPL fork of G-Helper with Dell Optimizer / CCTK bridge

## Control surfaces

| Surface | Status | Notes |
|--------|--------|-------|
| Dell Optimizer `do-cli.exe` | Used when installed | Thermal (+ charging fallback) |
| Dell Command Configure `cctk.exe` | Preferred for battery / Fn-Lock / thermal | Requires Administrator |
| ASUS ATKACPI | Absent | Expected on Dell Pro |
| AWCC WMI | Absent | Not Alienware |

## Backend priority

1. **CCTK** for thermal + battery + FnLock when available  
2. **Optimizer** for thermal (and charging only if CCTK missing)  
3. **Windows APIs** for sensors, AC/DC, refresh rate, overlay, power plan  

## Mode mapping

| UI mode | Dell thermal |
|---------|----------------|
| Silent | Quiet |
| Balanced | Optimized |
| Cool | Cool |
| Turbo | Ultra / UltraPerformance |

## Unsupported on this platform

GPU Eco/Ultimate/MUX, Aura RGB, Anime Matrix/Slash, Ally, ACPI fan curves, PPT limits, Pawn undervolt, ASUS driver scrape.
