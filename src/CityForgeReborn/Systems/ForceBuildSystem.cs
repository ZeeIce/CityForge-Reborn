using System;
using System.Collections.Generic;
using System.Reflection;
using Game;
using Game.Economy;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Force Build: runs the game's zone spawner one extra time with its built-in
    /// <c>debugFastSpawn</c> flag on. That flag skips the zone and per-building demand
    /// checks and floors the suitability score, so buildings spawn on low land value
    /// / unsuitable lots too (e.g. low-density houses on expensive land).
    ///
    /// <c>debugFastSpawn</c> affects every zone, so for the duration of the extra run the
    /// building demand of zones that aren't forced is pushed far below zero (which fails
    /// the spawner's per-building check) and restored right after.
    ///
    /// The extra run is offset half an interval from the game's own spawner run, so the
    /// two never pick the same vacant lot in the same frame.
    /// </summary>
    public partial class ForceBuildSystem : GameSystemBase
    {
        private const int kBlocked = -1000;

        private static readonly FieldInfo kLastResidentialBuildingDemand =
            typeof(ResidentialDemandSystem).GetField("m_LastBuildingDemand", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo kDebugFastSpawn = typeof(ZoneSpawnSystem).GetProperty("debugFastSpawn");

        private ZoneSpawnSystem m_ZoneSpawnSystem;
        private ResidentialDemandSystem m_Residential;
        private CommercialDemandSystem m_Commercial;
        private IndustrialDemandSystem m_Industrial;
        private bool m_Available;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 16;

        // ZoneSpawnSystem runs at offset 13 of the same interval.
        public override int GetUpdateOffset(SystemUpdatePhase phase) => 5;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ZoneSpawnSystem = World.GetOrCreateSystemManaged<ZoneSpawnSystem>();
            m_Residential = World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_Commercial = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_Industrial = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();

            m_Available = kLastResidentialBuildingDemand != null && kDebugFastSpawn != null && kDebugFastSpawn.CanWrite;
            if (!m_Available)
                Mod.Log.Warn("ZoneSpawnSystem.debugFastSpawn or ResidentialDemandSystem.m_LastBuildingDemand not found; Force Build falls back to 100% demand only.");
        }

        protected override void OnUpdate()
        {
            var s = Mod.Settings;
            if (!m_Available || !(s.ResidentialForce || s.CommercialForce || s.IndustrialForce || s.OfficeForce))
                return;

            // The user may have the game's own debug fast spawn on; leave it alone then.
            if ((bool)kDebugFastSpawn.GetValue(m_ZoneSpawnSystem))
                return;

            CompleteJobs(m_Commercial);
            CompleteJobs(m_Industrial);

            var residentialDemand = (int3)kLastResidentialBuildingDemand.GetValue(m_Residential);
            var commercialDemands = m_Commercial.GetBuildingDemands(out _);
            var industrialDemands = m_Industrial.GetBuildingDemands(out _);
            var storageDemands = m_Industrial.GetStorageBuildingDemands(out _);

            int[] savedCommercial = commercialDemands.ToArray();
            int[] savedIndustrial = industrialDemands.ToArray();
            int[] savedStorage = storageDemands.ToArray();

            try
            {
                if (!s.ResidentialForce)
                    kLastResidentialBuildingDemand.SetValue(m_Residential, new int3(kBlocked));
                if (!s.CommercialForce)
                    Fill(commercialDemands, kBlocked, _ => true);
                if (!s.IndustrialForce)
                {
                    Fill(industrialDemands, kBlocked, i => !IsOffice(i));
                    Fill(storageDemands, kBlocked, _ => true);
                }
                if (!s.OfficeForce)
                    Fill(industrialDemands, kBlocked, IsOffice);

                kDebugFastSpawn.SetValue(m_ZoneSpawnSystem, true);
                m_ZoneSpawnSystem.Update();
            }
            finally
            {
                kDebugFastSpawn.SetValue(m_ZoneSpawnSystem, false);

                // The spawn job reads the arrays asynchronously; wait for it before restoring.
                CompleteJobs(m_Commercial);
                CompleteJobs(m_Industrial);

                kLastResidentialBuildingDemand.SetValue(m_Residential, residentialDemand);
                commercialDemands.CopyFrom(savedCommercial);
                industrialDemands.CopyFrom(savedIndustrial);
                storageDemands.CopyFrom(savedStorage);
            }
        }

        public static bool IsOffice(int resourceIndex) => EconomyUtils.IsOfficeResource(EconomyUtils.GetResource(resourceIndex));

        private static void Fill(NativeArray<int> array, int value, Func<int, bool> include)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (include(i))
                    array[i] = value;
            }
        }

        private static readonly Dictionary<Type, FieldInfo[]> kJobHandleFields = new Dictionary<Type, FieldInfo[]>();

        /// <summary>Completes every reader/writer job a demand system is tracking.</summary>
        private static void CompleteJobs(object system)
        {
            var type = system.GetType();
            if (!kJobHandleFields.TryGetValue(type, out var fields))
            {
                fields = Array.FindAll(
                    type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                    field => field.FieldType == typeof(JobHandle));
                kJobHandleFields[type] = fields;
            }

            foreach (var field in fields)
                ((JobHandle)field.GetValue(system)).Complete();
        }
    }
}
