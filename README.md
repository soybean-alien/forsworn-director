# Forsworn Director

An offline/single-player BepInEx IL2CPP plugin for Forsworn. Press **F8** to open the control panel; use **F7/F9** to move between its global settings and individual class pages. Individual cheats default to off. The plugin pauses all effects unless FishNet reports an offline game or a host with no remote clients.

## Controls

- For every class name exposed by the game: god mode, health at class maximum, action points at class maximum, movement at class maximum, and ultimate charge at class maximum.
- Damage and armor bonus modifiers have no game-enforced maximum in the inspected build, so the mod does not invent one for those stats.
- Unlimited local Gold and Soul Shards. Each local balance is topped up to 999,999 while its toggle is on.
- Unlimited talent points. Talent node rank limits still come from the game.
- Mega XP: multiplies XP when the game's progression method awards it. The config allows 2x–5,000x. For this installation, the multiplier is set to 5,000x. XP is not generated while idle, and the game's level cap remains in effect.
- The inspected gameplay code has no bag-capacity or carry-weight limit to remove. Soul Shards are local currency; no separate premium wallet was found. The mod does not alter Steam inventory, Steam wallet, or real-money transactions.

## Install

1. Use a working BepInEx 6 IL2CPP x64 installation for the game's Unity 2022.3.62f3 build.
2. Copy `release/ForswornDirector.dll` to `Forsworn/BepInEx/plugins/`.
3. Start an offline single-player game and press **F8**. Settings are saved in `BepInEx/config/net.codex.forsworn.director.cfg`.

**Runtime status:** BepInEx 6.0.0-be.789 previously loaded the plugin with seven class profiles. Version 0.1.2 includes the paged controls, solo-host detection fix, and a 5,000x battle-earned XP multiplier cap. It is installed, and this game's config has Mega Battle XP enabled with `BattleXPMultiplier = 5000`. The change takes effect on the next launch; runtime XP gain has not yet been checked.

## Build from source

`ForswornDirector.csproj` references the BepInEx core and generated game interop assemblies in the sibling `_modlab/game-copy/BepInEx` folder. Build after that folder contains valid `interop` assemblies:

```powershell
dotnet build ForswornDirector.csproj -c Release -o release
```
