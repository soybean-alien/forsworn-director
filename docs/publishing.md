# Publishing Forsworn Director

`tools/package.ps1` builds both upload packages into `dist/`.

| Where | Status (2026-10-17) | What to upload |
|---|---|---|
| **GitHub Releases** | Live | `ForswornDirector-<ver>.zip` |
| **Nexus Mods** | No Forsworn game page yet ([request one](https://forums.nexusmods.com/forum/4004-game-requests/)); Nexus adds games on request | `ForswornDirector-<ver>.zip`, the banner as the main image, the demo GIF, and the description below |
| **Thunderstore / r2modman** | No Forsworn community yet; request one on the [Thunderstore Discord](https://discord.gg/UWpWhjZken) | `soybean_alien-ForswornDirector-<ver>.zip` (has manifest.json, icon.png, README.md, plugins/). Add the community's BepInEx IL2CPP pack to `dependencies` once it exists. |

Always lead with the warning: **single player and offline only; co-op use can get you banned.**

## Nexus Mods description (BBCode)

```
[center][img]https://raw.githubusercontent.com/soybean-alien/forsworn-director/main/docs/media/banner.png[/img][/center]

[size=5][color=#ff6a3d][b]⚠ SINGLE PLAYER | OFFLINE ONLY ⚠[/b][/color][/size]
This mod is for single-player and offline play only. [b]Do not use it in co-op.[/b] Cheating in co-op ruins the game for other players and [b]can get you banned by the developer[/b]. It may also break Steam's rules, so don't risk your account. The mod pauses itself when another player connects, but switch it off or uninstall it before co-op anyway.

[size=4][b]What it does[/b][/size]
Press [b]F8[/b] in a solo run to open the Director panel:
[list]
[*][b]Party:[/b] unlimited Gold, Soul Shards, Upgrade Tokens and talent points (999,999); Mega Battle XP x2 to x5,000; everything on/off in one click
[*][b]Every class:[/b] God Mode, Health Always Full, Endless Action Points, Endless Movement, Ultimate Always Ready
[*][b]+20 stat boosts per class:[/b] Damage, Shield Generation, AP Generation, Armor, Ultimate Generation, Movement, Thorns, Max Health
[*]A panel styled after Forsworn's own UI, with a live hero readout and a remembered position
[/list]

[size=4][b]Install[/b][/size]
[list=1]
[*]Install [url=https://builds.bepinex.dev/projects/bepinex_be]BepInEx 6 bleeding edge, Unity IL2CPP x64[/url] (build 789+) into the Forsworn folder. Start the game once, wait for the main menu, then quit.
[*]Extract this mod's zip into the Forsworn folder.
[*]Start a single-player run and press F8.
[/list]
Back up your saves first: %USERPROFILE%\AppData\LocalLow\Resummon Studios LLC\Forsworn

Source, issues and changelog: [url=https://github.com/soybean-alien/forsworn-director]github.com/soybean-alien/forsworn-director[/url]
Unofficial fan mod by soybean_alien, not affiliated with Resummon Studios. MIT License. Built with Claude Code (Anthropic) and play-tested by the author.
```

## Short post (Steam discussions, Reddit, Discord)

> **Forsworn Director: a single-player cheat panel for Forsworn (BepInEx, free, open source)**
> Press F8 in a solo run for god mode, endless AP and movement, +20 stat boosts per hero, 999,999 gold, Soul Shards and Upgrade Tokens, and up to x5,000 battle XP.
> ⚠ **Single player and offline only.** Don't use it in co-op; that can get you banned by the developer.
> Download and install steps: https://github.com/soybean-alien/forsworn-director
