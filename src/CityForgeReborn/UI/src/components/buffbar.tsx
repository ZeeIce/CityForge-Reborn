import React from "react";
import { useBool, useInt, useString } from "../bindings";
import { CATEGORIES, Category, CHEATS, parsePins, pinIcon, SPEED } from "../cheats";
import { setPanelState } from "../store";

/** Tiles shown before the rest fold into a "+N" tile. */
const MAX_TILES = 10;

interface Tile {
    key: string;
    label: string;
    cat: Category;
    icon: string;
    on: boolean;
    pinned: boolean;
    action: string;
    toggle: () => void;
}

/**
 * Cheats as status effects, one tile each, under the CityForge button. Cheats that
 * are on always show; pinned cheats also show while off (dimmed) so one click turns
 * them on. Clicking a tile flips it.
 */
export const BuffBar = ({ below }: { below: boolean }) => {
    const [visible] = useBool("showStatusBar");
    const [pinsValue] = useString("pinnedCheats");
    const [speed, setSpeed] = useInt(SPEED.key, SPEED.normal);
    const [instant] = useBool("instantConstruction");
    const [moveIn] = useInt("moveInMultiplier", 3);
    const [tourists] = useInt("touristMultiplier", 3);
    const [level] = useInt("buildingLevelTarget", 5);
    // One hook per cheat; the list never changes, so the hook order is stable.
    const states = CHEATS.map((c) => useBool(c.key));

    if (!visible) return null;

    const pins = parsePins(pinsValue);
    const detail: Record<string, string> = {
        boostMoveIn: `Move-ins ${moveIn}×`,
        boostTourists: `Tourists ${tourists}×`,
        levelUpBuildings: `Level ${level} buildings`,
    };

    const tile = (i: number): Tile => {
        const c = CHEATS[i];
        const [on, set] = states[i];
        return { ...c, label: detail[c.key] ?? c.label, on, pinned: pins.includes(c.key), action: on ? "turn off" : "turn on", toggle: () => set(!on) };
    };

    const pinned = pins.map((key) => tile(CHEATS.findIndex((c) => c.key === key)));
    const others = CHEATS.map((_, i) => tile(i)).filter((t) => t.on && !t.pinned);
    const tiles = [...pinned, ...others];
    if (!instant && speed !== SPEED.normal)
        tiles.push({ key: SPEED.key, label: `Build speed ${SPEED.labels[speed] ?? ""}`, cat: SPEED.cat, icon: SPEED.icon, on: true, pinned: false, action: "set back to 1×", toggle: () => setSpeed(SPEED.normal) });

    if (tiles.length === 0) return null;

    const shown = tiles.length > MAX_TILES ? tiles.slice(0, MAX_TILES - 1) : tiles;
    const hidden = tiles.length - shown.length;

    return (
        <div className={`cf-buffs${below ? " cf-buffs--below" : ""}`}>
            {shown.map((t) => {
                const color = CATEGORIES[t.cat].color;
                return (
                    <div
                        key={t.key}
                        className={`cf-buff${t.on ? " cf-buff--on" : " cf-buff--off"}`}
                        style={{ borderBottomColor: t.on ? color : "rgba(255, 255, 255, 0.18)" }}
                        onClick={t.toggle}
                    >
                        <img className="cf-buff__icon" src={t.icon} />
                        {t.pinned && <img className="cf-buff__pin" src={pinIcon} />}
                        <div className="cf-buff__tip">
                            <div className="cf-buff__tip-title">{t.label}</div>
                            <div className="cf-buff__tip-sub" style={{ color }}>
                                {`${CATEGORIES[t.cat].label}${t.pinned ? " · pinned" : ""} · click to ${t.action}`}
                            </div>
                        </div>
                    </div>
                );
            })}
            {hidden > 0 && (
                <div className="cf-buff cf-buff--more" onClick={() => setPanelState({ open: true })}>
                    {`+${hidden}`}
                    <div className="cf-buff__tip">
                        <div className="cf-buff__tip-title">{`${hidden} more`}</div>
                        <div className="cf-buff__tip-sub">Click to open the panel</div>
                    </div>
                </div>
            )}
        </div>
    );
};
