const path = require("path");
const mod = require("./mod.json");

// The modding toolchain sets CSII_USERDATAPATH to the game's user data folder.
// Without it (e.g. CI), the bundle is written to ./build instead.
const userDataPath = process.env.CSII_USERDATAPATH;
const outputPath = userDataPath
    ? path.join(userDataPath, "Mods", mod.id)
    : path.resolve(__dirname, "build");

module.exports = {
    entry: { [mod.id]: path.resolve(__dirname, "src/index.tsx") },
    output: {
        path: outputPath,
        filename: "[name].mjs",
        library: { type: "module" },
        // The game serves every UI mod's folder under coui://ui-mods/.
        publicPath: "coui://ui-mods/",
        // Only wipe the local build folder, never the game's Mods folder.
        clean: !userDataPath,
    },
    experiments: { outputModule: true },
    resolve: { extensions: [".tsx", ".ts", ".js"] },
    module: {
        rules: [
            { test: /\.tsx?$/, use: "ts-loader", exclude: /node_modules/ },
            { test: /\.css$/, use: ["style-loader", "css-loader"] },
            // Namespaced folder so the files can't clash with other mods' assets.
            { test: /\.svg$/i, type: "asset/resource", generator: { filename: "cityforge-icons/[name][ext]" } },
        ],
    },
    // These are provided by the game at runtime.
    externalsType: "window",
    externals: {
        "react": "React",
        "react-dom": "ReactDOM",
        "cs2/modding": "cs2/modding",
        "cs2/api": "cs2/api",
        "cs2/bindings": "cs2/bindings",
        "cs2/l10n": "cs2/l10n",
        "cs2/ui": "cs2/ui",
        "cs2/input": "cs2/input",
        "cs2/utils": "cs2/utils",
        "cohtml/cohtml": "cohtml/cohtml",
    },
    performance: { hints: false },
};
