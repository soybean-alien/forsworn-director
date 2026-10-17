# Forsworn Director by soybean_alien. MIT License.
# Builds the release and writes upload-ready packages to dist/:
#   ForswornDirector-<ver>.zip               GitHub Releases / Nexus Mods (extract into the Forsworn folder)
#   soybean_alien-ForswornDirector-<ver>.zip  Thunderstore / r2modman layout (manifest.json, icon.png, plugins/)
# Usage: powershell -ExecutionPolicy Bypass -File tools/package.ps1 [-BepInExDir <Forsworn>\BepInEx]
param([string]$BepInExDir)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$utf8 = New-Object System.Text.UTF8Encoding $false   # no BOM: Thunderstore rejects a BOM in manifest.json

# Windows PowerShell's Compress-Archive writes backslash paths, which mod managers mis-read; zip with forward slashes.
function Write-Zip([string]$source, [string]$zip) {
    $archive = [System.IO.Compression.ZipFile]::Open($zip, "Create")
    try {
        foreach ($file in Get-ChildItem $source -Recurse -File) {
            $name = $file.FullName.Substring($source.Length + 1).Replace("\", "/")
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $name, "Optimal") | Out-Null
        }
    } finally { $archive.Dispose() }
}
$root = Split-Path -Parent $PSScriptRoot
$version = [regex]::Match((Get-Content "$root\Plugin.cs" -Raw), 'PluginVersion = "([^"]+)"').Groups[1].Value
$repo = "https://github.com/soybean-alien/forsworn-director"
$raw = "https://raw.githubusercontent.com/soybean-alien/forsworn-director/main"

$buildArgs = @("build", "$root\ForswornDirector.csproj", "-c", "Release", "-o", "$root\build\out", "-clp:ErrorsOnly")
if ($BepInExDir) { $buildArgs += "-p:BepInExDir=$BepInExDir" }
& dotnet @buildArgs
if ($LASTEXITCODE) { throw "build failed" }

$dist = "$root\dist"
Remove-Item -Recurse -Force $dist -ErrorAction SilentlyContinue
$stage = "$dist\stage"

# 1. Manual install zip: mirrors the game folder.
$manual = "$stage\manual"
New-Item -ItemType Directory -Force "$manual\BepInEx\plugins\ForswornDirector" | Out-Null
Copy-Item "$root\build\out\ForswornDirector.dll" "$manual\BepInEx\plugins\ForswornDirector\"
Copy-Item "$root\LICENSE" "$manual\BepInEx\plugins\ForswornDirector\LICENSE.txt"
@"
Forsworn Director $version by soybean_alien
$repo

SINGLE PLAYER | OFFLINE ONLY. Do not use in co-op: cheating in multiplayer can get you banned by the developer
and may break Steam's rules. Switch the Director off or uninstall it before joining or hosting co-op.

Install
1. Install BepInEx 6 bleeding edge, Unity IL2CPP x64 (build 789+) from https://builds.bepinex.dev/projects/bepinex_be
   into the Forsworn folder, start the game once, wait for the main menu, then quit.
2. Extract this zip into the Forsworn folder (the one with Forsworn.exe).
   You should get BepInEx\plugins\ForswornDirector\ForswornDirector.dll
3. Start a single-player run and press F8.

Settings: BepInEx\config\soybean_alien.forsworn.director.cfg
Uninstall: delete BepInEx\plugins\ForswornDirector
Back up your saves first: %USERPROFILE%\AppData\LocalLow\Resummon Studios LLC\Forsworn

Unofficial fan mod, not affiliated with Resummon Studios. MIT License.
"@ | ForEach-Object { [IO.File]::WriteAllText("$manual\BepInEx\plugins\ForswornDirector\README.txt", $_, $utf8) }
Write-Zip $manual "$dist\ForswornDirector-$version.zip"

# 2. Thunderstore package (r2modman installs plugins/ into BepInEx\plugins\<package>).
$ts = "$stage\thunderstore"
New-Item -ItemType Directory -Force "$ts\plugins" | Out-Null
Copy-Item "$root\build\out\ForswornDirector.dll" "$ts\plugins\"
Copy-Item "$root\docs\media\icon.png" "$ts\icon.png"
Copy-Item "$root\CHANGELOG.md" "$ts\CHANGELOG.md"
[ordered]@{
    name = "ForswornDirector"; version_number = $version; website_url = $repo
    description = "SINGLE PLAYER / OFFLINE ONLY cheat panel (F8): god mode, endless AP, +20 stat boosts, 999,999 gold/shards/tokens, x5000 XP."
    dependencies = @()
} | ConvertTo-Json | ForEach-Object { [IO.File]::WriteAllText("$ts\manifest.json", $_, $utf8) }
# Thunderstore renders README.md itself; relative images must become absolute URLs.
(Get-Content "$root\README.md" -Raw).Replace("](docs/", "]($raw/docs/").Replace("](LICENSE)", "]($repo/blob/main/LICENSE)").Replace("](../../releases)", "]($repo/releases)") |
    ForEach-Object { [IO.File]::WriteAllText("$ts\README.md", $_, $utf8) }
Write-Zip $ts "$dist\soybean_alien-ForswornDirector-$version.zip"

Remove-Item -Recurse -Force $stage
Get-ChildItem $dist | ForEach-Object { "{0}  {1:N0} KB  {2}" -f $_.Name, ($_.Length / 1KB), (Get-FileHash $_.FullName).Hash }
