using System;
using Game;
using Game.Simulation;
using Unity.Entities;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Boosts move-ins and tourists by running the game's own spawn systems extra
    /// times. Spawning keeps following the game's rules (free homes, attractiveness),
    /// it just happens N times as often.
    /// </summary>
    public partial class PopulationBoostSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private SpawnBooster m_Households;
        private SpawnBooster m_Tourists;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_Households = new SpawnBooster(World, "Game.Simulation.HouseholdSpawnSystem");
            m_Tourists = new SpawnBooster(World, "Game.Simulation.TouristSpawnSystem");
        }

        protected override void OnUpdate()
        {
            var s = Mod.Settings;
            uint frame = m_SimulationSystem.frameIndex;

            if (s.BoostMoveIn)
                m_Households.Run(frame, s.MoveInMultiplier);
            if (s.BoostTourists)
                m_Tourists.Run(frame, s.TouristMultiplier);
        }

        private class SpawnBooster
        {
            private const int kMaxMultiplier = 10;

            private readonly GameSystemBase m_System;
            private readonly int m_Interval;
            private uint m_LastFrame = uint.MaxValue;

            public SpawnBooster(World world, string typeName)
            {
                // Looked up by name so a renamed game system disables the boost instead of breaking the build.
                var type = typeof(GameSystemBase).Assembly.GetType(typeName);
                m_System = type != null ? world.GetOrCreateSystemManaged(type) as GameSystemBase : null;

                if (m_System == null)
                {
                    Mod.Log.Warn($"{typeName} not found; its boost is disabled.");
                    return;
                }

                m_Interval = Math.Max(1, m_System.GetUpdateInterval(SystemUpdatePhase.GameSimulation));
            }

            public void Run(uint frame, int multiplier)
            {
                if (m_System == null || frame == m_LastFrame || frame % (uint)m_Interval != 0)
                    return;
                m_LastFrame = frame;

                // The game already ran it once this interval.
                int extra = Math.Min(multiplier, kMaxMultiplier) - 1;
                for (int i = 0; i < extra; i++)
                    m_System.Update();
            }
        }
    }
}
