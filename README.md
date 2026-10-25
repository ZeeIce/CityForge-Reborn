# CityForge Reborn

An all-in-one cheat / sandbox panel for **Cities: Skylines II**, written from scratch.

<img src="docs/economy.png" width="420" alt="Economy tab"> <img src="docs/demand.png" width="420" alt="Demand tab">

*(Browser preview with mocked game data.)*

## Status — v0.1.0 (first build, untested in-game)

| Feature | State |
| --- | --- |
| Panel: toolbar button, drag, resize, tabs, collapsible cards | ✅ |
| Money: ±100K / 1M / 10M / 50M, infinite money | ✅ |
| Milestones: advance to target, unlock all, keep unlocked | ✅ |
| Development tree points | ✅ |
| Instant construction, build speed ½× / 1× / 2× / 4× | ✅ |
| Demand overrides per zone + Force Build | ✅ |
| Panel colour presets + custom hex | ✅ |
| Population & tourism, buildings, storage, achievements | ⏳ next |
| Translations | ⏳ later |

See [FEATURES.md](FEATURES.md) for the full planned feature list.

## Building

You need a Windows PC with Cities: Skylines II.

1. **Install the modding toolchain** — in the game: *Options → Modding → Install*.
   This installs .NET, Node.js and sets the `CSII_*` environment variables. Restart your PC afterwards.
2. **Build:**
   ```
   dotnet build src/CityForgeReborn/CityForgeReborn.csproj -c Release
   ```
   or open `src/CityForgeReborn/CityForgeReborn.csproj` in Visual Studio 2022 and press *Build*.

   This compiles the C# mod **and** the in-game panel, and copies both into
   `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\CityForgeReborn`.
3. Start the game, enable the mod, load a city and click the **◆** button in the top-left corner.

Logs are written to `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\CityForgeReborn.Mod.log`.

### UI only

```
cd src/CityForgeReborn/UI
npm install
npm run build      # or: npm run dev  (rebuilds on save)
```

## Project layout

```
src/CityForgeReborn/
  Mod.cs                      entry point, registers systems
  Setting.cs                  options menu + saved cheat state
  Systems/
    CityForgeUISystem.cs      bindings between the panel and the game
    MoneyCheatSystem.cs
    MilestoneCheatSystem.cs   milestones + development points
    ConstructionCheatSystem.cs
    DemandOverrideSystem.cs
  UI/                         React/TypeScript panel
```

## Notes for testing

This first build hasn't been compiled against the game yet. If something doesn't
compile, the most likely culprits are game field names that need checking with a
decompiler (ILSpy/dnSpy on `Game.dll`):

- `MilestoneData.m_XpRequried`, `XP.m_XP`, `MilestoneLevel.m_AchievedMilestone`, `DevTreePoints.m_Points`
- `UnderConstruction.m_Progress`
- Demand: `DemandOverrideSystem` finds the demand systems' fields by name at runtime and
  logs which ones it found — check the log if demand overrides don't seem to work.

## License

GPL-3.0 — see [LICENSE](LICENSE). This is an independent implementation; it contains no code from the original CityForge mod.
