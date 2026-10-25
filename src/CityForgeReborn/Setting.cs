using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace CityForgeReborn
{
    /// <summary>
    /// Mod options shown in the game's Options menu, plus the hidden cheat state
    /// driven by the in-game panel. Everything here is saved automatically.
    /// </summary>
    [FileLocation(nameof(CityForgeReborn))]
    [SettingsUIGroupOrder(kGeneralGroup)]
    [SettingsUIShowGroupName(kGeneralGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";

        public const string kDefaultPanelColor = "#161b22";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool ShowToolbarButton { get; set; }

        // ---- Panel state (hidden from the Options menu) ----

        [SettingsUIHidden] public string PanelColor { get; set; }

        // ---- Money ----

        [SettingsUIHidden] public bool InfiniteMoney { get; set; }

        // ---- Milestones ----

        [SettingsUIHidden] public int MilestoneTarget { get; set; }
        [SettingsUIHidden] public bool KeepMilestonesUnlocked { get; set; }

        // ---- Construction ----

        [SettingsUIHidden] public bool InstantConstruction { get; set; }
        /// <summary>Index into <see cref="Systems.ConstructionCheatSystem.SpeedMultipliers"/> (½×, 1×, 2×, 4×).</summary>
        [SettingsUIHidden] public int BuildSpeed { get; set; }

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

        public override void SetDefaults()
        {
            ShowToolbarButton = true;
            PanelColor = kDefaultPanelColor;

            InfiniteMoney = false;

            MilestoneTarget = 10;
            KeepMilestonesUnlocked = false;

            InstantConstruction = false;
            BuildSpeed = 1;

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
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "CityForge Reborn" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGeneralGroup), "General" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ShowToolbarButton)), "Show toolbar button" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ShowToolbarButton)), "Show the CityForge button in the top-left corner while in a city." },
            };
        }

        public void Unload()
        {
        }
    }
}
