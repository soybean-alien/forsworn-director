# Changelog

## 1.0.0 (2026-10-17)
First public release.
- **F8 panel** styled after Forsworn's own UI. It has a Party page plus one page per class, a live hero readout, a draggable title bar, and a remembered position.
- **Party options:** unlimited Gold, Soul Shards, Upgrade Tokens and talent points; Mega Battle XP (x2 to x5,000); party-wide on/off.
- **Per class:** God Mode, Health Always Full, Endless Action Points, Endless Movement, Ultimate Always Ready, and eight separate +20 stat boosts (Damage, Shield Generation, AP Generation, Armor, Ultimate Generation, Movement, Thorns, Max Health).
- **Solo/offline guard:** every effect pauses while another player is connected.
- **Clicks on the panel stay on the panel.** They never move the party, pick combat targets, or paint markings on the act map.
- **Fixed** a crash during act transitions: stat hooks no longer read hero data while scenes load.
- **Fixed** sluggish battles: the session check is cached per frame and hero scans are shared.
