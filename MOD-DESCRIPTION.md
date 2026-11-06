# Service Stars

**Give every weapon a record worth keeping.**

Service Stars brings Battlefield-inspired weapon milestones to SPT. Earn kills with a weapon, build its record, and receive a service star every **100 kills** by default.

Each promotion displays an original bronze star in the center of the screen, accompanied by the weapon's name, your new star count, a short animation, and a subtle metallic chime.

## Features

- **Weapon-specific progression:** Track firearms, grenades, melee weapons, and standalone or underbarrel launchers separately.
- **Shared progress by weapon model:** Different instances and builds of the same weapon template share their record. Dying or losing a weapon does not reset it.
- **Inspect your progress:** See total kills, earned stars, and progress toward the next star in the item's inspection window.
- **Server-side persistence:** Progress is saved separately for each player, with retry-safe synchronization and support for reconnecting after an interruption.
- **Modded weapon support:** Additional weapons that use supported EFT item types, template categories, and the standard kill-statistics flow can participate automatically.
- **Automatic localization:** Follows the game's language. English and Chinese are included; other languages fall back to English.
- **Configurable promotions:** Adjust the star threshold, notification duration, screen position, and sound volume. Preview the notification without earning a kill.

In the current release, stars are cosmetic: they do **not yet** change damage, recoil, or other weapon attributes. Optional gameplay bonuses may be explored in a future release, with a configuration setting to enable or disable them. These bonuses are not currently implemented.

Tracking begins after installation because EFT does not expose a complete, reliable historical kill record for each weapon that this mod can import. Existing totals therefore cannot be accurately reconstructed; Service Stars records new kills going forward.

## Compatibility

- Built and tested on **SPT 4.1.6 / EFT 0.16.9.5.40743**.
- Requires both the included client plugin and server mod.
- Special mods that bypass the standard kill-statistics flow or omit the originating weapon may not be tracked correctly.
- **Fika:** Player testing in co-op sessions indicates that individual progress works: each player can see their own service-star records. Viewing a teammate's service-star records is not currently supported. This testing does not establish compatibility with every Fika version or mod combination.

## Installation

1. Close the game and SPT server.
2. Extract the ZIP and place `BepInEx/plugins/ServiceStars/` in your game's `BepInEx/plugins/` directory.
3. Place the package's `SPT_Runtime/user/mods/ServiceStars/` folder in your actual SPT server's `user/mods/` directory. The server folder name may differ between installations.
4. Start the server and game.

Keep any existing custom configuration when updating. Replace the existing DLLs rather than keeping duplicate copies.

## Configuration

- **F12 → Service Stars → Promotion:** Preview the notification and adjust its duration, position, or volume. Set volume to `0` to mute the chime.
- **F12 → Service Stars → Display:** Enable or disable promotion notifications.
- **Server mod's `config.json` → `KillsPerStar`:** Change the default 100-kill milestone. Restart the server after editing this value. Existing kill totals are retained; stars are recalculated using the new threshold.

## Profile safety and removal

Service Stars does **not modify SPT's main profile files** or require the mod to remain installed for a profile to load.

Its own data is stored separately:

- **Server:** `user/profileData/ServiceStars/`, relative to the server directory.
- **Client:** `BepInEx/config/ServiceStars/`.

To uninstall, close the game and server, then remove the client and server mod folders. No main-profile repair or migration is required. Keep both separate data locations if you want to retain progress for reinstallation.

## About this release

**Version 1.2.1** is the project's minimum usable baseline. The separate career page and favorite-weapon dashboard are **not included** in this release.

**Mod GUID:** `com.shane-xue.service-stars`  
**License:** MIT  
**Source:** [Shane-Xue/spt-service-star](https://github.com/Shane-Xue/spt-service-star)

The star artwork was generated specifically for this project using imagegen. It is used as the mod's logo and promotion medal; it is not extracted from Battlefield. The notification chime is synthesized by the plugin. This project is not affiliated with EA or DICE.

## AI Disclosure

This mod was developed with AI assistance for code, documentation, and troubleshooting. The star logo and promotion artwork were generated using AI.

AI involvement does not guarantee correctness or compatibility. Please report bugs with relevant logs and reproduction steps so they can be investigated.
