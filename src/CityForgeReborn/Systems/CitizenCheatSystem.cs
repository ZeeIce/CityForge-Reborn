using Game;
using Game.Citizens;
using Game.Common;
using Game.Economy;
using Game.Tools;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Citizen cheats: max happiness, max education, education distribution and
    /// rich households. All per-entity work runs as background jobs so it doesn't
    /// stall the main thread, even in large cities.
    /// </summary>
    public partial class CitizenCheatSystem : GameSystemBase
    {
        private const byte kMaxStat = 100;
        private const int kMaxEducationLevel = 4;
        private const int kRichHouseholdMoney = 500_000;

        private EntityQuery m_CitizenQuery;
        private EntityQuery m_HouseholdQuery;
        private bool m_ApplyDistribution;
        private uint m_DistributionSeed;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CitizenQuery = GetEntityQuery(
                ComponentType.ReadWrite<Citizen>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_HouseholdQuery = GetEntityQuery(
                ComponentType.ReadOnly<Household>(),
                ComponentType.ReadWrite<Resources>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 512;

        /// <summary>Queues a one-off redistribution of education levels using the saved percentages.</summary>
        public void ApplyEducationDistribution()
        {
            m_ApplyDistribution = true;
            m_DistributionSeed++;
        }

        protected override void OnUpdate()
        {
            var s = Mod.Settings;

            bool distribute = m_ApplyDistribution;
            m_ApplyDistribution = false;

            if ((s.MaxHappiness || s.MaxEducation || distribute) && !m_CitizenQuery.IsEmptyIgnoreFilter)
            {
                var job = new CitizenJob
                {
                    m_EntityType = GetEntityTypeHandle(),
                    m_CitizenType = GetComponentTypeHandle<Citizen>(false),
                    m_MaxHappiness = s.MaxHappiness,
                    m_MaxEducation = s.MaxEducation,
                    m_Distribute = distribute && !s.MaxEducation,
                    m_Thresholds = DistributionThresholds(s),
                    m_Seed = m_DistributionSeed,
                };
                Dependency = job.ScheduleParallel(m_CitizenQuery, Dependency);
            }

            if (s.RichCitizens && !m_HouseholdQuery.IsEmptyIgnoreFilter)
            {
                var job = new RichHouseholdJob
                {
                    m_ResourcesType = GetBufferTypeHandle<Resources>(false),
                    m_Money = kRichHouseholdMoney,
                };
                Dependency = job.ScheduleParallel(m_HouseholdQuery, Dependency);
            }
        }

        /// <summary>Cumulative percentages for education levels 0–3 (level 4 is the remainder).</summary>
        private static int4 DistributionThresholds(CityForgeSetting s)
        {
            var weights = new float4(s.EducationUneducated, s.EducationPoor, s.EducationEducated, s.EducationWell);
            float total = math.csum(weights) + s.EducationHigh;
            if (total <= 0f)
                return new int4(0);

            var share = weights / total * 100f;
            return new int4(
                (int)math.round(share.x),
                (int)math.round(share.x + share.y),
                (int)math.round(share.x + share.y + share.z),
                (int)math.round(math.csum(share)));
        }

        private struct CitizenJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Citizen> m_CitizenType;
            public bool m_MaxHappiness;
            public bool m_MaxEducation;
            public bool m_Distribute;
            public int4 m_Thresholds;
            public uint m_Seed;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(m_EntityType);
                var citizens = chunk.GetNativeArray(ref m_CitizenType);

                for (int i = 0; i < citizens.Length; i++)
                {
                    var citizen = citizens[i];

                    if (m_MaxHappiness)
                    {
                        citizen.m_WellBeing = kMaxStat;
                        citizen.m_Health = kMaxStat;
                    }

                    if (m_MaxEducation)
                    {
                        citizen.SetEducationLevel(kMaxEducationLevel);
                    }
                    else if (m_Distribute)
                    {
                        // Stable per-citizen roll, so the result matches the chosen percentages.
                        int roll = (int)(math.hash(new uint2((uint)entities[i].Index, m_Seed)) % 100);
                        int level = roll < m_Thresholds.x ? 0
                            : roll < m_Thresholds.y ? 1
                            : roll < m_Thresholds.z ? 2
                            : roll < m_Thresholds.w ? 3
                            : 4;
                        citizen.SetEducationLevel(level);
                    }

                    citizens[i] = citizen;
                }
            }
        }

        private struct RichHouseholdJob : IJobChunk
        {
            public BufferTypeHandle<Resources> m_ResourcesType;
            public int m_Money;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var resourceBuffers = chunk.GetBufferAccessor(ref m_ResourcesType);
                for (int i = 0; i < resourceBuffers.Length; i++)
                {
                    var resources = resourceBuffers[i];
                    int money = EconomyUtils.GetResources(Resource.Money, resources);
                    if (money < m_Money)
                        EconomyUtils.AddResources(Resource.Money, m_Money - money, resources);
                }
            }
        }
    }
}
