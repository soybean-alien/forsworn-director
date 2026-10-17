# Forsworn Director by soybean_alien. MIT License.
# Records the README demo from the real game: drives the F8 panel with SendInput and captures the
# Forsworn window (2560x1440 client, panel at its default spot on a run's map screen) into frames/.
# Then run:  python tools/DemoGif/make_gif.py
# Usage: start a solo run, close the panel, then:  powershell -ExecutionPolicy Bypass -File tools/DemoGif/record.ps1
param([string]$Out = "$PSScriptRoot\frames")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class W {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, IntPtr e);
  [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint c, uint t);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
'@
[W]::SetProcessDPIAware() | Out-Null
$game = Get-Process Forsworn | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$h = $game.MainWindowHandle
$origin = New-Object W+POINT; [W]::ClientToScreen($h, [ref]$origin) | Out-Null
[W]::SetForegroundWindow($h) | Out-Null
New-Item -ItemType Directory -Force $Out | Out-Null
Get-ChildItem $Out -Filter *.png | Remove-Item

# Capture region around the panel (client pixels), scaled to the README size.
$crop = New-Object System.Drawing.Rectangle 252, 50, 1905, 1260
$size = New-Object System.Drawing.Size 1270, 840
$script:n = 0

function Frame([int]$hold = 1) {
    Start-Sleep -Milliseconds 450
    $full = New-Object System.Drawing.Bitmap $crop.Width, $crop.Height
    $g = [System.Drawing.Graphics]::FromImage($full)
    $g.CopyFromScreen($origin.X + $crop.X, $origin.Y + $crop.Y, 0, 0, $crop.Size)
    $small = New-Object System.Drawing.Bitmap $size.Width, $size.Height
    $g2 = [System.Drawing.Graphics]::FromImage($small)
    $g2.InterpolationMode = 'HighQualityBicubic'
    $g2.DrawImage($full, 0, 0, $size.Width, $size.Height)
    $script:n++
    $small.Save((Join-Path $Out ("{0:D2}_{1}.png" -f $script:n, $hold)), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $g2.Dispose(); $full.Dispose(); $small.Dispose()
}
function Hover([int]$x, [int]$y) { [W]::SetCursorPos($origin.X + $x, $origin.Y + $y) | Out-Null }
function Tap([int]$x, [int]$y) {
    # Unity reads input once per frame: settle on the target, hold, and let the release land before moving on.
    Hover $x $y; Start-Sleep -Milliseconds 250
    [W]::mouse_event(2, 0, 0, 0, [IntPtr]::Zero); Start-Sleep -Milliseconds 150; [W]::mouse_event(4, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 300
}
function Press([string]$name) {
    $vk = [byte][int][System.Windows.Forms.Keys]$name; $scan = [byte][W]::MapVirtualKey($vk, 0)
    [W]::keybd_event($vk, $scan, 0, [IntPtr]::Zero); Start-Sleep -Milliseconds 60; [W]::keybd_event($vk, $scan, 2, [IntPtr]::Zero)
}

# Panel coordinates at the default position (client pixels, 2560x1440). Every toggle the take
# touches is flipped back, so the player's settings end where they started.
$tabParty = 850, 376; $tabAssassin = 950, 376; $tabKnight = 1052, 376; $tabPaladin = 1254, 376; $tabWizard = 1558, 376
$rowGold = 1000, 506
$xp1000 = 1366, 934; $xp5000 = 1530, 934; $xp100 = 1204, 934
$allOn = 1436, 472; $allOff = 1540, 472
$boostDamage = 1150, 1012; $boostArmor = 1556, 1074; $boostThorns = 1150, 1196; $boostHealth = 1556, 1196
$away = 2050, 1000   # off the panel, away from edge-scroll and the map tools

Hover @away; Frame 2                    # the run, panel closed
Press F8; Frame 2                       # F8: Party page
Hover @rowGold; Frame 1
Tap @xp1000; Hover @away; Frame 1
Tap @xp5000; Hover @away; Frame 2       # x5000 battle XP
Tap @tabKnight; Hover @away; Frame 2    # Knight: per-class toggles and +20 stat boosts
Tap @boostDamage; Hover @away; Frame 1
Tap @boostDamage; Hover @away; Frame 1
Tap @boostArmor; Hover @away; Frame 1
Tap @boostArmor; Hover @away; Frame 1
Tap @boostThorns; Hover @away; Frame 1
Tap @boostThorns; Hover @away; Frame 1
Tap @boostHealth; Hover @away; Frame 1
Tap @boostHealth; Hover @away; Frame 1
Tap @tabWizard; Hover @away; Frame 2
Tap @allOff; Hover @away; Frame 1       # one click turns a whole class off ...
Tap @allOn; Hover @away; Frame 1        # ... and back on
Tap @tabPaladin; Hover @away; Frame 1
Tap @tabAssassin; Hover @away; Frame 1
Tap @tabParty; Hover @away; Frame 1
Tap @xp100; Hover @away; Frame 2        # back to the multiplier the run uses
Press F8; Frame 2                       # F8 again: back to the game
Write-Host "Captured $script:n frames in $Out"