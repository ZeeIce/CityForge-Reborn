import React, { useRef, useState } from "react";
import { startDrag } from "../store";

type Variant = "default" | "positive" | "negative" | "accent";

export const Button = ({
    label,
    onClick,
    variant = "default",
    disabled,
}: {
    label: string;
    onClick: () => void;
    variant?: Variant;
    disabled?: boolean;
}) => (
    <button
        className={`cf-btn cf-btn--${variant}${disabled ? " cf-btn--disabled" : ""}`}
        onClick={() => !disabled && onClick()}
    >
        {label}
    </button>
);

export const Toggle = ({
    label,
    hint,
    value,
    onChange,
    tone = "accent",
}: {
    label: string;
    hint?: string;
    value: boolean;
    onChange: (value: boolean) => void;
    tone?: "accent" | "warning";
}) => (
    <div className="cf-row cf-toggle-row" onClick={() => onChange(!value)}>
        <div className="cf-row__text">
            <div className="cf-row__label">{label}</div>
            {hint && <div className="cf-row__hint">{hint}</div>}
        </div>
        <div className={`cf-switch${value ? ` cf-switch--on cf-switch--${tone}` : ""}`}>
            <div className="cf-switch__knob" />
        </div>
    </div>
);

/**
 * Drag slider. Shows the live value while dragging but only commits on release,
 * so dragging doesn't spam the game with save requests.
 */
export const Slider = ({
    label,
    value,
    min,
    max,
    format = (v) => `${v}`,
    onCommit,
}: {
    label: string;
    value: number;
    min: number;
    max: number;
    format?: (value: number) => string;
    onCommit: (value: number) => void;
}) => {
    const track = useRef<HTMLDivElement>(null);
    const [dragValue, setDragValue] = useState<number | null>(null);
    const shown = dragValue ?? value;
    const ratio = max > min ? (shown - min) / (max - min) : 0;

    const valueAt = (clientX: number) => {
        const rect = track.current!.getBoundingClientRect();
        const t = Math.min(1, Math.max(0, (clientX - rect.left) / rect.width));
        return Math.round(min + t * (max - min));
    };

    const onMouseDown = (e: React.MouseEvent) => {
        let current = valueAt(e.clientX);
        setDragValue(current);
        startDrag(
            e,
            (dx) => {
                current = valueAt(e.clientX + dx);
                setDragValue(current);
            },
            () => {
                setDragValue(null);
                onCommit(current);
            },
        );
    };

    return (
        <div className="cf-slider">
            <div className="cf-slider__header">
                <span className="cf-row__hint">{label}</span>
                <span className="cf-slider__value">{format(shown)}</span>
            </div>
            <div className="cf-slider__track" ref={track} onMouseDown={onMouseDown}>
                <div className="cf-slider__fill" style={{ width: `${ratio * 100}%` }} />
                <div className="cf-slider__thumb" style={{ left: `${ratio * 100}%` }} />
            </div>
        </div>
    );
};

export const Segmented = ({
    options,
    value,
    onChange,
}: {
    options: string[];
    value: number;
    onChange: (index: number) => void;
}) => (
    <div className="cf-segmented">
        {options.map((option, i) => (
            <div
                key={option}
                className={`cf-segmented__item${i === value ? " cf-segmented__item--active" : ""}`}
                onClick={() => onChange(i)}
            >
                {option}
            </div>
        ))}
    </div>
);

export const Card = ({
    title,
    icon,
    children,
    defaultOpen = true,
}: {
    title: string;
    icon: string;
    children: React.ReactNode;
    defaultOpen?: boolean;
}) => {
    const [open, setOpen] = useState(defaultOpen);
    return (
        <div className="cf-card">
            <div className="cf-card__header" onClick={() => setOpen(!open)}>
                <span className="cf-card__icon">{icon}</span>
                <span className="cf-card__title">{title}</span>
                <span className="cf-card__chevron">{open ? "▾" : "▸"}</span>
            </div>
            {open && <div className="cf-card__body">{children}</div>}
        </div>
    );
};

export const Notice = ({ text, tone = "info" }: { text: string; tone?: "info" | "warning" }) => (
    <div className={`cf-notice cf-notice--${tone}`}>{text}</div>
);

export const ButtonRow = ({ children }: { children: React.ReactNode }) => <div className="cf-btn-row">{children}</div>;
