import React, { useEffect, useRef } from "react";
import { ModRegistrar } from "cs2/modding";
import { useBool, useReadOnlyInt, useReadOnlyString } from "./bindings";
import { CityForgePanel } from "./components/panel";
import { setPanelState, getPanelState, usePanelState } from "./store";
import "./styles.css";

/** How many cheat names the status bar spells out before switching to "+N more". */
const STATUS_SHOWN = 3;

const ToolbarButton = () => {
    const [visible] = useBool("showToolbarButton");
    const panel = usePanelState();
    if (!visible) return null;

    return (
        <div
            className={`cf-toolbar-button${panel.open ? " cf-toolbar-button--active" : ""}`}
            title="CityForge"
            onClick={() => setPanelState({ open: !panel.open })}
        >
            ◆
        </div>
    );
};

/** A strip beside the button naming the cheats that are on. Click it to open the panel. */
const StatusBar = () => {
    const [visible] = useBool("showStatusBar");
    const active = useReadOnlyString("activeCheats");
    if (!visible || !active) return null;

    const cheats = active.split("|");
    const shown = cheats.slice(0, STATUS_SHOWN).join(" · ");
    const more = cheats.length - STATUS_SHOWN;

    return (
        <div className="cf-status" title={cheats.join("\n")} onClick={() => setPanelState({ open: true })}>
            <span className="cf-status__count">{`${cheats.length} on`}</span>
            <span className="cf-status__list">{more > 0 ? `${shown} · +${more} more` : shown}</span>
        </div>
    );
};

/** Opens or closes the panel when the hotkey (set in Options) is pressed. */
const Hotkey = () => {
    const presses = useReadOnlyInt("togglePanel");
    const last = useRef(presses);
    useEffect(() => {
        if (presses === last.current) return;
        last.current = presses;
        setPanelState({ open: !getPanelState().open });
    }, [presses]);
    return null;
};

const TopLeft = () => (
    <>
        <ToolbarButton />
        <StatusBar />
    </>
);

const Game = () => (
    <>
        <Hotkey />
        <CityForgePanel />
    </>
);

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", TopLeft);
    moduleRegistry.append("Game", Game);
};

export default register;
