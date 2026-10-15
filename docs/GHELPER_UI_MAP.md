# G-Helper UI map (clean-room reference)

Read-only reference: `_ref/g-helper/` (gitignored shallow clone of [seerge/g-helper](https://github.com/seerge/g-helper)).  
**Do not copy** `.cs` / designer / assets into D-Helper — MIT clean-room reimplementation only.

## Main window (`SettingsForm`)

| Section | G-Helper controls | D-Helper PC16250 |
|---|---|---|
| Header / sensors | Model + CPU/GPU/BAT/fan labels | Model + CPU / BAT / AC·DC / Hz strip |
| Performance | Silent · Balanced · Turbo (+ Cool) | Quiet · Balanced · Cool · Turbo (CCTK/Optimizer) |
| Fans + Power link | Opens `Fans` form | Opens Fans + Power form |
| GPU mode | Eco · Standard · Ultimate · Optimized · XGM | Same row, **disabled** (iGPU-only) |
| Screen | 60 / 120 / Auto / MiniLED / FHD | Auto refresh via Extra; visual pills **disabled** |
| Keyboard / Extra | Opens `Extra`; Fn-Lock; Overlay; backlight | Extra form; Fn-Lock (CCTK); Overlay; lighting **disabled** |
| Battery | Slider + % + full button | Charge mode + stop-% slider (CCTK `PrimaryBattChargeCfg`) |
| Visual / Gamma | Combo + slider | **Disabled** |
| Anime Matrix / RGB / peripherals | Combos + buttons | **Disabled** with tooltip |
| Footer | Startup · Updates · Quit | Startup · Updates · Quit |

## Secondary forms

| Form | Role | D-Helper |
|---|---|---|
| `Fans.cs` | Fan curves, PPT, Windows power | Windows power + thermal notes; curve/PPT **disabled** |
| `Extra.cs` | Hotkeys, startup, AC/DC, Fn-Lock | Same working Dell extras + backend status |
| `Updates.cs` | BIOS/driver scrape (ASUS) | Dell Support catalog links for PC16250 |

## Tray / input (`Program.cs`)

- Left-click tray → open main window near tray
- Right-click → mode shortcuts + Exit
- Balloon on first quiet start
- Hotkeys default **on** (Ctrl+Shift+F5 cycle mode, Ctrl+Shift+F12 open)

## Visual language (recreated)

- Dark compact tray window, section panels stacked top→bottom
- Mode pills with accent border when active
- Secondary nav buttons (Fans + Power, Extra, Updates)
- Disabled controls stay visible; tooltip: “Not available on Dell Pro”
