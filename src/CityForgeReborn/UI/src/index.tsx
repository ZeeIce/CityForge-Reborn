import React from "react";
import { ModRegistrar } from "cs2/modding";
import { useBool } from "./bindings";
import { CityForgePanel } from "./components/panel";
import { setPanelState, usePanelState } from "./store";
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

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", ToolbarButton);
    moduleRegistry.append("Game", CityForgePanel);
};

export default register;
