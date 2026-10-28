import React, { useEffect, useState } from "react";
import { actions, useBool, useInt, useReadOnlyInt, useReadOnlyString, useString } from "../bindings";
import { CATEGORIES, Category, CHEATS, parsePins, pinIcon } from "../cheats";
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

            <Card title="Unemployed exodus" icon="⇡" defaultOpen={false}>
                <Exodus />
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

const EXODUS_MODES = ["Remove instantly", "Move away"];

/**
 * Sends away households where nobody works or studies. Mounted only while its card is
 * open, so the city is only scanned when someone is looking.
 */
const Exodus = () => {
    const [mask, setMask] = useInt("exodusEducation", 1);
    const [moveAway, setMoveAway] = useBool("exodusMoveAway");
    const households = useReadOnlyInt("exodusHouseholds", -1);
    const residents = useReadOnlyInt("exodusCitizens");
    const result = useReadOnlyString("exodusResult");
    const [armed, setArmed] = useState(false);

    useEffect(() => actions.refreshExodus(), []);
    useEffect(() => {
        if (!armed) return;
        const timer = setTimeout(() => setArmed(false), 4000);
        return () => clearTimeout(timer);
    }, [armed]);

    const toggleLevel = (i: number) => {
        setArmed(false);
        setMask(mask ^ (1 << i));
    };
    const verb = moveAway ? "Send away" : "Remove";

    return (
        <>
            <div className="cf-row__hint">
                Households where nobody has a job or a school place, and every adult has one of the education levels below.
                Whole households leave together; children go with their parents, and retirees are never touched.
            </div>
            <div className="cf-chips cf-spaced">
                {EDUCATION_LEVELS.map((level, i) => (
                    <div key={level.key} className={`cf-chip${mask & (1 << i) ? " cf-chip--on" : ""}`} onClick={() => toggleLevel(i)}>
                        {level.label}
                    </div>
                ))}
            </div>
            <Segmented options={EXODUS_MODES} value={moveAway ? 1 : 0} onChange={(i) => { setArmed(false); setMoveAway(i === 1); }} />
            <div className="cf-row__hint cf-spaced">
                {moveAway
                    ? "They pack up and leave the normal way, by car, bus, train or plane. Expect extra traffic for a while."
                    : "They vanish on the spot: homes, school seats, cars and pets are freed at once."}
            </div>
            <div className="cf-exodus__count">
                {households < 0 ? "Counting…" : mask === 0 ? "Pick at least one education level." : `${households.toLocaleString()} households · ${residents.toLocaleString()} residents match`}
            </div>
            <ButtonRow>
                <Button
                    label={armed ? `Click again to ${verb.toLowerCase()} ${households.toLocaleString()} households` : `${verb} matching households`}
                    variant={armed ? "negative" : "accent"}
                    disabled={households <= 0 || mask === 0}
                    onClick={() => {
                        if (!armed) return setArmed(true);
                        setArmed(false);
                        actions.runExodus();
                    }}
                />
                <Button label="Recount" onClick={actions.refreshExodus} />
            </ButtonRow>
            {result && <Notice text={result} />}
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
    const [statusBar, setStatusBar] = useBool("showStatusBar");
    const [pinsValue, setPins] = useString("pinnedCheats");
    const hotkey = useReadOnlyString("hotkeyLabel");
    const [listening] = useBool("hotkeyListening");
    const pins = parsePins(pinsValue);
    const togglePin = (key: string) => setPins((pins.includes(key) ? pins.filter((k) => k !== key) : [...pins, key]).join(","));
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
            <Card title="Hotkey" icon="✦">
                <div className="cf-row__hint">Opens or closes this panel while you are in a city.</div>
                <div className="cf-hotkey">
                    <div className={`cf-hotkey__key${listening ? " cf-hotkey__key--listening" : ""}`}>
                        {listening ? "Press a key… (Esc cancels)" : hotkey || "Not set"}
                    </div>
                    {listening ? (
                        <Button label="Cancel" onClick={actions.cancelHotkey} />
                    ) : (
                        <>
                            <Button label="Change" variant="accent" onClick={actions.captureHotkey} />
                            <Button label="Reset" onClick={actions.resetHotkey} />
                        </>
                    )}
                </div>
                <div className="cf-row__hint cf-spaced">Hold Ctrl, Shift or Alt while pressing the key to include them.</div>
            </Card>
            <Card title="Buff bar" icon="▦">
                <Toggle
                    label="Show buff bar"
                    hint="Icons under the CityForge button for every cheat that is on. Click one to switch it."
                    value={statusBar}
                    onChange={setStatusBar}
                />
                <div className="cf-row__hint cf-spaced">Pinned cheats stay on the bar even while off, for one-click toggling.</div>
                {(Object.keys(CATEGORIES) as Category[]).map((cat) => (
                    <React.Fragment key={cat}>
                        <div className="cf-pin-group" style={{ color: CATEGORIES[cat].color }}>{CATEGORIES[cat].label}</div>
                        {CHEATS.filter((c) => c.cat === cat).map((c) => {
                            const pinned = pins.includes(c.key);
                            return (
                                <div key={c.key} className={`cf-pin-row${pinned ? " cf-pin-row--on" : ""}`} onClick={() => togglePin(c.key)}>
                                    <img className="cf-pin-row__icon" src={c.icon} />
                                    <span className="cf-pin-row__label">{c.label}</span>
                                    <img className="cf-pin-row__mark" src={pinIcon} />
                                </div>
                            );
                        })}
                    </React.Fragment>
                ))}
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
