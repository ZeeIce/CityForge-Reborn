using Game;
using Game.Agents;
using Game.Citizens;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Unemployed exodus: sends away households where nobody works or studies and
    /// every adult has one of the chosen education levels. Whole households go, so no
    /// children are left behind; retirees-only households are never touched.
    /// "Remove instantly" marks the household Deleted, and the game's own
    /// HouseholdAndCitizenRemoveSystem then removes the residents and frees their home,
    /// school seats, cars and pets. "Move away" adds MovingAway, the same flag the game
    /// sets for unhappy households, so they leave by the normal route.
    /// Runs on request only, in the UI phase so it also works while the game is paused.
    /// </summary>
    public partial class ExodusSystem : GameSystemBase
    {
        /// <summary>Bit per education level, 0 (uneducated) to 4 (highly educated).</summary>
        public const int kAllLevels = 0b11111;

        private EntityQuery m_HouseholdQuery;
        private bool m_CountRequested;
        private bool m_RunRequested;

        /// <summary>Households matching the current filter, or -1 before the first count.</summary>
        public int MatchingHouseholds { get; private set; } = -1;
        public int MatchingCitizens { get; private set; }
        public string LastResult { get; private set; } = "";

        protected override void OnCreate()
        {
            base.OnCreate();
            m_HouseholdQuery = GetEntityQuery(
                ComponentType.ReadOnly<Household>(),
                ComponentType.ReadOnly<HouseholdCitizen>(),
                ComponentType.Exclude<TouristHousehold>(),
                ComponentType.Exclude<CommuterHousehold>(),
                ComponentType.Exclude<MovingAway>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
        }

        public void RequestCount() => m_CountRequested = true;

        public void RequestRun() => m_RunRequested = true;

        protected override void OnUpdate()
        {
            if (!m_CountRequested && !m_RunRequested)
                return;

            bool run = m_RunRequested;
            m_CountRequested = m_RunRequested = false;

            if (!GameManager.instance.gameMode.IsGame())
            {
                MatchingHouseholds = -1;
                return;
            }

            var s = Mod.Settings;
            var matches = new NativeList<Entity>(Allocator.Temp);
            int citizens = Find(s.ExodusEducation & kAllLevels, matches);

            if (!run)
            {
                MatchingHouseholds = matches.Length;
                MatchingCitizens = citizens;
                matches.Dispose();
                return;
            }

            if (matches.Length > 0)
            {
                if (s.ExodusMoveAway)
                {
                    foreach (var household in matches)
                        EntityManager.AddComponentData(household, new MovingAway { m_Reason = MoveAwayReason.NotHappy });
                }
                else
                {
                    EntityManager.AddComponent<Deleted>(matches.AsArray());
                }
            }

            string verb = s.ExodusMoveAway ? "sent away" : "removed";
            LastResult = $"{matches.Length:N0} households ({citizens:N0} residents) {verb}.";
            Mod.Log.Info($"Unemployed exodus: {LastResult} Education mask {s.ExodusEducation}.");
            matches.Dispose();

            // They are gone (or leaving, which the query excludes), so recount next frame.
            MatchingHouseholds = 0;
            MatchingCitizens = 0;
            m_CountRequested = true;
        }

        /// <summary>Collects matching households and returns how many residents they hold.</summary>
        private int Find(int educationMask, NativeList<Entity> matches)
        {
            int citizens = 0;
            using var households = m_HouseholdQuery.ToEntityArray(Allocator.Temp);
            foreach (var household in households)
            {
                if ((EntityManager.GetComponentData<Household>(household).m_Flags & HouseholdFlags.MovedIn) == 0)
                    continue;

                var members = EntityManager.GetBuffer<HouseholdCitizen>(household, true);
                if (Qualifies(members, educationMask))
                {
                    matches.Add(household);
                    citizens += members.Length;
                }
            }

            return citizens;
        }

        private bool Qualifies(DynamicBuffer<HouseholdCitizen> members, int educationMask)
        {
            int adults = 0;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i].m_Citizen;
                if (!EntityManager.HasComponent<Citizen>(member))
                    continue;

                // Anyone with a job or a school place keeps the household here.
                if (EntityManager.HasComponent<Worker>(member) || EntityManager.HasComponent<Student>(member))
                    return false;

                var citizen = EntityManager.GetComponentData<Citizen>(member);
                if (citizen.GetAge() != CitizenAge.Adult)
                    continue;

                if ((educationMask & (1 << citizen.GetEducationLevel())) == 0)
                    return false;

                adults++;
            }

            return adults > 0;
        }
    }
}
