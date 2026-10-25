using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Objects;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Instant construction and build-speed multiplier. Rather than depending on how
    /// the game computes construction progress, we watch how far each building
    /// progressed since the last tick and scale that step by the chosen multiplier.
    /// </summary>
    public partial class ConstructionCheatSystem : GameSystemBase
    {
        public static readonly float[] SpeedMultipliers = { 0.5f, 1f, 2f, 4f };

        private const byte kComplete = 100;

        private EntityQuery m_Query;
        private readonly Dictionary<Entity, float> m_Progress = new Dictionary<Entity, float>();
        private readonly HashSet<Entity> m_Seen = new HashSet<Entity>();

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Query = GetEntityQuery(
                ComponentType.ReadWrite<UnderConstruction>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        protected override void OnUpdate()
        {
            var settings = Mod.Settings;
            bool instant = settings.InstantConstruction;
            float multiplier = SpeedMultipliers[math.clamp(settings.BuildSpeed, 0, SpeedMultipliers.Length - 1)];

            if ((!instant && multiplier == 1f) || m_Query.IsEmptyIgnoreFilter)
            {
                m_Progress.Clear();
                return;
            }

            using var entities = m_Query.ToEntityArray(Allocator.Temp);
            m_Seen.Clear();

            foreach (var entity in entities)
            {
                m_Seen.Add(entity);
                var construction = EntityManager.GetComponentData<UnderConstruction>(entity);

                if (instant)
                {
                    if (construction.m_Progress < kComplete)
                    {
                        construction.m_Progress = kComplete;
                        EntityManager.SetComponentData(entity, construction);
                    }
                    continue;
                }

                if (!m_Progress.TryGetValue(entity, out float last))
                {
                    m_Progress[entity] = construction.m_Progress;
                    continue;
                }

                // How far the game moved this building since we last touched it.
                float step = construction.m_Progress - math.floor(last);
                if (step <= 0f)
                {
                    if (step < 0f)
                        m_Progress[entity] = construction.m_Progress;
                    continue;
                }

                float next = math.min(kComplete, last + step * multiplier);
                m_Progress[entity] = next;

                byte rounded = (byte)math.floor(next);
                if (rounded != construction.m_Progress)
                {
                    construction.m_Progress = rounded;
                    EntityManager.SetComponentData(entity, construction);
                }
            }

            if (m_Progress.Count != m_Seen.Count)
            {
                var stale = new List<Entity>();
                foreach (var entity in m_Progress.Keys)
                {
                    if (!m_Seen.Contains(entity))
                        stale.Add(entity);
                }
                foreach (var entity in stale)
                    m_Progress.Remove(entity);
            }
        }
    }
}
