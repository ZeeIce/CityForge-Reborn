// Writes the buff-bar icons as small white line-art SVG files (24px grid).
// The game's UI shows SVG images fine; tiles add the colour around them.
// Usage: node tools/make-icons.js
const fs = require("fs");
const path = require("path");

const icons = {
    money: "M12 3v18M16 7.5c0-1.7-1.8-3-4-3s-4 1.3-4 3 1.8 2.6 4 3 4 1.4 4 3-1.8 3-4 3-4-1.3-4-3",
    milestone: "M6 21V4M6 4h11l-2.5 4L17 12H6",
    instant: "M4 20h9M7 20v-7l5-5M12 8l4 4-5 5M14 6l4-2 2 2-2 4",
    speed: "M4 16a8 8 0 1 1 16 0M12 16l4-5M9 16h6",
    movein: "M4 11l8-6 8 6v9H4zM10 20v-5h4v5M12 2v4M10 4l2 2 2-2",
    tourist: "M6 8h12v11H6zM9 8V5h6v3M9 12v4M15 12v4",
    happy: "M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM8.5 14.5c1 1.3 2.2 2 3.5 2s2.5-.7 3.5-2M9 9.5h.01M15 9.5h.01",
    rich: "M6 4h12l3 5-9 11L3 9zM3 9h18M9 4l3 16 3-16",
    education: "M2 9l10-5 10 5-10 5zM6 11v5c3 2.5 9 2.5 12 0v-5M22 9v6",
    levelup: "M6 13l6-6 6 6M6 19l6-6 6 6",
    storage: "M3 8l9-5 9 5v10l-9 5-9-5zM3 8l9 5 9-5M12 13v10",
    efficiency: "M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9L7 7M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1",
    water: "M12 3c3.5 4.5 6 7.7 6 11a6 6 0 0 1-12 0c0-3.3 2.5-6.5 6-11z",
    power: "M13 2L4 14h7l-1 8 9-12h-7z",
    pollution: "M5 19c0-8 6-13 15-14-1 9-6 15-14 15M5 19l7-7",
    demand: "M4 20V12M10 20V7M16 20v-5M22 20H2M16 9l3-4 3 4",
    force: "M3 21h18M6 21V10l6-5 6 5v11M10 21v-6h4v6M2 7l3-2M22 7l-3-2",
    pin: "M9 3h6l-1 6 4 4H6l4-4zM12 13v8",
};

const out = path.join(__dirname, "..", "src", "CityForgeReborn", "UI", "src", "icons");
fs.mkdirSync(out, { recursive: true });
for (const [name, d] of Object.entries(icons)) {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#ffffff" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="${d}"/></svg>\n`;
    fs.writeFileSync(path.join(out, `${name}.svg`), svg);
}
console.log(`wrote ${Object.keys(icons).length} icons to ${out}`);
