using System.Collections.Generic;
using System.Text;
using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CityForgeReborn.Systems
{
    /// <summary>
    /// Unemployed exodus: sends away households where no one has a job, no adult is in
    /// college or university, and every adult has one of the chosen education levels.
    /// Children and teens at school don't keep a family here. Whole households go, so no
    /// children are left behind; retirees-only households are never touched.
    /// "Remove instantly" marks the household Deleted, and the game's own
    /// HouseholdAndCitizenRemoveSystem then removes the residents and frees their home,
    /// school seats, cars and pets. "Move away" adds MovingAway, the same flag the game
    /// sets for unhappy households, so they leave by the normal route.
    /// Both go through the EndFrameBarrier, like the game's own move-away code. This
    /// system runs after the frame's modification phases, and clean-up destroys every
    /// Deleted entity at the end of the frame, so a Deleted added here directly would
    /// destroy the household before HouseholdAndCitizenRemoveSystem saw it, leaving its
    /// citizens pointing at a missing household and crashing the simulation.
    /// Each scan also builds a report for the panel: a breakdown per education level and
    /// a sample of the matching households, so players can see who is going.
    /// Runs on request only, in the UI phase so it also works while the game is paused.
    /// </summary>
    public partial class ExodusSystem : GameSystemBase
    {
        /// <summary>Bit per education level, 0 (uneducated) to 4 (highly educated).</summary>
        public const int kAllLevels = 0b11111;
        private const int kLevels = 5;
        /// <summary>Households listed by name in the panel; the counts always cover all of them.</summary>
        private const int kListed = 40;

        private static readonly string[] kEducation = { "Uneducated", "Poorly educated", "Educated", "Well educated", "Highly educated" };

        private EntityQuery m_HouseholdQuery;
        private NameSystem m_Names;
        private SelectedInfoUISystem m_SelectedInfo;
        private EndFrameBarrier m_EndFrameBarrier;
        // Households queued on the barrier but not yet changed; kept out of scans until then.
        private readonly HashSet<Entity> m_Pending = new HashSet<Entity>();
        // Single households picked in the panel, applied in OnUpdate where the barrier is open.
        private readonly List<Entity> m_OneRequested = new List<Entity>();
        private bool m_CountRequested;
        private bool m_RunRequested;
        // Households sent away by the last "Move away" run, so they can be followed.
        private readonly List<Entity> m_Sent = new List<Entity>();

        /// <summary>Households matching the current filter, or -1 before the first count.</summary>
        public int MatchingHouseholds { get; private set; } = -1;
        public int MatchingCitizens { get; private set; }
        public string LastResult { get; private set; } = "";
        /// <summary>JSON for the panel: per-level breakdown, the matching sample and the sent list.</summary>
        public string Report { get; private set; } = "{}";

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Names = World.GetOrCreateSystemManaged<NameSystem>();
            m_SelectedInfo = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            m_EndFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
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

        /// <summary>How many of <paramref name="count"/> matches a run takes at <paramref name="percent"/>; at least one.</summary>
        public static int Share(int count, int percent) =>
            count == 0 ? 0 : System.Math.Max(1, System.Math.Min(count, (int)System.Math.Round(count * percent / 100.0)));

        /// <summary>Queues one household picked from the panel's list; handled in <see cref="OnUpdate"/>.</summary>
        public void ApplyOne(int index, int version)
        {
            m_OneRequested.Add(new Entity { Index = index, Version = version });
        }

        /// <summary>Sends away (or removes) one household from the panel's list.</summary>
        private void ApplyQueuedOne(Entity household)
        {
            if (!EntityManager.Exists(household) ||
                !EntityManager.HasBuffer<HouseholdCitizen>(household) ||
                EntityManager.HasComponent<Deleted>(household) || EntityManager.HasComponent<MovingAway>(household) ||
                m_Pending.Contains(household))
                return;

            var one = new NativeArray<Entity>(1, Allocator.Temp);
            one[0] = household;
            LastResult = Apply(one, Mod.Settings.ExodusMoveAway, "");
            Mod.Log.Info($"Unemployed exodus, one household: {LastResult}");
            one.Dispose();
            m_CountRequested = true;
        }

        /// <summary>Removes or sends away the given households and describes who went.</summary>
        private string Apply(NativeArray<Entity> households, bool moveAway, string note)
        {
            int residents = 0;
            var adults = new int[kLevels];
            foreach (var household in households)
            {
                var members = EntityManager.GetBuffer<HouseholdCitizen>(household, true);
                residents += members.Length;
                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i].m_Citizen;
                    if (!EntityManager.HasComponent<Citizen>(member))
                        continue;
                    var citizen = EntityManager.GetComponentData<Citizen>(member);
                    int level = citizen.GetEducationLevel();
                    if (citizen.GetAge() == CitizenAge.Adult && level >= 0 && level < kLevels)
                        adults[level]++;
                }
            }

            var buffer = m_EndFrameBarrier.CreateCommandBuffer();
            foreach (var household in households)
            {
                m_Pending.Add(household);
                if (moveAway)
                {
                    buffer.AddComponent(household, new MovingAway { m_Reason = MoveAwayReason.NotHappy });
                    m_Sent.Insert(0, household);
                    if (m_Sent.Count > kListed)
                        m_Sent.RemoveAt(kListed);
                }
                else
                {
                    buffer.AddComponent<Deleted>(household);
                }
            }

            string verb = moveAway ? "sent away" : "removed";
            string what = households.Length == 1 ? "1 household" : $"{households.Length:N0} households";
            return $"{what}{note}, {residents:N0} residents, {verb}: {DescribeAdults(adults)}.";
        }

        /// <summary>Selects a household's first adult and points the camera at them.</summary>
        public void Focus(int index, int version)
        {
            var household = new Entity { Index = index, Version = version };
            if (!EntityManager.Exists(household) || !EntityManager.HasBuffer<HouseholdCitizen>(household))
                return;

            var members = EntityManager.GetBuffer<HouseholdCitizen>(household, true);
            Entity target = Entity.Null;
            for (int i = 0; i < members.Length && target == Entity.Null; i++)
            {
                var c = members[i].m_Citizen;
                if (EntityManager.HasComponent<Citizen>(c) && EntityManager.GetComponentData<Citizen>(c).GetAge() == CitizenAge.Adult)
                    target = c;
            }

            if (target == Entity.Null && members.Length > 0)
                target = members[0].m_Citizen;
            if (target == Entity.Null)
                return;

            m_SelectedInfo.SetSelection(target);
            m_SelectedInfo.Focus(target);
        }

        protected override void OnUpdate()
        {
            if (!m_CountRequested && !m_RunRequested && m_OneRequested.Count == 0)
                return;

            bool run = m_RunRequested;
            m_CountRequested = m_RunRequested = false;

            if (!GameManager.instance.gameMode.IsGame())
            {
                MatchingHouseholds = -1;
                m_Sent.Clear();
                m_Pending.Clear();
                m_OneRequested.Clear();
                return;
            }

            foreach (var household in m_OneRequested)
                ApplyQueuedOne(household);
            m_OneRequested.Clear();

            // Queued changes land at the start of the next frame; until then keep counting.
            m_Pending.RemoveWhere(h => !EntityManager.Exists(h) ||
                EntityManager.HasComponent<Deleted>(h) || EntityManager.HasComponent<MovingAway>(h));
            if (m_Pending.Count > 0)
                m_CountRequested = true;

            var s = Mod.Settings;
            int mask = s.ExodusEducation & kAllLevels;
            var matches = new NativeList<Entity>(Allocator.Temp);
            var scan = Scan(mask, matches);

            if (!run)
            {
                MatchingHouseholds = matches.Length;
                MatchingCitizens = scan.Residents;
                Report = BuildReport(scan, matches);
                matches.Dispose();
                return;
            }

            // "Thanos mode": only a share of the matches, picked at random, so a big
            // exodus can be spread over several runs instead of flooding the roads.
            int take = Share(matches.Length, s.ExodusPercent);
            var random = new Random((uint)System.Environment.TickCount | 1u);
            for (int i = 0; i < take; i++)
            {
                int j = random.NextInt(i, matches.Length);
                (matches[i], matches[j]) = (matches[j], matches[i]);
            }

            m_Sent.Clear();
            var chosen = new NativeArray<Entity>(take, Allocator.Temp);
            NativeArray<Entity>.Copy(matches.AsArray(), chosen, take);
            LastResult = Apply(chosen, s.ExodusMoveAway, $" ({s.ExodusPercent}% of {matches.Length:N0} matching)");
            Mod.Log.Info($"Unemployed exodus: {LastResult} Education mask {mask}.");
            chosen.Dispose();
            matches.Dispose();

            // They are gone (or leaving, which the query excludes), so recount next frame.
            MatchingHouseholds = 0;
            MatchingCitizens = 0;
            m_CountRequested = true;
        }

        private sealed class ScanResult
        {
            public readonly int[] Adults = new int[kLevels];
            public readonly int[] Jobless = new int[kLevels];
            public readonly int[] MatchedAdults = new int[kLevels];
            public int Residents;
        }

        /// <summary>Collects matching households and counts adults per education level across the city.</summary>
        private ScanResult Scan(int mask, NativeList<Entity> matches)
        {
            var result = new ScanResult();
            var levels = new int[kLevels];
            using var households = m_HouseholdQuery.ToEntityArray(Allocator.Temp);
            foreach (var household in households)
            {
                if ((EntityManager.GetComponentData<Household>(household).m_Flags & HouseholdFlags.MovedIn) == 0 ||
                    m_Pending.Contains(household))
                    continue;

                var members = EntityManager.GetBuffer<HouseholdCitizen>(household, true);
                System.Array.Clear(levels, 0, kLevels);
                int adults = 0;
                bool qualifies = true;

                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i].m_Citizen;
                    if (!EntityManager.HasComponent<Citizen>(member))
                        continue;

                    var citizen = EntityManager.GetComponentData<Citizen>(member);
                    var age = citizen.GetAge();
                    bool works = EntityManager.HasComponent<Worker>(member);
                    bool studies = EntityManager.HasComponent<Game.Citizens.Student>(member);

                    // A job anywhere in the family keeps it here, and so does an adult in
                    // college or university. Kids at school don't.
                    if (works || (studies && age == CitizenAge.Adult))
                        qualifies = false;

                    if (age != CitizenAge.Adult)
                        continue;

                    int level = citizen.GetEducationLevel();
                    if (level < 0 || level >= kLevels)
                        continue;

                    result.Adults[level]++;
                    if (!works && !studies)
                        result.Jobless[level]++;
                    if ((mask & (1 << level)) == 0)
                        qualifies = false;
                    levels[level]++;
                    adults++;
                }

                if (!qualifies || adults == 0)
                    continue;

                matches.Add(household);
                result.Residents += members.Length;
                for (int l = 0; l < kLevels; l++)
                    result.MatchedAdults[l] += levels[l];
            }

            return result;
        }

        private static string DescribeAdults(int[] adults)
        {
            var parts = new List<string>();
            for (int l = 0; l < kLevels; l++)
            {
                if (adults[l] > 0)
                    parts.Add($"{adults[l]:N0} {kEducation[l].ToLowerInvariant()}");
            }

            return parts.Count == 0 ? "no adults" : string.Join(", ", parts) + " adults";
        }

        // ---------------------------------------------------------------- report

        private string BuildReport(ScanResult scan, NativeList<Entity> matches)
        {
            var sb = new StringBuilder(4096);
            sb.Append("{\"levels\":[");
            for (int l = 0; l < kLevels; l++)
            {
                if (l > 0) sb.Append(',');
                sb.Append("{\"adults\":").Append(scan.Adults[l])
                  .Append(",\"jobless\":").Append(scan.Jobless[l])
                  .Append(",\"matched\":").Append(scan.MatchedAdults[l]).Append('}');
            }

            sb.Append("],\"list\":[");
            for (int i = 0; i < matches.Length && i < kListed; i++)
            {
                if (i > 0) sb.Append(',');
                AppendHousehold(sb, matches[i]);
            }

            sb.Append("],\"sent\":[");
            bool first = true;
            foreach (var household in m_Sent)
            {
                if (!EntityManager.Exists(household) || EntityManager.HasComponent<Deleted>(household))
                    continue;
                if (!first) sb.Append(',');
                first = false;
                AppendHousehold(sb, household);
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private void AppendHousehold(StringBuilder sb, Entity household)
        {
            sb.Append("{\"i\":").Append(household.Index).Append(",\"v\":").Append(household.Version);
            sb.Append(",\"home\":");
            AppendString(sb, HomeName(household));
            sb.Append(",\"members\":[");

            var members = EntityManager.GetBuffer<HouseholdCitizen>(household, true);
            bool first = true;
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i].m_Citizen;
                if (!EntityManager.HasComponent<Citizen>(member))
                    continue;

                var citizen = EntityManager.GetComponentData<Citizen>(member);
                var age = citizen.GetAge();
                bool works = EntityManager.HasComponent<Worker>(member);
                bool studies = EntityManager.HasComponent<Game.Citizens.Student>(member);
                string status = works ? "Has a job"
                    : studies ? (age == CitizenAge.Adult ? "In college" : "At school")
                    : age == CitizenAge.Adult ? "No job"
                    : age == CitizenAge.Elderly ? "Retired"
                    : "";

                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"name\":");
                AppendString(sb, m_Names.GetRenderedLabelName(member));
                sb.Append(",\"age\":");
                AppendString(sb, age.ToString());
                sb.Append(",\"edu\":").Append(citizen.GetEducationLevel());
                sb.Append(",\"status\":");
                AppendString(sb, status);
                sb.Append('}');
            }

            sb.Append("]}");
        }

        private string HomeName(Entity household)
        {
            if (EntityManager.HasComponent<PropertyRenter>(household))
            {
                var home = EntityManager.GetComponentData<PropertyRenter>(household).m_Property;
                if (home != Entity.Null && EntityManager.Exists(home))
                    return m_Names.GetRenderedLabelName(home);
            }

            return EntityManager.HasComponent<HomelessHousehold>(household) ? "Homeless" : "No home";
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (char ch in value ?? "")
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append(' '); break;
                    default:
                        if (ch < ' ') sb.Append(' ');
                        else sb.Append(ch);
                        break;
                }
            }

            sb.Append('"');
        }
    }
}
