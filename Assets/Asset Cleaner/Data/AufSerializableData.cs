using System;
using System.Linq;

namespace Asset_Cleaner {
    [Serializable]
    class AufSerializableData {
        public const int CurrentVersion = 1;
        public int Version;
        public bool MarkRed;
        public int RebuildCacheOnDemand;
        public int UpdateUnusedAssetsOnDemand;
        public bool ShowInfoBox;
        public string IgnorePathContainsCombined;
        public bool IgnoreMaterial;
        public bool IgnoreScriptable;
        public bool IgnoreSprite;

        static int BoolToInt(bool val) {
            return val ? 2 : 1;
        }

        static bool IntToBool(int val, bool defaultVal) {
            switch (val) {
                case 2:
                    return true;
                case 1:
                    return false;
                default:
                    return defaultVal;
            }
        }

        public static AufSerializableData Default() {
            return new AufSerializableData {
                Version = CurrentVersion,
                MarkRed = true,
                ShowInfoBox = true,
                IgnorePathContainsCombined = "Gizmos;Resources;Editor;Asset Cleaner;Asset Usage Finder;",
                IgnoreMaterial = false,
                IgnoreScriptable = true,
                IgnoreSprite = false,
                RebuildCacheOnDemand = 2,
                UpdateUnusedAssetsOnDemand = 2,
            };
        }

        public static void OnSerialize(in Config src, out AufSerializableData result) {
            result = new AufSerializableData();
            result.Version = CurrentVersion;
            result.MarkRed = src.MarkRed;
            result.ShowInfoBox = src.ShowInfoBox;
            result.IgnorePathContainsCombined = src.IgnorePathContainsCombined;
            result.IgnoreMaterial = src.IgnoreMaterial;
            result.IgnoreScriptable = src.IgnoreScriptable;
            result.IgnoreSprite = src.IgnoreSprite;
            result.RebuildCacheOnDemand = BoolToInt(src.RebuildCacheOnDemand);
            result.UpdateUnusedAssetsOnDemand = BoolToInt(src.UpdateUnusedAssetsOnDemand);
        }

        public static void OnDeserialize(in AufSerializableData src, ref Config result) {
            var def = Default();

            result.MarkRed = src.MarkRed;
            result.IgnorePathContainsCombined = src.IgnorePathContainsCombined;
            result.ShowInfoBox = src.ShowInfoBox;
            result.IgnorePathContains = result.IgnorePathContainsCombined
                .Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            result.IgnoreMaterial = src.IgnoreMaterial;
            result.IgnoreScriptable = src.IgnoreScriptable;
            result.IgnoreSprite = src.IgnoreSprite;
            result.RebuildCacheOnDemand = IntToBool(src.RebuildCacheOnDemand, def.RebuildCacheOnDemand == 2);
            result.UpdateUnusedAssetsOnDemand = IntToBool(src.UpdateUnusedAssetsOnDemand, def.UpdateUnusedAssetsOnDemand == 2);
        }

        public bool Valid() {
            return Version == CurrentVersion || IgnorePathContainsCombined == null;
        }
    }
}