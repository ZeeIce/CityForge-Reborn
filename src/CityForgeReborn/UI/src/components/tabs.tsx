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

    return (
        <Card title="Construction" icon="⚒">
            <Toggle label="Instant construction" value={instant} onChange={setInstant} />
            {instant && <Notice text="Buildings finish the moment they are placed." />}
            <div className={instant ? "cf-dimmed" : ""}>
                <div className="cf-row__hint cf-spaced">Build speed</div>
                <Segmented options={SPEEDS} value={speed} onChange={setSpeed} />
            </div>
        </Card>
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
                hint="Buildings spawn regardless of demand"
                tone="warning"
                value={force}
                onChange={setForce}
            />
            {force ? (
                <Notice tone="warning" text="Force build is on: demand is held at 100%." />
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

export const StyleTab = () => {
    const [color, setColor] = useString("panelColor", DEFAULT_PANEL_COLOR);
    const [draft, setDraft] = useState<string | null>(null);
    const text = draft ?? color;

    return (
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
    );
};
