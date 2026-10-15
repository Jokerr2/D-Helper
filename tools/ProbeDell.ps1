# ProbeDell.ps1 — Discover Dell control surfaces for D-Helper (PC16250 and similar)
# Run: powershell -ExecutionPolicy Bypass -File tools\ProbeDell.ps1

$ErrorActionPreference = "Continue"
$out = @()
function Log($msg) { $script:out += $msg; Write-Output $msg }

Log "=== D-Helper Dell Probe $(Get-Date -Format o) ==="
$cs = Get-CimInstance Win32_ComputerSystem
$bios = Get-CimInstance Win32_BIOS
Log "Manufacturer: $($cs.Manufacturer)"
Log "Model: $($cs.Model)"
Log "BIOS: $($bios.SMBIOSBIOSVersion)"

Log ""
Log "=== Dell Optimizer (do-cli) ==="
$doPaths = @(
  "${env:ProgramFiles}\Dell\DellOptimizer\do-cli.exe",
  "${env:ProgramFiles}\Dell\Dell Optimizer\do-cli.exe",
  "${env:ProgramFiles(x86)}\Dell\DellOptimizer\do-cli.exe",
  "${env:ProgramFiles}\Dell\Optimizer\do-cli.exe"
)
$doCli = $doPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($doCli) {
  Log "FOUND: $doCli"
  & $doCli /get -name=SystemPowerConfiguration.ThermalMode 2>&1 | ForEach-Object { Log "  $_" }
  & $doCli /get -name=SystemPowerConfiguration.ChargingMode 2>&1 | ForEach-Object { Log "  $_" }
} else {
  Log "NOT FOUND: do-cli.exe"
}

Log ""
Log "=== Dell Optimizer WMI ==="
try {
  $svc = Get-CimInstance -Namespace root\dcim\sysman\DellOptimizer -ClassName DellOptimizerService -ErrorAction Stop
  Log "FOUND: DellOptimizerService"
  $svc | Format-List * | Out-String | ForEach-Object { Log $_ }
} catch {
  Log "NOT AVAILABLE: $($_.Exception.Message)"
}

Log ""
Log "=== Dell Command Configure (cctk) ==="
$cctkPaths = @(
  "${env:ProgramFiles(x86)}\Dell\Command Configure\X86_64\cctk.exe",
  "${env:ProgramFiles}\Dell\Command Configure\X86_64\cctk.exe",
  "${env:ProgramFiles(x86)}\Dell\Command Configure\cctk.exe"
)
$cctk = $cctkPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $cctk) {
  $found = Get-ChildItem -Path "${env:ProgramFiles}\Dell","${env:ProgramFiles(x86)}\Dell" -Recurse -Filter cctk.exe -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($found) { $cctk = $found.FullName }
}
if ($cctk) {
  Log "FOUND: $cctk"
  & $cctk --ThermalManagement 2>&1 | ForEach-Object { Log "  $_" }
  & $cctk --PrimaryBattChargeCfg 2>&1 | ForEach-Object { Log "  $_" }
  & $cctk --FnLock 2>&1 | ForEach-Object { Log "  $_" }
} else {
  Log "NOT FOUND: cctk.exe"
}

Log ""
Log "=== root\dcim\sysman ==="
try {
  $ns = Get-CimInstance -Namespace root\dcim -ClassName __NAMESPACE -ErrorAction Stop
  Log "Namespaces under root\dcim: $($ns.Name -join ', ')"
  $classes = Get-CimClass -Namespace root\dcim\sysman -ErrorAction Stop |
    Where-Object { $_.CimClassName -notlike '__*' -and $_.CimClassName -notlike 'CIM_*' -and $_.CimClassName -notlike 'MSFT_*' } |
    Select-Object -ExpandProperty CimClassName
  if ($classes) {
    Log "Provider classes: $($classes -join ', ')"
  } else {
    Log "No Dell provider classes registered (empty sysman namespace)"
  }
} catch {
  Log "ERROR: $($_.Exception.Message)"
}

Log ""
Log "=== AWCC WMI ==="
try {
  $awcc = Get-CimClass -Namespace root\WMI -ClassName AWCCWmiMethodFunction -ErrorAction Stop
  Log "FOUND: AWCCWmiMethodFunction (Alienware/G-Series)"
} catch {
  Log "NOT PRESENT (expected on business Dell Pro)"
}

Log ""
Log "=== Windows battery / power ==="
$batt = Get-CimInstance Win32_Battery -ErrorAction SilentlyContinue
if ($batt) {
  Log "Battery: $($batt.Name) charge=$($batt.EstimatedChargeRemaining)% status=$($batt.BatteryStatus)"
} else {
  Log "No Win32_Battery"
}
Add-Type -AssemblyName System.Windows.Forms
$ps = [System.Windows.Forms.SystemInformation]::PowerStatus
Log "PowerLine: $($ps.PowerLineStatus) LifePercent: $($ps.BatteryLifePercent)"

Log ""
Log "=== Installed Dell software ==="
Get-ChildItem "${env:ProgramFiles}\Dell","${env:ProgramFiles(x86)}\Dell" -ErrorAction SilentlyContinue |
  ForEach-Object { Log "  $($_.FullName)" }
Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*,
  HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\* -ErrorAction SilentlyContinue |
  Where-Object { $_.DisplayName -match 'Dell|Optimizer|Command Configure|Power Manager' } |
  ForEach-Object { Log "  $($_.DisplayName) $($_.DisplayVersion)" }

$reportPath = Join-Path $PSScriptRoot "..\docs\probe-output.txt"
$out -join "`n" | Set-Content -Path $reportPath -Encoding UTF8
Log ""
Log "Wrote $reportPath"
