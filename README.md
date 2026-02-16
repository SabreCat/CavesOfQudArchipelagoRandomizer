# Features
* Locations to check:
  * By gaining XP. The amount of checks per level can be configured.
  * Main quest and early game side quest steps
  * Deliver items to Argyve or the Grit Gate Intercom (talk with them to see a list).
  * Finding guaranteed-spawn lore items, such as sheaves of variously damaged parchment.
* Items to receive:
  * Level up gains (HP, mutation points, skill points, attribute points / bonus). These are
    considered the main progress mechanic because they mainly limit what a player can do and where
    they can go.
  * Some steps of the main quest need an item for being unlocked
  * A ton of filler items to receive into the inventory, picked randomly from the games population
    tables. This can contain garbage as well as high tier weapons or other stuff.
  * Grenade traps: Spawns a couple of grenades/bombs around the player with random timers
  * Creature traps: Spawns a party of creatures around the player (they might not be hostile
    depending on the standing).
* Settings:
  * Goal: One of the early main quests. This also determines the max level base after that no more
    checks are send (and no more stat ups gained).
  * LocationsPerLevel: How many locations shall there be per player level. Increase this to get more
    filler items and traps.
  * ExtraLocationLevels: Additional levels beyond the recommended level for starting the main quests
    included in checks (and received stat ups)
  * TrapPercentage: The percentage of filler items replaced by traps
* Ingame options:
  * Show popups or not when receiving items or traps
  * Allow traps to be triggered in settlements or not
  * Let traps trigger again or not after loading an earlier game state or starting a new character
  * DeathLink (currently only as ingame option, can be changed any time)
  * Show AP debug output or not

# Current state and known problems

* Spheres of progression are mainly bound to the players level and the main quest progress. It
  should work fairly well know, you shouldn't encounter required early items on late checks. But it
  is still to be tested and also still WIP.
* Delivery quests are an arbitrary set of items (mostly injectors), these are WIP.
* Traps can be disastrous to the game when triggering at the wrong moment (e.g. when hitting
  important npcs). There is an in game option to delay received traps
  until leaving a settlement - but that might still mess things up.
* Traps are completely random regarding progression, so you might encounter a very dangerous enemy
  very early (but I think it adds to the fun if it happens occasionally).
* Certain gained stats might not be increased correctly on gaining other stats (e.g. gaining an
  intelligence boost might not increase the total skill points)

In general, besides being incomplete and WIP, I think it is very playable now.

# Installation and creating a game
* Download the mod zip file and the APWorld from the release page
* Extract the mod to your caves of qud Mods folder found under
  `%USERPROFILE%\AppData\LocalLow\Freehold Games\CavesOfQud\Mods` (on Windows). The mod shoud have its
  own directory under `Mods`, e.g. `Mods/CavesOfQudArchipelagoRandomizer`. IMPORTANT: Do not rename
  the mod folder - this breaks the mod.
* Install the APWorld file as usual and either generate the template files using the Archipelago
  launcher. This also generates the yaml config file.
* Start Caves of Qud and enable or approve the mod under "Mods", then start a new game.
* It's recommended to 1) play roleplay mode, so you don't need to start over when dying, and 2)
  choose a mutant. And only Joppa start is currently supported.
* When creating a new character, the connection is opened after the character has been created. If
  the connection fails, the game will hang. This does not happen when loading an already saved game
  later on.
* When reloading a previous checkpoint / save or loading a saved game, it will NOT prompt for
  connection info again. This only happens if the connection fails.
