using Colossal.PSI.Common;
using Game;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Keeps achievements enabled while mods are loaded. The game turns them off
    /// when it detects mods (and again on some loads), so the flag is re-applied
    /// periodically while the option is on.
    /// </summary>
    public partial class AchievementSystem : GameSystemBase
    {
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;

        protected override void OnUpdate()
        {
            if (!Mod.Settings.KeepAchievementsEnabled)
                return;

            var platform = PlatformManager.instance;
            if (platform != null && !platform.achievementsEnabled)
            {
                platform.achievementsEnabled = true;
                Mod.Log.Info("Achievements re-enabled.");
            }
        }
    }
}
