// Minimal type declarations for the game-provided UI modules used by this mod.
// The official modding toolchain ships fuller versions of these if you want them.

declare module "cs2/api" {
    export interface ValueBinding<T> {
        readonly value: T;
        subscribe(listener?: (value: T) => void): { setChangeListener(l: (v: T) => void): void; dispose(): void };
    }
    export function bindValue<T>(group: string, name: string, fallbackValue?: T): ValueBinding<T>;
    export function useValue<T>(binding: ValueBinding<T>): T;
    export function trigger(group: string, name: string, ...args: any[]): void;
}

declare module "cs2/modding" {
    import type { ComponentType } from "react";
    export interface ModuleRegistry {
        append(location: string, component: ComponentType<any>): void;
    }
    export type ModRegistrar = (moduleRegistry: ModuleRegistry) => void;
}

declare module "*.css";
