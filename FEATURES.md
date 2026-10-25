# CityForge Reborn — Feature Spec

An independent, from-scratch cheat/dev panel for Cities: Skylines II.

This list describes **what** the mod should do, compiled from the original
CityForge's public README, changelog and screenshots (user-facing behaviour
only). No source code from the original mod was used; all implementation in
this repository is written independently.

---

## Panel & UI

- Toolbar button (top-left) that opens/closes the panel
- Floating panel with title bar and close button
- Draggable by the title bar, resizable from the corner
- Every section is collapsible by clicking its header
- Status banners under active toggles (e.g. "Instant build active")
- Panel must never block mouse input to the game's own UI after use
- Two-column layout for smaller sections, full width for Money / Demand

## Mod Settings (game Options menu)

- Show toolbar button (on/off)
- Keep achievements enabled while mods are active (locked while a city is loaded)

## Sections

### 💰 Money
- Add fixed amounts: +100K, +1M, +10M, +50M
- Remove fixed amounts: −100K, −1M, −10M, −50M
- Infinite Money toggle

### 🏆 Milestones
- Target slider (pick a milestone level)
- "Advance to N" button — XP bar must update correctly afterwards
- "Unlock All" button
- Keep permanently unlocked toggle

### 🌳 Development Tree
- Grant development points in one click (original granted 228 = enough for the full tree)

### 🏗️ Construction Time
- Instant Construction toggle
- Build speed selector: ½×, 1×, 2×, 4×

### 👥 Population & Tourism
- Boost Move-In Rate toggle + multiplier slider (up to 10×)
- Boost Tourists toggle + multiplier slider (up to 10×)
- Max Citizen Happiness toggle
- Rich Citizens toggle (periodically gives every household a large sum of money)
- Max Education toggle (all citizens at highest education level)
- Education Distribution: 5 percentage sliders
  (Uneducated, Poorly Educated, Educated, Well Educated, Highly Educated)

### 🏢 Buildings
- Level selector (1–5) + "Upgrade All to Level N" button
  - Must be processed in batches across frames (no freeze, safe at max sim speed)
- Fill Storage button
- Keep storage permanently full toggle
- Max Company Efficiency toggle
- Reset cheats on new map toggle (disable all cheats when another city is loaded)

### 📈 Demand
Per zone type, each with an override toggle and a Force Build toggle:
- Residential — separate Low / Medium / High sliders (0–100%)
- Commercial — slider (0–100%)
- Industrial — slider (0–100%)
- Office — slider (0–100%)
- Force Build = buildings spawn regardless of demand
  - Must also bypass low-density residential "suitability" checks
  - Must work on its own without the demand override being on
  - Industrial/Office overrides must feed the value the spawn system actually reads

### 🎨 Appearance
- Row of colour presets for the panel background + Reset
- Custom hex colour input

## General Requirements

- All settings save automatically and restore between sessions
- Settings saving should retry if another mod is saving at the same time
- Heavy per-entity work (happiness, efficiency, rich citizens, construction)
  should run as background jobs so it doesn't stutter the game at 3× speed
- Localisation-ready (original shipped 12 languages:
  EN, DE, FR, ES, IT, PT, PL, RU, JA, KO, ZH-Hans, ZH-Hant)

## Possible Extras (not in the original)

Ideas we could add later: time-of-day/weather control, unlimited
water/electricity, disable pollution, instantly clear garbage/crime, etc.
