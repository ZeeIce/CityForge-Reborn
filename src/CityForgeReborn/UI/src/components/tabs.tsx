import React, { useState } from "react";
import { actions, useBool, useInt, useReadOnlyInt, useString } from "../bindings";
import { Button, ButtonRow, Card, Notice, Segmented, Slider, Toggle } from "./controls";

const formatMoney = (amount: number) => {
    const abs = Math.abs(amount);
    const text = abs >= 1_000_000 ? `${abs / 1_000_000}M` : `${abs / 1_000}K`;
    return `${amount < 0 ? "−" : "+"}${text}`;
};

const MONEY_STEPS = [100_000, 1_000_000, 10_000_000, 50_000_000];
const DEV_POINT_STEPS = [10, 50, 250];

export const EconomyTab = () => {
    const [infiniteMoney, setInfiniteMoney] = useBool("infiniteMoney");
    const [target, setTarget] = useInt("milestoneTarget", 10);
    const [keepUnlocked, setKeepUnlocked] = useBool("keepMilestonesUnlocked");
    const current = useReadOnlyInt("currentMilestone");
    const max = useReadOnlyInt("maxMilestone", 20) || 20;

    return (
        <>
            <Card title="Money" icon="¤">
                <ButtonRow>
                    {MONEY_STEPS.map((step) => (
                        <Button key={step} label={formatMoney(step)} variant="positive" onClick={() => actions.addMoney(step)} />
                    ))}
                </ButtonRow>
                <ButtonRow>
                    {MONEY_STEPS.map((step) => (
                        <Button key={step} label={formatMoney(-step)} variant="negative" onClick={() => actions.addMoney(-step)} />
                    ))}
                </ButtonRow>
                <Toggle
                    label="Infinite money"
                    hint="Keeps your balance above 1 billion"
                    value={infiniteMoney}
                    onChange={setInfiniteMoney}
                />
            </Card>

            <Card title="Milestones" icon="★">
                <div className="cf-stat">
                    Current milestone <b>{current}</b> / {max}
                </div>
                <Slider label="Target milestone" value={Math.min(target, max)} min={1} max={max} onCommit={setTarget} />
                <ButtonRow>
                    <Button
                        label={`Advance to ${Math.min(target, max)}`}
                        variant="accent"
                        disabled={current >= target}
                        onClick={actions.advanceMilestone}
                    />
                    <Button label="Unlock all" disabled={current >= max} onClick={actions.unlockAllMilestones} />
                </ButtonRow>
                <Toggle
                    label="Keep unlocked"
                    hint="Re-applies the target milestone whenever a city loads"
                    value={keepUnlocked}
                    onChange={setKeepUnlocked}
                />
            </Card>

            <Card title="Development tree" icon="✦">
                <ButtonRow>
                    {DEV_POINT_STEPS.map((points) => (
                        <Button key={points} label={`+${points} points`} onClick={() => actions.addDevTreePoints(points)} />
                    ))}
                </ButtonRow>
            </Card>
        </>
    );
};

const SPEEDS = ["½×", "1×", "2×", "4×"];

