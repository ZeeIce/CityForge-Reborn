using Game;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Building cheats: level up to a target level, max efficiency and storage filling.
    ///
    /// Levelling works by topping up each building's condition so the game's own
    /// upkeep system levels it up naturally, one level at a time. That avoids
    /// replacing thousands of buildings at once (which can freeze the game).
    /// </summary>
    public partial class BuildingCheatSystem : GameSystemBase
    {
        private const int kLevelUpCondition = 100_000_000;
        private const float kMaxEfficiency = 1f;

        private EntityQuery m_LevelQuery;
        private EntityQuery m_EfficiencyQuery;
        private EntityQuery m_StorageQuery;
        private bool m_FillStorageOnce;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_LevelQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadWrite<BuildingCondition>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_EfficiencyQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadWrite<Efficiency>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_StorageQuery = GetEntityQuery(
                ComponentType.ReadOnly<StorageCompany>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadWrite<Resources>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        public void FillStorage() => m_FillStorageOnce = true;

        protected override void OnUpdate()
        {
            var s = Mod.Settings;

            if (s.LevelUpBuildings && !m_LevelQuery.IsEmptyIgnoreFilter)
            {
                var job = new LevelUpJob
                {
                    m_PrefabRefType = GetComponentTypeHandle<PrefabRef>(true),
                    m_ConditionType = GetComponentTypeHandle<BuildingCondition>(false),
                    m_SpawnableData = GetComponentLookup<SpawnableBuildingData>(true),
                    m_TargetLevel = math.clamp(s.BuildingLevelTarget, 1, 5),
                };
                Dependency = job.ScheduleParallel(m_LevelQuery, Dependency);
            }

            if (s.MaxEfficiency && !m_EfficiencyQuery.IsEmptyIgnoreFilter)
            {
                var job = new EfficiencyJob { m_EfficiencyType = GetBufferTypeHandle<Efficiency>(false) };
                Dependency = job.ScheduleParallel(m_EfficiencyQuery, Dependency);
            }

            bool fill = m_FillStorageOnce || s.KeepStorageFull;
            m_FillStorageOnce = false;
            if (fill && !m_StorageQuery.IsEmptyIgnoreFilter)
            {
                var job = new FillStorageJob
                {
                    m_PrefabRefType = GetComponentTypeHandle<PrefabRef>(true),
                    m_ResourcesType = GetBufferTypeHandle<Resources>(false),
                    m_StorageData = GetComponentLookup<StorageCompanyData>(true),
                    m_LimitData = GetComponentLookup<StorageLimitData>(true),
                };
                Dependency = job.ScheduleParallel(m_StorageQuery, Dependency);
            }
        }

        private struct LevelUpJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            public ComponentTypeHandle<BuildingCondition> m_ConditionType;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_SpawnableData;
            public int m_TargetLevel;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var prefabs = chunk.GetNativeArray(ref m_PrefabRefType);
                var conditions = chunk.GetNativeArray(ref m_ConditionType);

                for (int i = 0; i < conditions.Length; i++)
                {
                    if (!m_SpawnableData.TryGetComponent(prefabs[i].m_Prefab, out var spawnable) || spawnable.m_Level >= m_TargetLevel)
                        continue;

                    var condition = conditions[i];
                    if (condition.m_Condition < kLevelUpCondition)
                    {
                        condition.m_Condition = kLevelUpCondition;
                        conditions[i] = condition;
                    }
                }
            }
        }

        private struct EfficiencyJob : IJobChunk
        {
            public BufferTypeHandle<Efficiency> m_EfficiencyType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffers = chunk.GetBufferAccessor(ref m_EfficiencyType);
                for (int i = 0; i < buffers.Length; i++)
                {
                    var factors = buffers[i];
                    for (int j = 0; j < factors.Length; j++)
                    {
                        var factor = factors[j];
                        if (factor.m_Efficiency < kMaxEfficiency)
                        {
                            factor.m_Efficiency = kMaxEfficiency;
                            factors[j] = factor;
                        }
                    }
                }
            }
        }

        private struct FillStorageJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<PrefabRef> m_PrefabRefType;
            public BufferTypeHandle<Resources> m_ResourcesType;
            [ReadOnly] public ComponentLookup<StorageCompanyData> m_StorageData;
            [ReadOnly] public ComponentLookup<StorageLimitData> m_LimitData;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var prefabs = chunk.GetNativeArray(ref m_PrefabRefType);
                var resourceBuffers = chunk.GetBufferAccessor(ref m_ResourcesType);

                for (int i = 0; i < resourceBuffers.Length; i++)
                {
                    var prefab = prefabs[i].m_Prefab;
                    if (!m_StorageData.TryGetComponent(prefab, out var storage) || !m_LimitData.TryGetComponent(prefab, out var limit))
                        continue;

                    ulong stored = (ulong)storage.m_StoredResources;
                    int types = math.countbits(stored);
                    if (types == 0)
                        continue;

                    // Split capacity evenly between the resources this storage handles.
                    int perResource = limit.m_Limit / types;
                    var resources = resourceBuffers[i];

                    for (int bit = 0; bit < 64; bit++)
                    {
                        if ((stored & (1UL << bit)) == 0)
                            continue;

                        var resource = (Resource)(1UL << bit);
                        int current = EconomyUtils.GetResources(resource, resources);
                        if (current < perResource)
                            EconomyUtils.AddResources(resource, perResource - current, resources);
                    }
                }
            }
        }
    }
}
