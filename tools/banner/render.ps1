# Forsworn Director by soybean_alien. MIT License.
# Renders tools/banner/banner.html to docs/media/banner.png (2560x1280) and docs/media/social-preview.png
# (1280x640, GitHub's social preview; must stay under 1 MB) with a local headless Chrome or Edge.
# tools/banner/shots/panel.png is a real in-game capture of the F8 panel.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$browser = @("C:\Program Files\Google\Chrome\Application\chrome.exe", "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
             "C:\Program Files\Microsoft\Edge\Application\msedge.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $browser) { throw "Chrome or Edge is needed" }
New-Item -ItemType Directory -Force "$root\docs\media" | Out-Null
$html = (Resolve-Path "$PSScriptRoot\banner.html").Path
$profile = Join-Path $env:TEMP "forsworn-director-banner-profile"
foreach ($r in @(@{ Out = "$root\docs\media\banner.png"; Scale = "1" }, @{ Out = "$root\docs\media\social-preview.png"; Scale = "0.5" })) {
    $p = Start-Process $browser -PassThru -WindowStyle Hidden -ArgumentList @("--headless=new", "--disable-gpu", "--hide-scrollbars",
        "--user-data-dir=$profile", "--force-device-scale-factor=$($r.Scale)", "--window-size=2560,1280",
        "--virtual-time-budget=5000", "--screenshot=$($r.Out)", ([Uri]$html).AbsoluteUri)
    $p.WaitForExit(60000) | Out-Null
    Write-Host "$(Split-Path $r.Out -Leaf): $([math]::Round((Get-Item $r.Out).Length / 1KB)) KB"
}

# Mod icon for Thunderstore/Nexus (256x256).
$icon = "$root\docs\media\icon.png"
$p = Start-Process $browser -PassThru -WindowStyle Hidden -ArgumentList @("--headless=new", "--disable-gpu", "--hide-scrollbars",
    "--user-data-dir=$profile", "--window-size=512,512", "--virtual-time-budget=5000", "--screenshot=$icon",
    ([Uri](Resolve-Path "$PSScriptRoot\icon.html").Path).AbsoluteUri)
$p.WaitForExit(60000) | Out-Null
python -c "import sys; from PIL import Image; im = Image.open(sys.argv[1]).convert('RGB').crop((0, 0, 512, 512)); im.resize((256, 256), Image.LANCZOS).save(sys.argv[1])" $icon
Write-Host "icon.png: $([math]::Round((Get-Item $icon).Length / 1KB)) KB"
