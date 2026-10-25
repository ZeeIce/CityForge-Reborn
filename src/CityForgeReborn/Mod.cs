using CityForgeReborn.Systems;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;

namespace CityForgeReborn
{
    public class Mod : IMod
    {
        public static ILog Log = LogManager.GetLogger($"{nameof(CityForgeReborn)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static Setting Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
            AssetDatabase.global.LoadSettings(nameof(CityForgeReborn), Settings, new Setting(this));

            updateSystem.UpdateAt<MoneyCheatSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<MilestoneCheatSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<ConstructionCheatSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CitizenCheatSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PopulationBoostSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<BuildingCheatSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<PollutionCheatSystem>(SystemUpdatePhase.GameSimulation);
            // Must run right after the game hands out electricity.
            updateSystem.UpdateAfter<UtilityCheatSystem, DispatchElectricitySystem>(SystemUpdatePhase.GameSimulation);
            // Demand must be overwritten right before the zone spawner reads it.
            updateSystem.UpdateBefore<DemandOverrideSystem, ZoneSpawnSystem>(SystemUpdatePhase.GameSimulation);
            // Extra spawner run for Force Build, using the demand values set above.
            updateSystem.UpdateAfter<ForceBuildSystem, DemandOverrideSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<CityForgeUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<AchievementSystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
