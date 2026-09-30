using System;
using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    public partial class Main
    {
        private const string ChronicleCategoryPolitics = "politics";
        private const string ChronicleCategoryParties = "parties";
        private const string ChronicleCategoryIdeology = "ideology";
        private const string ChronicleCategoryCrises = "crises";
        private const string ChronicleCategoryWar = "war";
        private const string ChronicleCategoryInternational = "international";

        private enum PoliticalChronicleImportance
        {
            Low = 0,
            Normal = 1,
            Major = 2,
            Historic = 3
        }

        private sealed class PoliticalChronicleEntry
        {
            public long Id;
            public int Year;
            public string EventKey;
            public string Category;
            public PoliticalChronicleImportance Importance;
            public string Text;
            public string IconPath;

            public string KingdomId;
            public string KingdomName;
            public string CityId;
            public string CityName;
            public string ActorId;
            public string ActorName;

            public List<string> Causes = new List<string>();
            public List<string> Consequences = new List<string>();
        }

        private static readonly List<PoliticalChronicleEntry>
            PoliticalChronicleEntries = new List<PoliticalChronicleEntry>();
        private static long _nextPoliticalChronicleEntryId = 1;

        private static void ResetPoliticalChronicle()
        {
            PoliticalChronicleEntries.Clear();
            _nextPoliticalChronicleEntryId = 1;
        }

        private static void CopyChronicleDetails(
            IList<string> source,
            List<string> destination
        )
        {
            if (source == null || destination == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string value = source[i];
                if (!string.IsNullOrEmpty(value))
                {
                    destination.Add(value);
                }
            }
        }

        private static void AddPoliticalChronicleEntry(
            string text,
            Kingdom kingdom,
            City city,
            Actor actor,
            string iconPath,
            string eventKey,
            IList<string> causes,
            IList<string> consequences
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            PoliticalChronicleEntry entry = new PoliticalChronicleEntry();
            entry.Id = _nextPoliticalChronicleEntryId++;
            entry.Year = Math.Max(0, GetWorldYearSafe());
            entry.EventKey = eventKey ?? "";
            entry.Category = ResolvePoliticalChronicleCategory(eventKey);
            entry.Importance = ResolvePoliticalChronicleImportance(eventKey);
            entry.Text = text;
            entry.IconPath = string.IsNullOrEmpty(iconPath)
                ? PoliticsIconPath
                : iconPath;

            entry.KingdomId = GetStableObjectIdentity(kingdom);
            entry.KingdomName = kingdom == null
                ? ""
                : GetWorldObjectDisplayName(kingdom);
            entry.CityId = GetStableObjectIdentity(city);
            entry.CityName = city == null
                ? ""
                : GetWorldObjectDisplayName(city);
            entry.ActorId = GetStableObjectIdentity(actor);
            entry.ActorName = actor == null
                ? ""
                : GetWorldObjectDisplayName(actor);

            CopyChronicleDetails(causes, entry.Causes);
            CopyChronicleDetails(consequences, entry.Consequences);

            PoliticalChronicleEntries.Add(entry);

            // 1.8.0-dev6: map_stats.custom_data is saved by WorldBox with
            // the world. Persist the new entry immediately so a save made
            // right after a political event still contains that event.
            PersistPoliticalChronicleEntry(entry);
        }

        private static string ResolvePoliticalChronicleCategory(
            string eventKey
        )
        {
            string key = eventKey ?? "";

            if (
                key.StartsWith("war_") ||
                key.StartsWith("diplomatic_") ||
                key.StartsWith("peace_")
            )
            {
                return ChronicleCategoryWar;
            }

            if (
                key.StartsWith("bloc_") ||
                key.StartsWith("summit_")
            )
            {
                return ChronicleCategoryInternational;
            }

            if (
                key.StartsWith("party_") ||
                key.StartsWith("movement_") ||
                key.StartsWith("ideological_party_split_") ||
                key.StartsWith("party_congress") ||
                key.StartsWith("general_secretary_")
            )
            {
                return ChronicleCategoryParties;
            }

            if (
                key.StartsWith("crisis_") ||
                key.StartsWith("rebellion_") ||
                key.StartsWith("coup_") ||
                key.StartsWith("revolution_") ||
                key.StartsWith("peaceful_transition_")
            )
            {
                return ChronicleCategoryCrises;
            }

            if (
                key.StartsWith("ideology_") ||
                key.StartsWith("current_changed_") ||
                key.StartsWith("state_ideology_") ||
                key.StartsWith("city_ideology_")
            )
            {
                return ChronicleCategoryIdeology;
            }

            return ChronicleCategoryPolitics;
        }

        private static PoliticalChronicleImportance
            ResolvePoliticalChronicleImportance(string eventKey)
        {
            string key = eventKey ?? "";

            if (
                key.StartsWith("war_hostilities_") ||
                key.StartsWith("rebellion_started_") ||
                key.StartsWith("government_changed_") ||
                key.StartsWith("coup_") ||
                key.StartsWith("revolution_") ||
                key.StartsWith("bloc_founded_") ||
                key.StartsWith("bloc_dissolved_")
            )
            {
                return PoliticalChronicleImportance.Historic;
            }

            if (
                key.StartsWith("election_result_") ||
                key.StartsWith("party_split_") ||
                key.StartsWith("ideological_party_split_") ||
                key.StartsWith("crisis_started_") ||
                key.StartsWith("crisis_accepted_") ||
                key.StartsWith("diplomatic_escalation_") ||
                key.StartsWith("war_surprise_") ||
                key.StartsWith("war_truce_") ||
                key.StartsWith("bloc_joined_") ||
                key.StartsWith("bloc_left_") ||
                key.StartsWith("bloc_evolved_") ||
                key.StartsWith("bloc_collective_defense_") ||
                key.StartsWith("leadership_succession_") ||
                key.StartsWith("political_succession_")
            )
            {
                return PoliticalChronicleImportance.Major;
            }

            if (
                key.StartsWith("ideology_evolution_") ||
                key.StartsWith("summit_vote_") ||
                key.StartsWith("summit_opened_") ||
                key.StartsWith("summit_cancelled_") ||
                key.StartsWith("crisis_escalated_") ||
                key.StartsWith("crisis_ended_")
            )
            {
                return PoliticalChronicleImportance.Low;
            }

            return PoliticalChronicleImportance.Normal;
        }
    }
}
