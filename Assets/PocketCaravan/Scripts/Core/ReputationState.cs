using System;
using System.Collections.Generic;
using System.IO;

namespace PocketCaravan
{
    // These numbers are stable serialization identifiers, not reputation score thresholds.
    public enum ReputationScope { Civilization = 0, Town = 1 }
    public enum ReputationStanding { Hostile = 0, Wary = 1, Neutral = 2, Friendly = 3, Trusted = 4, Respected = 5 }

    /// <summary>Character-owned reputation. No balance thresholds or reward effects are chosen here.</summary>
    public sealed class ReputationState
    {
        public const int FirstSaveVersion = 3;
        [Serializable]
        public class Entry
        {
            public ReputationScope scope;
            public string targetId;
            public ReputationStanding standing = ReputationStanding.Neutral;
            // Nonnegative progress within the named standing, not an absolute world-wide score.
            public int progress;
        }

        [Serializable]
        public class Save
        {
            // Zero is an absent-block sentinel: Unity may materialize missing inline objects.
            public int schemaVersion;
            public string homeCivilizationId;
            public List<Entry> entries = new List<Entry>();
        }

        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        public string HomeCivilizationId { get; private set; }

        static string Key(ReputationScope scope, string id) => ((int)scope).ToString() + ":" + id;
        static Entry Copy(Entry entry) => new Entry { scope = entry.scope, targetId = entry.targetId,
            standing = entry.standing, progress = entry.progress };

        static void ValidateTarget(ReputationScope scope, string id)
        {
            if ((scope != ReputationScope.Civilization && scope != ReputationScope.Town) || string.IsNullOrWhiteSpace(id))
                throw new InvalidDataException("평판 대상 종류 또는 ID가 올바르지 않습니다.");
        }

        public static string HomeOf(OriginData origin)
        {
            if (origin == null) return null;
            // Unintegrated origins have no RegionData yet; retain their existing origin ID verbatim.
            string id = origin.homeRegion != null ? origin.homeRegion.id : origin.id;
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidDataException("출신 문명 ID가 없습니다.");
            return id;
        }

        public void Initialize(string homeCivilizationId)
        {
            if (homeCivilizationId != null && string.IsNullOrWhiteSpace(homeCivilizationId))
                throw new InvalidDataException("출신 문명 ID가 올바르지 않습니다.");
            entries.Clear();
            HomeCivilizationId = homeCivilizationId;
            if (homeCivilizationId != null)
                Set(ReputationScope.Civilization, homeCivilizationId, ReputationStanding.Friendly, 0);
        }

        /// <summary>Unrecorded civilizations/towns are neutral. Reading never creates a reward or record.</summary>
        public Entry Get(ReputationScope scope, string targetId)
        {
            ValidateTarget(scope, targetId);
            return entries.TryGetValue(Key(scope, targetId), out var value) ? Copy(value)
                : new Entry { scope = scope, targetId = targetId, standing = ReputationStanding.Neutral, progress = 0 };
        }

        /// <summary>Data assignment only. R2/R6 must supply approved award/threshold rules before gameplay calls this.</summary>
        public void Set(ReputationScope scope, string targetId, ReputationStanding standing, int progress)
        {
            ValidateTarget(scope, targetId);
            if (standing < ReputationStanding.Hostile || standing > ReputationStanding.Respected || progress < 0)
                throw new InvalidDataException("평판 단계 또는 진행점이 올바르지 않습니다.");
            entries[Key(scope, targetId)] = new Entry { scope = scope, targetId = targetId, standing = standing, progress = progress };
        }

        public Save Export()
        {
            var saved = new Save { schemaVersion = 1, homeCivilizationId = HomeCivilizationId };
            foreach (var value in entries.Values) saved.entries.Add(Copy(value));
            saved.entries.Sort((a, b) => a.scope != b.scope ? a.scope.CompareTo(b.scope) : StringComparer.Ordinal.Compare(a.targetId, b.targetId));
            return saved;
        }

        public static void Validate(Save saved)
        {
            if (saved == null || saved.schemaVersion != 1 || saved.entries == null)
                throw new InvalidDataException("지원하지 않거나 누락된 평판 저장입니다.");
            if (saved.homeCivilizationId != null && string.IsNullOrWhiteSpace(saved.homeCivilizationId))
                throw new InvalidDataException("출신 문명 저장 ID가 올바르지 않습니다.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in saved.entries)
            {
                if (value == null) throw new InvalidDataException("평판 저장 항목이 없습니다.");
                ValidateTarget(value.scope, value.targetId);
                if (value.standing < ReputationStanding.Hostile || value.standing > ReputationStanding.Respected || value.progress < 0
                    || !keys.Add(Key(value.scope, value.targetId)))
                    throw new InvalidDataException("평판 단계·진행점·중복 항목을 확인하세요.");
            }
            if (saved.homeCivilizationId != null && !keys.Contains(Key(ReputationScope.Civilization, saved.homeCivilizationId)))
                throw new InvalidDataException("출신 문명의 평판 기록이 없습니다.");
        }

        /// <summary>Validate everything before replacing live state; unknown valid IDs remain preserved.</summary>
        public void Import(Save saved)
        {
            Validate(saved);
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var value in saved.entries) next.Add(Key(value.scope, value.targetId), Copy(value));
            entries.Clear();
            foreach (var pair in next) entries.Add(pair.Key, pair.Value);
            HomeCivilizationId = saved.homeCivilizationId;
        }

        public void ExportTo(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.version = SaveSystem.CurrentVersion;
            data.reputation = Export();
        }

        /// <summary>Pre-R1 files had no origin selection/record. Initialize once from configured start origin.</summary>
        public void ImportFrom(SaveData data, OriginData configuredOrigin)
        {
            ValidateSaveBlock(data);
            if (data.version < FirstSaveVersion) Initialize(HomeOf(configuredOrigin));
            else Import(data.reputation);
        }

        public static void ValidateSaveBlock(SaveData data)
        {
            if (data == null || data.version < 1 || data.version > SaveSystem.CurrentVersion)
                throw new InvalidDataException("지원하지 않는 평판 저장 버전입니다.");
            if (data.version < FirstSaveVersion)
            {
                if (data.reputation != null && (data.reputation.schemaVersion != 0
                    || !string.IsNullOrEmpty(data.reputation.homeCivilizationId)
                    || (data.reputation.entries != null && data.reputation.entries.Count != 0)))
                    throw new InvalidDataException("이전 버전 표기의 저장에 새 평판 기록이 있습니다. 원본을 보존합니다.");
            }
            else Validate(data.reputation);
        }
    }
}
