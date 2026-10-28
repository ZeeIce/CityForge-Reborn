import React, { useEffect, useRef } from "react";
import { ModRegistrar } from "cs2/modding";
import { useBool, useReadOnlyInt } from "./bindings";
import { BuffBar } from "./components/buffbar";
import { CityForgePanel } from "./components/panel";
import { setPanelState, getPanelState, usePanelState } from "./store";
import "./styles.css";

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

/** Opens or closes the panel when the hotkey is pressed. */
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

/** The button, with the buff bar hanging underneath it (or in its place when the button is hidden). */
const TopLeft = () => {
    const [button] = useBool("showToolbarButton");
    return (
        <div className="cf-topleft">
            <ToolbarButton />
            <BuffBar below={button} />
        </div>
    );
};

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
