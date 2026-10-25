using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.UI;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Bridge between the React panel and the game. Options are exposed by name
    /// through generic get/set bindings so adding a new toggle or slider only
    /// needs one Register* line here.
    /// </summary>
    public partial class CityForgeUISystem : UISystemBase
    {
        public const string kGroup = "cityforge";

        private static readonly Regex kHexColor = new Regex("^#[0-9a-fA-F]{6}$");

        private readonly Dictionary<string, Action<bool>> m_BoolSetters = new Dictionary<string, Action<bool>>();
        private readonly Dictionary<string, Action<int>> m_IntSetters = new Dictionary<string, Action<int>>();
        private readonly Dictionary<string, Action<string>> m_StringSetters = new Dictionary<string, Action<string>>();

        private MoneyCheatSystem m_Money;
        private MilestoneCheatSystem m_Milestones;
        private CitizenCheatSystem m_Citizens;
        private BuildingCheatSystem m_Buildings;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Money = World.GetOrCreateSystemManaged<MoneyCheatSystem>();
            m_Milestones = World.GetOrCreateSystemManaged<MilestoneCheatSystem>();
            m_Citizens = World.GetOrCreateSystemManaged<CitizenCheatSystem>();
            m_Buildings = World.GetOrCreateSystemManaged<BuildingCheatSystem>();

            var s = Mod.Settings;

            RegisterBool("showToolbarButton", () => s.ShowToolbarButton, v => s.ShowToolbarButton = v);
            RegisterString("panelColor", () => s.PanelColor, v =>
            {
                if (kHexColor.IsMatch(v))
                    s.PanelColor = v;
            });

            RegisterBool("resetCheatsOnLoad", () => s.ResetCheatsOnLoad, v => s.ResetCheatsOnLoad = v);

            RegisterBool("infiniteMoney", () => s.InfiniteMoney, v => s.InfiniteMoney = v);

            RegisterInt("milestoneTarget", () => s.MilestoneTarget, v => s.MilestoneTarget = math.max(1, v));
            RegisterBool("keepMilestonesUnlocked", () => s.KeepMilestonesUnlocked, v => s.KeepMilestonesUnlocked = v);

            RegisterBool("instantConstruction", () => s.InstantConstruction, v => s.InstantConstruction = v);
            RegisterInt("buildSpeed", () => s.BuildSpeed, v => s.BuildSpeed = math.clamp(v, 0, ConstructionCheatSystem.SpeedMultipliers.Length - 1));

            RegisterBool("boostMoveIn", () => s.BoostMoveIn, v => s.BoostMoveIn = v);
            RegisterInt("moveInMultiplier", () => s.MoveInMultiplier, v => s.MoveInMultiplier = math.clamp(v, 2, 10));
            RegisterBool("boostTourists", () => s.BoostTourists, v => s.BoostTourists = v);
            RegisterInt("touristMultiplier", () => s.TouristMultiplier, v => s.TouristMultiplier = math.clamp(v, 2, 10));
            RegisterBool("maxHappiness", () => s.MaxHappiness, v => s.MaxHappiness = v);
            RegisterBool("richCitizens", () => s.RichCitizens, v => s.RichCitizens = v);
            RegisterBool("maxEducation", () => s.MaxEducation, v => s.MaxEducation = v);
            RegisterInt("educationUneducated", () => s.EducationUneducated, v => s.EducationUneducated = Percent(v));
            RegisterInt("educationPoor", () => s.EducationPoor, v => s.EducationPoor = Percent(v));
            RegisterInt("educationEducated", () => s.EducationEducated, v => s.EducationEducated = Percent(v));
            RegisterInt("educationWell", () => s.EducationWell, v => s.EducationWell = Percent(v));
            RegisterInt("educationHigh", () => s.EducationHigh, v => s.EducationHigh = Percent(v));

            RegisterBool("levelUpBuildings", () => s.LevelUpBuildings, v => s.LevelUpBuildings = v);
            RegisterInt("buildingLevelTarget", () => s.BuildingLevelTarget, v => s.BuildingLevelTarget = math.clamp(v, 1, 5));
            RegisterBool("keepStorageFull", () => s.KeepStorageFull, v => s.KeepStorageFull = v);
            RegisterBool("maxEfficiency", () => s.MaxEfficiency, v => s.MaxEfficiency = v);

            RegisterBool("unlimitedWater", () => s.UnlimitedWater, v => s.UnlimitedWater = v);
            RegisterBool("unlimitedElectricity", () => s.UnlimitedElectricity, v => s.UnlimitedElectricity = v);
            RegisterBool("noPollution", () => s.NoPollution, v => s.NoPollution = v);

            RegisterBool("residentialOverride", () => s.ResidentialOverride, v => s.ResidentialOverride = v);
            RegisterBool("residentialForce", () => s.ResidentialForce, v => s.ResidentialForce = v);
            RegisterInt("residentialLow", () => s.ResidentialLow, v => s.ResidentialLow = Percent(v));
            RegisterInt("residentialMedium", () => s.ResidentialMedium, v => s.ResidentialMedium = Percent(v));
            RegisterInt("residentialHigh", () => s.ResidentialHigh, v => s.ResidentialHigh = Percent(v));

            RegisterBool("commercialOverride", () => s.CommercialOverride, v => s.CommercialOverride = v);
            RegisterBool("commercialForce", () => s.CommercialForce, v => s.CommercialForce = v);
            RegisterInt("commercialDemand", () => s.CommercialDemand, v => s.CommercialDemand = Percent(v));

            RegisterBool("industrialOverride", () => s.IndustrialOverride, v => s.IndustrialOverride = v);
            RegisterBool("industrialForce", () => s.IndustrialForce, v => s.IndustrialForce = v);
            RegisterInt("industrialDemand", () => s.IndustrialDemand, v => s.IndustrialDemand = Percent(v));

            RegisterBool("officeOverride", () => s.OfficeOverride, v => s.OfficeOverride = v);
            RegisterBool("officeForce", () => s.OfficeForce, v => s.OfficeForce = v);
            RegisterInt("officeDemand", () => s.OfficeDemand, v => s.OfficeDemand = Percent(v));

            // Read-only game state.
            AddUpdateBinding(new GetterValueBinding<int>(kGroup, "currentMilestone", () => m_Milestones.CurrentMilestone));
            AddUpdateBinding(new GetterValueBinding<int>(kGroup, "maxMilestone", () => m_Milestones.MaxMilestone));

            // Generic setters.
            AddBinding(new TriggerBinding<string, bool>(kGroup, "setBool", (key, value) => Set(m_BoolSetters, key, value)));
            AddBinding(new TriggerBinding<string, int>(kGroup, "setInt", (key, value) => Set(m_IntSetters, key, value)));
            AddBinding(new TriggerBinding<string, string>(kGroup, "setString", (key, value) => Set(m_StringSetters, key, value)));

            // One-shot actions.
            AddBinding(new TriggerBinding<int>(kGroup, "addMoney", amount => m_Money.AddMoney(amount)));
            AddBinding(new TriggerBinding(kGroup, "advanceMilestone", () => m_Milestones.AdvanceTo(s.MilestoneTarget)));
            AddBinding(new TriggerBinding(kGroup, "unlockAllMilestones", () => m_Milestones.UnlockAll()));
            AddBinding(new TriggerBinding<int>(kGroup, "addDevTreePoints", points => m_Milestones.AddDevTreePoints(math.max(0, points))));
            AddBinding(new TriggerBinding(kGroup, "applyEducationDistribution", () => m_Citizens.ApplyEducationDistribution()));
            AddBinding(new TriggerBinding(kGroup, "fillStorage", () => m_Buildings.FillStorage()));
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            var s = Mod.Settings;
            if (mode.IsGame() && s.ResetCheatsOnLoad)
            {
                Mod.Log.Info("Resetting cheats for newly loaded city.");
                s.DisableAllCheats();
                s.ApplyAndSave();
            }
        }

        private static int Percent(int value) => math.clamp(value, 0, 100);

        private void RegisterBool(string key, Func<bool> getter, Action<bool> setter)
        {
            AddUpdateBinding(new GetterValueBinding<bool>(kGroup, key, getter));
            m_BoolSetters[key] = setter;
        }

        private void RegisterInt(string key, Func<int> getter, Action<int> setter)
        {
            AddUpdateBinding(new GetterValueBinding<int>(kGroup, key, getter));
            m_IntSetters[key] = setter;
        }

        private void RegisterString(string key, Func<string> getter, Action<string> setter)
        {
            AddUpdateBinding(new GetterValueBinding<string>(kGroup, key, getter));
            m_StringSetters[key] = setter;
        }

        private static void Set<T>(Dictionary<string, Action<T>> setters, string key, T value)
        {
            if (!setters.TryGetValue(key, out var setter))
            {
                Mod.Log.Warn($"Unknown option '{key}'.");
                return;
            }

            setter(value);
            Mod.Settings.ApplyAndSave();
        }
    }
}