export const BuildTab = () => {
    const [instant, setInstant] = useBool("instantConstruction");
    const [speed, setSpeed] = useInt("buildSpeed", 1);
    const [levelUp, setLevelUp] = useBool("levelUpBuildings");
    const [levelTarget, setLevelTarget] = useInt("buildingLevelTarget", 5);
    const [keepFull, setKeepFull] = useBool("keepStorageFull");
    const [maxEfficiency, setMaxEfficiency] = useBool("maxEfficiency");
    const [unlimitedWater, setUnlimitedWater] = useBool("unlimitedWater");
    const [unlimitedElectricity, setUnlimitedElectricity] = useBool("unlimitedElectricity");
    const [noPollution, setNoPollution] = useBool("noPollution");

    return (
        <>
            <Card title="Construction" icon="⚒">
                <Toggle label="Instant construction" value={instant} onChange={setInstant} />
                {instant && <Notice text="Buildings finish the moment they are placed." />}
                <div className={instant ? "cf-dimmed" : ""}>
                    <div className="cf-row__hint cf-spaced">Build speed</div>
                    <Segmented options={SPEEDS} value={speed} onChange={setSpeed} />
                </div>
            </Card>

            <Card title="Buildings" icon="▦">
                <div className="cf-row__hint cf-spaced">Target level</div>
                <Segmented options={LEVELS} value={levelTarget - 1} onChange={(i) => setLevelTarget(i + 1)} />
                <Toggle
                    label={`Level up to level ${levelTarget}`}
                    hint="Zoned buildings level up one step at a time until they reach the target"
                    value={levelUp}
                    onChange={setLevelUp}
                />
                <Toggle
                    label="Max building efficiency"
                    hint="Companies and services run at full efficiency"
                    value={maxEfficiency}
                    onChange={setMaxEfficiency}
                />
            </Card>

            <Card title="City services" icon="⚡">
                <Toggle
                    label="Unlimited water"
                    hint="Every building gets full water and sewage, no pipes needed"
                    value={unlimitedWater}
                    onChange={setUnlimitedWater}
                />
                <Toggle
                    label="Unlimited electricity"
                    hint="Every building gets full power, no power plants needed"
                    value={unlimitedElectricity}
                    onChange={setUnlimitedElectricity}
                />
                <Toggle
                    label="No pollution"
                    hint="Clears air, ground, noise and groundwater pollution"
                    value={noPollution}
                    onChange={setNoPollution}
                />
            </Card>

            <Card title="Storage" icon="▤">
                <ButtonRow>
                    <Button label="Fill all storage" variant="accent" onClick={actions.fillStorage} />
                </ButtonRow>
                <Toggle label="Keep storage full" value={keepFull} onChange={setKeepFull} />
            </Card>
        </>
    );
};

const LEVELS = ["1", "2", "3", "4", "5"];

const multiplier = (v: number) => `${v}×`;

const BoostToggle = ({ label, toggleKey, multiplierKey }: { label: string; toggleKey: string; multiplierKey: string }) => {
    const [enabled, setEnabled] = useBool(toggleKey);
    const [value, setValue] = useInt(multiplierKey, 3);
    return (
        <>
            <Toggle label={label} value={enabled} onChange={setEnabled} />
            {enabled && <Slider label="Multiplier" value={value} min={2} max={10} format={multiplier} onCommit={setValue} />}
        </>
    );
};

const EDUCATION_LEVELS = [
    { key: "educationUneducated", label: "Uneducated" },
    { key: "educationPoor", label: "Poorly educated" },
    { key: "educationEducated", label: "Educated" },
    { key: "educationWell", label: "Well educated" },
    { key: "educationHigh", label: "Highly educated" },
];

export const PeopleTab = () => {
    const [maxHappiness, setMaxHappiness] = useBool("maxHappiness");
    const [rich, setRich] = useBool("richCitizens");
    const [maxEducation, setMaxEducation] = useBool("maxEducation");

    return (
        <>
            <Card title="Growth" icon="⇡">
                <BoostToggle label="Boost move-in rate" toggleKey="boostMoveIn" multiplierKey="moveInMultiplier" />
                <BoostToggle label="Boost tourists" toggleKey="boostTourists" multiplierKey="touristMultiplier" />
            </Card>

            <Card title="Citizens" icon="☺">
                <Toggle label="Max happiness" hint="Well-being and health held at maximum" value={maxHappiness} onChange={setMaxHappiness} />
                <Toggle label="Rich citizens" hint="Every household keeps at least 500,000" value={rich} onChange={setRich} />
                <Toggle label="Max education" hint="Every citizen is highly educated" value={maxEducation} onChange={setMaxEducation} />
            </Card>

            <Card title="Education distribution" icon="✎" defaultOpen={false}>
                {maxEducation && <Notice tone="warning" text="Turn off Max education to use a custom distribution." />}
                <div className={maxEducation ? "cf-dimmed" : ""}>
                    {EDUCATION_LEVELS.map((level) => (
                        <ZoneSlider key={level.key} binding={level.key} label={level.label} />
                    ))}
                    <div className="cf-row__hint cf-spaced">Values are relative weights; they don't need to add up to 100.</div>
                    <ButtonRow>
                        <Button
                            label="Apply to all citizens"
                            variant="accent"
                            disabled={maxEducation}
                            onClick={actions.applyEducationDistribution}
                        />
                    </ButtonRow>
                </div>
            </Card>
        </>
    );
};

