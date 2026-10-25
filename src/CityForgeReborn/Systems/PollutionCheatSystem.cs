using Game;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// No pollution: clears the air, ground and noise pollution maps and the pollution
    /// in groundwater.
    ///
    /// Building and road pollution values are left alone. Changing those (and the
    /// global pollution parameters) is what other no-pollution mods do, and it's where
    /// noise pollution tends to break: values get stuck at zero or drift after toggling.
    /// Clearing the maps only touches the result, so turning the cheat off simply lets
    /// pollution build up again normally. An all-zero noise map is the same state a new
    /// city starts in, including the map's temporary accumulation layer.
    ///
    /// Water surface pollution (sewage in rivers and lakes) runs on the GPU and isn't covered.
    /// </summary>
    public partial class PollutionCheatSystem : GameSystemBase
    {
        private AirPollutionSystem m_Air;
        private GroundPollutionSystem m_Ground;
        private NoisePollutionSystem m_Noise;
        private GroundWaterSystem m_GroundWater;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Air = World.GetOrCreateSystemManaged<AirPollutionSystem>();
            m_Ground = World.GetOrCreateSystemManaged<GroundPollutionSystem>();
            m_Noise = World.GetOrCreateSystemManaged<NoisePollutionSystem>();
            m_GroundWater = World.GetOrCreateSystemManaged<GroundWaterSystem>();
        }

        protected override void OnUpdate()
        {
            if (!Mod.Settings.NoPollution)
                return;

            m_Air.AddWriter(Clear(m_Air.GetMap(false, out var airDeps), airDeps));
            m_Ground.AddWriter(Clear(m_Ground.GetMap(false, out var groundDeps), groundDeps));
            m_Noise.AddWriter(Clear(m_Noise.GetMap(false, out var noiseDeps), noiseDeps));

            var water = new CleanGroundWaterJob { m_Map = m_GroundWater.GetMap(false, out var waterDeps) }.Schedule(waterDeps);
            m_GroundWater.AddWriter(water);
        }

        /// <summary>Zeroes a whole map. Viewed as bytes so one non-generic Burst job covers every map type.</summary>
        private static JobHandle Clear<T>(NativeArray<T> map, JobHandle dependencies) where T : struct
        {
            var bytes = map.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
            return new ClearJob { m_Bytes = bytes }.Schedule(dependencies);
        }

        [BurstCompile]
        private struct ClearJob : IJob
        {
            public NativeArray<byte> m_Bytes;

            public void Execute()
            {
                for (int i = 0; i < m_Bytes.Length; i++)
                    m_Bytes[i] = 0;
            }
        }

        [BurstCompile]
        private struct CleanGroundWaterJob : IJob
        {
            public NativeArray<GroundWater> m_Map;

            public void Execute()
            {
                // Only the pollution; the amount of water is left as is.
                for (int i = 0; i < m_Map.Length; i++)
                {
                    var cell = m_Map[i];
                    if (cell.m_Polluted == 0)
                        continue;
                    cell.m_Polluted = 0;
                    m_Map[i] = cell;
                }
            }
        }
    }
}
