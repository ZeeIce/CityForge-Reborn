using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game;
using Game.Input;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using CityForgeReborn.Systems;

namespace CityForgeReborn
{
    /// <summary>
    /// Mod options shown in the game's Options menu, plus the hidden cheat state
    /// driven by the in-game panel. Everything here is saved automatically.
    /// </summary>
    /// <remarks>
    /// The class name must stay unique among all mods: the game finds the file to save by
    /// the settings type's name, and many mods call theirs "Setting".
    /// </remarks>
    [FileLocation(nameof(CityForgeReborn))]
    [SettingsUIGroupOrder(kGeneralGroup, kHotkeyGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kHotkeyGroup)]
    [SettingsUIKeyboardAction(kTogglePanelAction, ActionType.Button, usages: new[] { Usages.kDefaultUsage, Usages.kToolUsage, Usages.kCancelableToolUsage, Usages.kDiscardableToolUsage })]
    public class CityForgeSetting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kHotkeyGroup = "Hotkeys";

        public const string kTogglePanelAction = "TogglePanel";

        public const string kDefaultPanelColor = "#161b22";

        public CityForgeSetting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool ShowToolbarButton { get; set; }

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool ShowStatusBar { get; set; }

        /// <summary>The game decides whether achievements count when a city loads, so this can only change in the main menu.</summary>
        [SettingsUISection(kSection, kGeneralGroup)]
        [SettingsUIDisableByCondition(typeof(CityForgeSetting), nameof(IsCityLoaded))]
        public bool KeepAchievementsEnabled { get; set; }

        [SettingsUISection(kSection, kHotkeyGroup)]
        [SettingsUIKeyboardBinding(BindingKeyboard.C, kTogglePanelAction, ctrl: true, shift: true)]
        public ProxyBinding TogglePanelBinding { get; set; }

        [SettingsUISection(kSection, kHotkeyGroup)]
        [SettingsUIButton]
        public bool ResetHotkeys
        {
            set => ResetKeyBindings();
        }

        public bool IsCityLoaded() => GameManager.instance != null && GameManager.instance.gameMode.IsGame();

        /// <summary>Applies at once; the file write goes through <see cref="SettingsSaver"/>, which retries.</summary>
        public override void ApplyAndSave()
        {
            Apply();
            SettingsSaver.Request();
        }

        // ---- Panel state (hidden from the Options menu) ----

        [SettingsUIHidden] public string PanelColor { get; set; }
        [SettingsUIHidden] public bool ResetCheatsOnLoad { get; set; }

        // ---- Money ----

        [SettingsUIHidden] public bool InfiniteMoney { get; set; }

        // ---- Milestones ----

        [SettingsUIHidden] public int MilestoneTarget { get; set; }
        [SettingsUIHidden] public bool KeepMilestonesUnlocked { get; set; }

        // ---- Construction ----

        [SettingsUIHidden] public bool InstantConstruction { get; set; }
        /// <summary>Index into <see cref="Systems.ConstructionCheatSystem.SpeedMultipliers"/> (½×, 1×, 2×, 4×).</summary>
        [SettingsUIHidden] public int BuildSpeed { get; set; }

        // ---- Population & tourism ----

        [SettingsUIHidden] public bool BoostMoveIn { get; set; }
        [SettingsUIHidden] public int MoveInMultiplier { get; set; }
        [SettingsUIHidden] public bool BoostTourists { get; set; }
        [SettingsUIHidden] public int TouristMultiplier { get; set; }
        [SettingsUIHidden] public bool MaxHappiness { get; set; }
        [SettingsUIHidden] public bool RichCitizens { get; set; }
        [SettingsUIHidden] public bool MaxEducation { get; set; }

        // Education distribution, % of citizens per level (normalised when applied).
        [SettingsUIHidden] public int EducationUneducated { get; set; }
        [SettingsUIHidden] public int EducationPoor { get; set; }
        [SettingsUIHidden] public int EducationEducated { get; set; }
        [SettingsUIHidden] public int EducationWell { get; set; }
        [SettingsUIHidden] public int EducationHigh { get; set; }

        // ---- Buildings ----

        [SettingsUIHidden] public bool LevelUpBuildings { get; set; }
        [SettingsUIHidden] public int BuildingLevelTarget { get; set; }
        [SettingsUIHidden] public bool KeepStorageFull { get; set; }
        [SettingsUIHidden] public bool MaxEfficiency { get; set; }

        // ---- City services ----

        [SettingsUIHidden] public bool UnlimitedWater { get; set; }
        [SettingsUIHidden] public bool UnlimitedElectricity { get; set; }
        [SettingsUIHidden] public bool NoPollution { get; set; }

        // ---- Demand (values are 0–100 %) ----

        [SettingsUIHidden] public bool ResidentialOverride { get; set; }
        [SettingsUIHidden] public bool ResidentialForce { get; set; }
        [SettingsUIHidden] public int ResidentialLow { get; set; }
        [SettingsUIHidden] public int ResidentialMedium { get; set; }
        [SettingsUIHidden] public int ResidentialHigh { get; set; }

        [SettingsUIHidden] public bool CommercialOverride { get; set; }
        [SettingsUIHidden] public bool CommercialForce { get; set; }
        [SettingsUIHidden] public int CommercialDemand { get; set; }

        [SettingsUIHidden] public bool IndustrialOverride { get; set; }
        [SettingsUIHidden] public bool IndustrialForce { get; set; }
        [SettingsUIHidden] public int IndustrialDemand { get; set; }

        [SettingsUIHidden] public bool OfficeOverride { get; set; }
        [SettingsUIHidden] public bool OfficeForce { get; set; }
        [SettingsUIHidden] public int OfficeDemand { get; set; }

