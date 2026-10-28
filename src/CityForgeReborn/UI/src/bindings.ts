import { bindValue, trigger, useValue, ValueBinding } from "cs2/api";

/** Must match CityForgeUISystem.kGroup on the C# side. */
const GROUP = "cityforge";

const cache = new Map<string, ValueBinding<any>>();

function binding<T>(key: string, fallback: T): ValueBinding<T> {
    let existing = cache.get(key);
    if (!existing) {
        existing = bindValue<T>(GROUP, key, fallback);
        cache.set(key, existing);
    }
    return existing;
}

export function useBool(key: string): [boolean, (value: boolean) => void] {
    const value = useValue(binding(key, false));
    return [value, (v) => trigger(GROUP, "setBool", key, v)];
}

export function useInt(key: string, fallback = 0): [number, (value: number) => void] {
    const value = useValue(binding(key, fallback));
    return [value, (v) => trigger(GROUP, "setInt", key, Math.round(v))];
}

export function useString(key: string, fallback = ""): [string, (value: string) => void] {
    const value = useValue(binding(key, fallback));
    return [value, (v) => trigger(GROUP, "setString", key, v)];
}

export function useReadOnlyInt(key: string, fallback = 0): number {
    return useValue(binding(key, fallback));
}

export function useReadOnlyString(key: string, fallback = ""): string {
    return useValue(binding(key, fallback));
}

export const actions = {
    addMoney: (amount: number) => trigger(GROUP, "addMoney", amount),
    advanceMilestone: () => trigger(GROUP, "advanceMilestone"),
    unlockAllMilestones: () => trigger(GROUP, "unlockAllMilestones"),
    addDevTreePoints: (points: number) => trigger(GROUP, "addDevTreePoints", points),
    applyEducationDistribution: () => trigger(GROUP, "applyEducationDistribution"),
    fillStorage: () => trigger(GROUP, "fillStorage"),
    captureHotkey: () => trigger(GROUP, "captureHotkey"),
    cancelHotkey: () => trigger(GROUP, "cancelHotkey"),
    resetHotkey: () => trigger(GROUP, "resetHotkey"),
};
