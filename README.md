# CityForge Reborn

A cheat and sandbox panel for **Cities: Skylines II**.
Click the **◆** button in the top-left corner of the game to open it.

<img src="docs/economy.png" width="400" alt="Economy tab"> <img src="docs/build.png" width="400" alt="Build tab">

*Panel preview with sample numbers.*

## What it does

**Economy**
- Add or remove money, or turn on infinite money
- Jump to any milestone, or unlock them all
- Get development tree points

**Build**
- Instant construction, or build at ½× to 4× speed
- Level up buildings, max out efficiency
- Fill all storage, or keep it full

**People**
- Boost move-ins and tourists (up to 10×)
- Max happiness, rich citizens, max education
- Set your own education mix

**Demand**
- Set demand for each zone yourself
- **Force Build**: buildings spawn even with no demand or a poor lot

**Settings**
- Change the panel colour
- Turn all cheats off when you load a city
- Keep achievements on while using mods (in the game's Options menu)

## Install

**Paradox Mods:** subscribe in the game's mod menu.

**Manual:** download the zip from [Releases](../../releases) and unzip it into:
```
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\
```
You should end up with a `Mods\CityForgeReborn` folder.

## Problems?

Open an [issue](../../issues) and attach this log file:
```
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\CityForgeReborn.Mod.log
```

## Coming later

- Translations
- Time of day and weather control
- Unlimited water and power, no pollution

## Build it yourself

1. In the game, go to *Options → Modding → Install*, then restart your PC.
2. Run:
   ```
   dotnet build src/CityForgeReborn/CityForgeReborn.csproj -c Release
   ```
   This builds the mod and copies it into your Mods folder.

If the build says *"You must install or update .NET"*, install the
[.NET 6 runtime](https://dotnet.microsoft.com/download/dotnet/6.0). The game's tools need it.

Changes are listed in [CHANGELOG.md](CHANGELOG.md).

## License

GPL-3.0. Written from scratch; no code from the original CityForge mod.