const percent = (v: number) => `${v}%`;

const ZoneCard = ({ title, zone, levels }: { title: string; zone: string; levels: { key: string; label: string }[] }) => {
    const [enabled, setEnabled] = useBool(`${zone}Override`);
    const [force, setForce] = useBool(`${zone}Force`);

    return (
        <Card title={title} icon="▲">
            <Toggle label="Override demand" value={enabled} onChange={setEnabled} />
            <Toggle
                label="Force build"
                hint="Buildings spawn regardless of demand, land value or suitability"
                tone="warning"
                value={force}
                onChange={setForce}
            />
            {force ? (
                <Notice tone="warning" text="Force build is on: buildings keep spawning regardless of demand or lot suitability." />
            ) : (
                enabled && levels.map((level) => <ZoneSlider key={level.key} binding={level.key} label={level.label} />)
            )}
        </Card>
    );
};

const ZoneSlider = ({ binding, label }: { binding: string; label: string }) => {
    const [value, setValue] = useInt(binding, 50);
    return <Slider label={label} value={value} min={0} max={100} format={percent} onCommit={setValue} />;
};

export const DemandTab = () => (
    <>
        <ZoneCard
            title="Residential"
            zone="residential"
            levels={[
                { key: "residentialLow", label: "Low density" },
                { key: "residentialMedium", label: "Medium density" },
                { key: "residentialHigh", label: "High density" },
            ]}
        />
        <ZoneCard title="Commercial" zone="commercial" levels={[{ key: "commercialDemand", label: "Demand" }]} />
        <ZoneCard title="Industrial" zone="industrial" levels={[{ key: "industrialDemand", label: "Demand" }]} />
        <ZoneCard title="Office" zone="office" levels={[{ key: "officeDemand", label: "Demand" }]} />
    </>
);

export const DEFAULT_PANEL_COLOR = "#161b22";
const PRESETS = [DEFAULT_PANEL_COLOR, "#1e1b2e", "#13221c", "#2a1a14", "#0f1f2e", "#26222a", "#1a1a1a", "#2b2410"];
const HEX = /^#[0-9a-fA-F]{6}$/;

export const SettingsTab = () => {
    const [color, setColor] = useString("panelColor", DEFAULT_PANEL_COLOR);
    const [resetOnLoad, setResetOnLoad] = useBool("resetCheatsOnLoad");
    const [draft, setDraft] = useState<string | null>(null);
    const text = draft ?? color;

    return (
        <>
            <Card title="General" icon="⚙">
                <Toggle
                    label="Reset cheats on load"
                    hint="Turns every cheat off whenever a city is loaded"
                    value={resetOnLoad}
                    onChange={setResetOnLoad}
                />
            </Card>
            <Card title="Appearance" icon="◐">
                <div className="cf-row__hint cf-spaced">Panel colour</div>
                <div className="cf-swatches">
                    {PRESETS.map((preset) => (
                        <div
                            key={preset}
                            className={`cf-swatch${preset.toLowerCase() === color.toLowerCase() ? " cf-swatch--active" : ""}`}
                            style={{ backgroundColor: preset }}
                            onClick={() => setColor(preset)}
                        />
                    ))}
                </div>
                <div className="cf-hex">
                    <div className="cf-hex__preview" style={{ backgroundColor: HEX.test(text) ? text : color }} />
                    <input
                        className={`cf-hex__input${HEX.test(text) ? "" : " cf-hex__input--invalid"}`}
                        value={text}
                        maxLength={7}
                        onChange={(e) => {
                            const next = e.target.value;
                            setDraft(next);
                            if (HEX.test(next)) setColor(next);
                        }}
                        onBlur={() => setDraft(null)}
                    />
                    <Button label="Reset" onClick={() => setColor(DEFAULT_PANEL_COLOR)} />
                </div>
            </Card>
        </>
    );
};
