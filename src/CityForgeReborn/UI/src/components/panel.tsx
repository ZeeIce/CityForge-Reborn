import React from "react";
import { useString } from "../bindings";
import { getPanelState, setPanelState, startDrag, usePanelState } from "../store";
import { BuildTab, DEFAULT_PANEL_COLOR, DemandTab, EconomyTab, StyleTab } from "./tabs";

const TABS = [
    { id: "economy", label: "Economy", content: EconomyTab },
    { id: "build", label: "Build", content: BuildTab },
    { id: "demand", label: "Demand", content: DemandTab },
    { id: "style", label: "Style", content: StyleTab },
];

const MIN_WIDTH = 380;
const MIN_HEIGHT = 300;

export const CityForgePanel = () => {
    const panel = usePanelState();
    const [color] = useString("panelColor", DEFAULT_PANEL_COLOR);

    if (!panel.open) return null;

    const onMoveStart = (e: React.MouseEvent) => {
        const { x, y } = getPanelState();
        startDrag(e, (dx, dy) => setPanelState({ x: Math.max(0, x + dx), y: Math.max(0, y + dy) }));
    };

    const onResizeStart = (e: React.MouseEvent) => {
        const { width, height } = getPanelState();
        startDrag(e, (dx, dy) =>
            setPanelState({ width: Math.max(MIN_WIDTH, width + dx), height: Math.max(MIN_HEIGHT, height + dy) }),
        );
    };

    const Active = (TABS.find((tab) => tab.id === panel.tab) ?? TABS[0]).content;

    return (
        <div
            className="cf-panel"
            style={{ left: panel.x, top: panel.y, width: panel.width, height: panel.height, backgroundColor: color }}
        >
            <div className="cf-panel__header" onMouseDown={onMoveStart}>
                <div className="cf-panel__brand">
                    <span className="cf-panel__logo">◆</span>
                    <span className="cf-panel__title">CityForge</span>
                    <span className="cf-panel__subtitle">Reborn</span>
                </div>
                <div
                    className="cf-panel__close"
                    onMouseDown={(e) => e.stopPropagation()}
                    onClick={() => setPanelState({ open: false })}
                >
                    ✕
                </div>
            </div>

            <div className="cf-tabs">
                {TABS.map((tab) => (
                    <div
                        key={tab.id}
                        className={`cf-tab${tab.id === panel.tab ? " cf-tab--active" : ""}`}
                        onClick={() => setPanelState({ tab: tab.id })}
                    >
                        {tab.label}
                    </div>
                ))}
            </div>

            <div className="cf-panel__content">
                <Active />
            </div>

            <div className="cf-panel__resize" onMouseDown={onResizeStart} />
        </div>
    );
};
