# G-Helper UI map (GPL fork)

Source: forked from [seerge/g-helper](https://github.com/seerge/g-helper) (GPL-3.0); app code lives in `app/DHelper`.  
Runtime UI is the upstream WinForms layout with Dell capability gates.

## Main window (`Settings`)

| Section | G-Helper | D-Helper PC16250 |
|---|---|---|
| Header / sensors | Model + CPU/GPU/BAT/fan | Same chrome; GPU sensors may be empty (iGPU) |
| Performance | Silent · Balanced · Turbo (+ custom) | Same + **Cool** custom mode via Dell thermal |
| Fans + Power link | Opens `Fans` | Opens Fans; curves/PPT disabled |
| GPU mode | Eco · Standard · Ultimate · Optimized · XGM | **Hidden** |
| Screen | Auto / Hz / MiniLED / FHD | Refresh Auto/min/max kept; MiniLED/OD gated off |
| Keyboard / Extra | Aura + Extra | Keyboard/Aura **hidden**; Extra opens (Dell extras) |
| Battery | Slider + full | Charge limit via CCTK bridge |
| Visual / Gamma | Combo + slider | **Hidden** |
| Anime Matrix / peripherals | Combos + buttons | **Hidden** |
| Footer | Startup · Updates · Quit | Same |

## Secondary forms

| Form | Role | D-Helper |
|---|---|---|
| `Fans.cs` | Fan curves, PPT, Windows power | Windows power kept; curve/PPT/UV **disabled** |
| `Extra.cs` | Hotkeys, backlight, services | Backlight/ASUS services **hidden**; Dell backend status shown |
| `Updates.cs` | ASUS BIOS/driver scrape | Dell Support catalog links |

## Tray / input

- Left-click tray → open main window
- Right-click → performance modes (no GPU submenu on Dell) + Overlay + Quit
- Hotkeys via G-Helper `InputDispatcher`
- Balloon / toast via upstream toast form

## Dell bridge

`Program.acpi` (`AsusACPI`) connects in **Dell-bridged mode** when ATKACPI is absent and Optimizer/CCTK probe succeeds. Bridged IDs: performance mode, battery limit, Fn-Lock.
