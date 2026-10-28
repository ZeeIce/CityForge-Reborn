// Every ongoing cheat the buff bar can show, in panel-tab order.
// `key` is the option name on the C# side (CityForgeUISystem registers them).
import money from "./icons/money.svg";
import milestone from "./icons/milestone.svg";
import instant from "./icons/instant.svg";
import speed from "./icons/speed.svg";
import levelup from "./icons/levelup.svg";
import storage from "./icons/storage.svg";
import efficiency from "./icons/efficiency.svg";
import water from "./icons/water.svg";
import power from "./icons/power.svg";
import pollution from "./icons/pollution.svg";
import movein from "./icons/movein.svg";
import tourist from "./icons/tourist.svg";
import happy from "./icons/happy.svg";
import rich from "./icons/rich.svg";
import education from "./icons/education.svg";
import demand from "./icons/demand.svg";
import force from "./icons/force.svg";
import pin from "./icons/pin.svg";

export const pinIcon = pin;

export type Category = "economy" | "build" | "people" | "demand";

export const CATEGORIES: Record<Category, { label: string; color: string }> = {
    economy: { label: "Economy", color: "#ffb15c" },
    build: { label: "Build", color: "#6cb8ff" },
    people: { label: "People", color: "#7ed6a0" },
    demand: { label: "Demand", color: "#c79bff" },
};

export interface Cheat {
    key: string;
    label: string;
    cat: Category;
    icon: string;
}

/** On/off cheats: these can be pinned and toggled from the bar. */
export const CHEATS: Cheat[] = [
    { key: "infiniteMoney", label: "Infinite money", cat: "economy", icon: money },
    { key: "keepMilestonesUnlocked", label: "Keep milestones unlocked", cat: "economy", icon: milestone },
    { key: "instantConstruction", label: "Instant build", cat: "build", icon: instant },
    { key: "levelUpBuildings", label: "Level up buildings", cat: "build", icon: levelup },
    { key: "keepStorageFull", label: "Keep storage full", cat: "build", icon: storage },
    { key: "maxEfficiency", label: "Max efficiency", cat: "build", icon: efficiency },
    { key: "unlimitedWater", label: "Unlimited water", cat: "build", icon: water },
    { key: "unlimitedElectricity", label: "Unlimited electricity", cat: "build", icon: power },
    { key: "noPollution", label: "No pollution", cat: "build", icon: pollution },
    { key: "boostMoveIn", label: "Boost move-ins", cat: "people", icon: movein },
    { key: "boostTourists", label: "Boost tourists", cat: "people", icon: tourist },
    { key: "maxHappiness", label: "Max happiness", cat: "people", icon: happy },
    { key: "richCitizens", label: "Rich citizens", cat: "people", icon: rich },
    { key: "maxEducation", label: "Max education", cat: "people", icon: education },
    { key: "residentialOverride", label: "Residential demand", cat: "demand", icon: demand },
    { key: "residentialForce", label: "Residential Force Build", cat: "demand", icon: force },
    { key: "commercialOverride", label: "Commercial demand", cat: "demand", icon: demand },
    { key: "commercialForce", label: "Commercial Force Build", cat: "demand", icon: force },
    { key: "industrialOverride", label: "Industrial demand", cat: "demand", icon: demand },
    { key: "industrialForce", label: "Industrial Force Build", cat: "demand", icon: force },
    { key: "officeOverride", label: "Office demand", cat: "demand", icon: demand },
    { key: "officeForce", label: "Office Force Build", cat: "demand", icon: force },
];

/** Build speed isn't on/off; the bar shows it while it isn't 1× and a click puts it back. */
export const SPEED = { key: "buildSpeed", cat: "build" as Category, icon: speed, labels: ["½×", "1×", "2×", "4×"], normal: 1 };

/** Pins are stored as a comma-separated list of keys, in the order they were pinned. */
export function parsePins(value: string): string[] {
    const known = new Set(CHEATS.map((c) => c.key));
    return value.split(",").filter((k) => known.has(k));
}
