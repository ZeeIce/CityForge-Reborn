using System;
using System.Collections.Generic;
using System.Reflection;
using Game;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Overrides zone demand by writing straight into the demand systems' output
    /// values right before <see cref="ZoneSpawnSystem"/> reads them.
    ///
    /// The demand systems keep their results in private NativeValue/NativeArray
    /// fields, so they are located by reflection once, by name, and anything that
    /// can't be found is logged and skipped instead of crashing the game.
    /// </summary>
    public partial class DemandOverrideSystem : GameSystemBase
    {
        private const int kForced = 100;

        private DemandTarget m_Residential;
        private DemandTarget m_Commercial;
        private DemandTarget m_Industrial;
        private DemandTarget m_Office;

        protected override void OnCreate()
        {
            base.OnCreate();

            var residential = World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            var commercial = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            var industrial = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();

            m_Residential = new DemandTarget(residential, name => true, name => true, index => true);
            m_Commercial = new DemandTarget(commercial, name => true, name => true, index => true);
            // The industrial system also computes office and storage demand. Offices share its
            // per-resource arrays (the ones the zone spawner reads), split by resource.
            m_Industrial = new DemandTarget(industrial, name => !IsOfficeName(name), name => true, index => !ForceBuildSystem.IsOffice(index));
            m_Office = new DemandTarget(industrial, IsOfficeName, name => name.IndexOf("Storage", StringComparison.Ordinal) < 0, ForceBuildSystem.IsOffice);
        }

        protected override void OnUpdate()
        {
            var s = Mod.Settings;

            if (s.ResidentialOverride || s.ResidentialForce)
            {
                var levels = s.ResidentialForce
                    ? new int3(kForced)
                    : new int3(s.ResidentialLow, s.ResidentialMedium, s.ResidentialHigh);
                m_Residential.Apply(levels);
            }

            if (s.CommercialOverride || s.CommercialForce)
                m_Commercial.Apply(new int3(s.CommercialForce ? kForced : s.CommercialDemand));

            if (s.IndustrialOverride || s.IndustrialForce)
                m_Industrial.Apply(new int3(s.IndustrialForce ? kForced : s.IndustrialDemand));

            if (s.OfficeOverride || s.OfficeForce)
                m_Office.Apply(new int3(s.OfficeForce ? kForced : s.OfficeDemand));
        }

        private static bool IsOfficeName(string name) => name.IndexOf("Office", StringComparison.Ordinal) >= 0;

        private class DemandTarget
        {
            private static readonly string[] kDemandNames = { "BuildingDemand", "CompanyDemand", "HouseholdDemand", "ResourceDemand" };

            private readonly object m_System;
            private readonly List<FieldInfo> m_Values = new List<FieldInfo>();
            private readonly List<FieldInfo> m_Arrays = new List<FieldInfo>();
            private readonly List<FieldInfo> m_Dependencies = new List<FieldInfo>();
            private readonly Func<int, bool> m_IndexFilter;

            /// <param name="valueFilter">Which NativeValue fields (by name) belong to this zone.</param>
            /// <param name="arrayFilter">Which per-resource arrays (by name) belong to this zone.</param>
            /// <param name="indexFilter">Which resource indices of those arrays belong to this zone.</param>
            public DemandTarget(object system, Func<string, bool> valueFilter, Func<string, bool> arrayFilter, Func<int, bool> indexFilter)
            {
                m_System = system;
                m_IndexFilter = indexFilter;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                foreach (var field in system.GetType().GetFields(flags))
                {
                    var type = field.FieldType;

                    if (type == typeof(JobHandle))
                    {
                        m_Dependencies.Add(field);
                        continue;
                    }

                    if (!IsDemandName(field.Name))
                        continue;

                    if (type == typeof(NativeArray<int>))
                    {
                        if (arrayFilter(field.Name))
                            m_Arrays.Add(field);
                    }
                    else if (type.IsGenericType && type.Name.StartsWith("NativeValue", StringComparison.Ordinal) && valueFilter(field.Name))
                        m_Values.Add(field);
                }

                if (m_Values.Count == 0 && m_Arrays.Count == 0)
                    Mod.Log.Warn($"No demand fields found on {system.GetType().Name}; demand override disabled for it.");
                else
                    Mod.Log.Info($"{system.GetType().Name}: overriding {m_Values.Count} value(s) and {m_Arrays.Count} array(s).");
            }

            private static bool IsDemandName(string name)
            {
                foreach (var demandName in kDemandNames)
                {
                    if (name.IndexOf(demandName, StringComparison.Ordinal) >= 0)
                        return true;
                }
                return false;
            }

            /// <param name="levels">Low/medium/high demand; single-level zones use x.</param>
            public void Apply(int3 levels)
            {
                // Make sure the demand jobs have finished before we overwrite their results.
                foreach (var dependency in m_Dependencies)
                    ((JobHandle)dependency.GetValue(m_System)).Complete();

                int average = (levels.x + levels.y + levels.z) / 3;

                foreach (var field in m_Values)
                {
                    // NativeValue wraps a pointer, so writing through the boxed copy
                    // updates the system's real value.
                    object boxed = field.GetValue(m_System);
                    var valueProperty = field.FieldType.GetProperty("value");
                    if (boxed == null || valueProperty == null || !IsCreated(boxed))
                        continue;

                    if (valueProperty.PropertyType == typeof(int3))
                        valueProperty.SetValue(boxed, levels);
                    else if (valueProperty.PropertyType == typeof(int))
                        valueProperty.SetValue(boxed, average);
                }

                foreach (var field in m_Arrays)
                {
                    var array = (NativeArray<int>)field.GetValue(m_System);
                    if (!array.IsCreated)
                        continue;
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (m_IndexFilter(i))
                            array[i] = average;
                    }
                }
            }

            private static bool IsCreated(object nativeValue)
            {
                var property = nativeValue.GetType().GetProperty("IsCreated");
                return property == null || (bool)property.GetValue(nativeValue);
            }
        }
    }
}
