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
        /// <summary>Cheats kept on the buff bar even while off: comma-separated option keys, in pin order.</summary>
        [SettingsUIHidden] public string PinnedCheats { get; set; }

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

        // Unemployed exodus: education levels as bits (1 = uneducated ... 16 = highly educated).
        [SettingsUIHidden] public int ExodusEducation { get; set; }
        [SettingsUIHidden] public bool ExodusMoveAway { get; set; }
        /// <summary>Share of matching households a run takes, 1–100 %, picked at random.</summary>
        [SettingsUIHidden] public int ExodusPercent { get; set; }

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

        public override void SetDefaults()
        {
            ShowToolbarButton = true;
            ShowStatusBar = true;
            KeepAchievementsEnabled = false;
            PanelColor = kDefaultPanelColor;
            ResetCheatsOnLoad = false;
            PinnedCheats = "";

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

            ExodusEducation = 1;
            ExodusMoveAway = false;
            ExodusPercent = 50;

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

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.ShowStatusBar)), "Show buff bar" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.ShowStatusBar)), "Show an icon under the CityForge button for every cheat that is on, plus any you pin. Click an icon to switch that cheat." },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.KeepAchievementsEnabled)), "Keep achievements enabled" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.KeepAchievementsEnabled)), "Let achievements unlock while mods are active. The game will still warn that mods turn achievements off; with this on they unlock anyway, and the CityForge panel's Settings tab shows whether they are active. Can only be changed from the main menu, before a city is loaded." },

                { m_Setting.GetOptionLabelLocaleID(nameof(CityForgeSetting.TogglePanelBinding)), "Open or close the panel" },
                { m_Setting.GetOptionDescLocaleID(nameof(CityForgeSetting.TogglePanelBinding)), "Keyboard shortcut for the CityForge panel while in a city. You can also change it from the panel's Settings tab." },
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
