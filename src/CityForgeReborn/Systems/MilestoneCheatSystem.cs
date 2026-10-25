using Game;
using Game.City;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Milestone and development-tree cheats. Milestones are advanced by raising the
    /// city's XP to the requirement of the target milestone, so the game's own
    /// MilestoneSystem hands out the rewards and the XP bar stays consistent.
    /// </summary>
    public partial class MilestoneCheatSystem : GameSystemBase
    {
        private CitySystem m_CitySystem;
        private EntityQuery m_MilestoneQuery;
        private int m_CachedMaxMilestone;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_MilestoneQuery = GetEntityQuery(ComponentType.ReadOnly<MilestoneData>());
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 256;

        protected override void OnUpdate()
        {
            var settings = Mod.Settings;
            if (settings.KeepMilestonesUnlocked && CurrentMilestone < settings.MilestoneTarget)
                AdvanceTo(settings.MilestoneTarget);
        }

        public int CurrentMilestone
        {
            get
            {
                var city = m_CitySystem.City;
                if (city == Entity.Null || !EntityManager.HasComponent<MilestoneLevel>(city))
                    return 0;
                return EntityManager.GetComponentData<MilestoneLevel>(city).m_AchievedMilestone;
            }
        }

        public int MaxMilestone
        {
            get
            {
                if (m_CachedMaxMilestone == 0 && !m_MilestoneQuery.IsEmptyIgnoreFilter)
                {
                    using var milestones = m_MilestoneQuery.ToComponentDataArray<MilestoneData>(Allocator.Temp);
                    foreach (var milestone in milestones)
                    {
                        if (milestone.m_Index > m_CachedMaxMilestone)
                            m_CachedMaxMilestone = milestone.m_Index;
                    }
                }
                return m_CachedMaxMilestone;
            }
        }

        public void AdvanceTo(int target)
        {
            var city = m_CitySystem.City;
            if (city == Entity.Null || !EntityManager.HasComponent<XP>(city))
                return;

            int required = GetRequiredXP(target);
            if (required < 0)
            {
                Mod.Log.Warn($"Milestone {target} not found.");
                return;
            }

            var xp = EntityManager.GetComponentData<XP>(city);
            if (xp.m_XP < required)
            {
                xp.m_XP = required;
                EntityManager.SetComponentData(city, xp);
            }
        }

        public void UnlockAll() => AdvanceTo(MaxMilestone);

        public void AddDevTreePoints(int points)
        {
            var city = m_CitySystem.City;
            if (city == Entity.Null || !EntityManager.HasComponent<DevTreePoints>(city))
                return;

            var devPoints = EntityManager.GetComponentData<DevTreePoints>(city);
            devPoints.m_Points += points;
            EntityManager.SetComponentData(city, devPoints);
        }

        private int GetRequiredXP(int index)
        {
            using var milestones = m_MilestoneQuery.ToComponentDataArray<MilestoneData>(Allocator.Temp);
            foreach (var milestone in milestones)
            {
                // "m_XpRequried" is the game's own (misspelled) field name.
                if (milestone.m_Index == index)
                    return milestone.m_XpRequried;
            }
            return -1;
        }
    }
}
