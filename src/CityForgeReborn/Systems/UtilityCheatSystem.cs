using Game;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Unlimited water and electricity.
    ///
    /// Water uses the game's own switches on <see cref="DispatchWaterSystem"/>, which
    /// give every building full fresh water and sewage service. The game resets them
    /// when a city loads, so they're re-applied every update while the cheat is on.
    ///
    /// Electricity has no such switch, so this system runs right after
    /// <see cref="DispatchElectricitySystem"/> (same interval and offset) and tops every
    /// building up to what it wanted, clearing the warnings and efficiency penalty.
    /// The cooldown is reset each time, so the game never gets far enough to warn again.
    /// </summary>
    public partial class UtilityCheatSystem : GameSystemBase
    {
        private DispatchWaterSystem m_DispatchWater;
        private IconCommandSystem m_IconCommandSystem;
        private EntityQuery m_ConsumerQuery;
        private EntityQuery m_ParameterQuery;
        private bool m_WaterApplied;

        // DispatchElectricitySystem runs at interval 128, offset 126.
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 128;

        public override int GetUpdateOffset(SystemUpdatePhase phase) => 126;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DispatchWater = World.GetOrCreateSystemManaged<DispatchWaterSystem>();
            m_IconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
            m_ConsumerQuery = GetEntityQuery(
                ComponentType.ReadWrite<ElectricityConsumer>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<ElectricityParameterData>());
        }

        protected override void OnUpdate()
        {
            var s = Mod.Settings;

            if (s.UnlimitedWater)
            {
                m_DispatchWater.freshConsumptionDisabled = true;
                m_DispatchWater.sewageConsumptionDisabled = true;
                m_WaterApplied = true;
            }
            else if (m_WaterApplied)
            {
                // Only undo what we set; the game also uses these for legacy saves.
                m_DispatchWater.freshConsumptionDisabled = false;
                m_DispatchWater.sewageConsumptionDisabled = false;
                m_WaterApplied = false;
            }

            if (!s.UnlimitedElectricity || m_ConsumerQuery.IsEmptyIgnoreFilter || m_ParameterQuery.IsEmptyIgnoreFilter)
                return;

            var parameters = m_ParameterQuery.GetSingleton<ElectricityParameterData>();
            var job = new FullElectricityJob
            {
                m_EntityType = GetEntityTypeHandle(),
                m_ConsumerType = GetComponentTypeHandle<ElectricityConsumer>(false),
                m_EfficiencyType = GetBufferTypeHandle<Efficiency>(false),
                m_NoElectricityIcon = parameters.m_ElectricityNotificationPrefab,
                m_BottleneckIcon = parameters.m_BuildingBottleneckNotificationPrefab,
                m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
            };
            Dependency = job.Schedule(m_ConsumerQuery, Dependency);
            m_IconCommandSystem.AddCommandBufferWriter(Dependency);
        }

        private struct FullElectricityJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<ElectricityConsumer> m_ConsumerType;
            public BufferTypeHandle<Efficiency> m_EfficiencyType;
            public Entity m_NoElectricityIcon;
            public Entity m_BottleneckIcon;
            public IconCommandBuffer m_IconCommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var consumers = chunk.GetNativeArray(ref m_ConsumerType);
                var efficiencies = chunk.GetBufferAccessor(ref m_EfficiencyType);

                for (int i = 0; i < consumers.Length; i++)
                {
                    var consumer = consumers[i];

                    if ((consumer.m_Flags & ElectricityConsumerFlags.NoElectricityWarning) != 0)
                        m_IconCommandBuffer.Remove(entities[i], m_NoElectricityIcon);
                    if ((consumer.m_Flags & ElectricityConsumerFlags.BottleneckWarning) != 0)
                        m_IconCommandBuffer.Remove(entities[i], m_BottleneckIcon);

                    consumer.m_FulfilledConsumption = consumer.m_WantedConsumption;
                    consumer.m_CooldownCounter = 0;
                    consumer.m_Flags = ElectricityConsumerFlags.Connected;
                    consumers[i] = consumer;

                    if (efficiencies.Length != 0)
                        BuildingUtils.SetEfficiencyFactor(efficiencies[i], EfficiencyFactor.ElectricitySupply, 1f);
                }
            }
        }
    }
}