        /// <summary>Turns every ongoing cheat off (used by "Reset cheats on load").</summary>
        public void DisableAllCheats()
        {
            InfiniteMoney = false;
            KeepMilestonesUnlocked = false;
            InstantConstruction = false;
            BuildSpeed = 1;
            BoostMoveIn = false;
            BoostTourists = false;
            MaxHappiness = false;
            RichCitizens = false;
            MaxEducation = false;
            LevelUpBuildings = false;
            KeepStorageFull = false;
            MaxEfficiency = false;
            UnlimitedWater = false;
            UnlimitedElectricity = false;
            NoPollution = false;
            ResidentialOverride = ResidentialForce = false;
            CommercialOverride = CommercialForce = false;
            IndustrialOverride = IndustrialForce = false;
            OfficeOverride = OfficeForce = false;
        }

        /// <summary>Short labels for every ongoing cheat that is on, for the status bar.</summary>
        public List<string> ActiveCheats()
        {
            var on = new List<string>();
            void Add(bool active, string label)
            {
                if (active) on.Add(label);
            }

            Add(InfiniteMoney, "Infinite money");
            Add(KeepMilestonesUnlocked, "Milestones unlocked");
            Add(InstantConstruction, "Instant build");
            Add(!InstantConstruction && BuildSpeed != 1, $"Build speed {SpeedLabel(BuildSpeed)}");
            Add(BoostMoveIn, $"Move-ins {MoveInMultiplier}×");
            Add(BoostTourists, $"Tourists {TouristMultiplier}×");
            Add(MaxHappiness, "Max happiness");
            Add(RichCitizens, "Rich citizens");
            Add(MaxEducation, "Max education");
            Add(LevelUpBuildings, $"Level {BuildingLevelTarget} buildings");
            Add(KeepStorageFull, "Storage full");
            Add(MaxEfficiency, "Max efficiency");
            Add(UnlimitedWater, "Unlimited water");
            Add(UnlimitedElectricity, "Unlimited electricity");
            Add(NoPollution, "No pollution");
            AddDemand(on, "Residential", ResidentialOverride, ResidentialForce);
            AddDemand(on, "Commercial", CommercialOverride, CommercialForce);
            AddDemand(on, "Industrial", IndustrialOverride, IndustrialForce);
            AddDemand(on, "Office", OfficeOverride, OfficeForce);
            return on;
        }

        private static void AddDemand(List<string> on, string zone, bool overridden, bool force)
        {
            if (overridden) on.Add($"{zone} demand");
            if (force) on.Add($"{zone} Force Build");
        }

        private static string SpeedLabel(int index)
        {
            var speeds = ConstructionCheatSystem.SpeedMultipliers;
            float v = speeds[System.Math.Max(0, System.Math.Min(index, speeds.Length - 1))];
            return v == 0.5f ? "½×" : $"{v:0}×";
        }

        public override void SetDefaults()
        {
            ShowToolbarButton = true;
            ShowStatusBar = true;
            KeepAchievementsEnabled = false;
            PanelColor = kDefaultPanelColor;
            ResetCheatsOnLoad = false;

            InfiniteMoney = false;

            MilestoneTarget = 10;
            KeepMilestonesUnlocked = false;

            InstantConstruction = false;
            BuildSpeed = 1;

            BoostMoveIn = false;
            MoveInMultiplier = 3;
            BoostTourists = false;
            TouristMultiplier = 3;
            MaxHappiness = false;
            RichCitizens = false;
            MaxEducation = false;

            EducationUneducated = 10;
            EducationPoor = 20;
            EducationEducated = 30;
            EducationWell = 25;
            EducationHigh = 15;

            LevelUpBuildings = false;
            BuildingLevelTarget = 5;
            KeepStorageFull = false;
            MaxEfficiency = false;

            UnlimitedWater = false;
            UnlimitedElectricity = false;
            NoPollution = false;

            ResidentialOverride = false;
            ResidentialForce = false;
            ResidentialLow = 50;
            ResidentialMedium = 50;
            ResidentialHigh = 50;

            CommercialOverride = false;
            CommercialForce = false;
            CommercialDemand = 50;

            IndustrialOverride = false;
            IndustrialForce = false;
            IndustrialDemand = 50;

            OfficeOverride = false;
            OfficeForce = false;
            OfficeDemand = 50;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly CityForgeSetting m_Setting;

        public LocaleEN(CityForgeSetting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "CityForge Reborn" },
                { m_Setting.GetOptionTabLocaleID(CityForgeSetting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(CityForgeSetting.kGeneralGroup), "General" },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.ShowToolbarButton)), "Show toolbar button" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.ShowToolbarButton)), "Show the CityForge button in the top-left corner while in a city." },

                { m_Setting.GetOptionGroupLocaleID(CityForgeSetting.kHotkeyGroup), "Hotkeys" },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.ShowStatusBar)), "Show active cheats" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.ShowStatusBar)), "Show a strip next to the CityForge button listing the cheats that are on. Click it to open the panel." },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.KeepAchievementsEnabled)), "Keep achievements enabled" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.KeepAchievementsEnabled)), "Allow Steam/Paradox achievements to unlock while mods are active. Can only be changed from the main menu, before a city is loaded." },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.TogglePanelBinding)), "Open or close the panel" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.TogglePanelBinding)), "Keyboard shortcut for the CityForge panel while in a city." },
                { m_Setting.GetBindingKeyLocaleID(CityForgeSetting.kTogglePanelAction), "Open or close the panel" },
                { m_Setting.GetBindingMapLocaleID(), "CityForge Reborn" },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.ResetHotkeys)), "Reset hotkeys" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.ResetHotkeys)), "Put the CityForge shortcuts back to their defaults." },
            };
        }

        public void Unload()
        {
        }
    }
}
