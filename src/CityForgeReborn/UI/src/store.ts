import { useEffect, useState } from "react";

/**
 * Tiny shared store for UI-only state (panel open/closed, position, size).
 * The toolbar button and the panel are registered separately, so they can't
 * share React state directly.
 */
export interface PanelState {
    open: boolean;
    x: number;
    y: number;
    width: number;
    height: number;
    tab: string;
}

let state: PanelState = { open: false, x: 120, y: 110, width: 520, height: 640, tab: "economy" };
const listeners = new Set<() => void>();

export function getPanelState(): PanelState {
    return state;
}

export function setPanelState(patch: Partial<PanelState>) {
    state = { ...state, ...patch };
    listeners.forEach((listener) => listener());
}

export function usePanelState(): PanelState {
    const [, force] = useState(0);
    useEffect(() => {
        const listener = () => force((n) => n + 1);
        listeners.add(listener);
        return () => {
            listeners.delete(listener);
        };
    }, []);
    return state;
}

/**
 * Tracks a mouse drag on window and always cleans up on release, so the game
 * UI never gets stuck without mouse input after dragging.
 */
export function startDrag(
    event: { clientX: number; clientY: number; preventDefault(): void; stopPropagation(): void },
    onMove: (dx: number, dy: number) => void,
    onEnd?: () => void,
) {
    event.preventDefault();
    event.stopPropagation();
    const startX = event.clientX;
    const startY = event.clientY;

    const move = (e: MouseEvent) => onMove(e.clientX - startX, e.clientY - startY);
    const up = () => {
        window.removeEventListener("mousemove", move);
        window.removeEventListener("mouseup", up);
        onEnd?.();
    };

    window.addEventListener("mousemove", move);
    window.addEventListener("mouseup", up);
}
