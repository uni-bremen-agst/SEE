namespace Asset_Cleaner {
    class Config {
        // serialized
        public bool MarkRed;
        public string IgnorePathContainsCombined;
        public bool ShowInfoBox;
        public bool RebuildCacheOnDemand;
        public bool UpdateUnusedAssetsOnDemand;

        // todo make type array
        public bool IgnoreMaterial;
        public bool IgnoreScriptable;
        public bool IgnoreSprite;

        // serialized only while window is opened
        public bool Locked;

        // non-serialized
        public string[] IgnorePathContains;
        public string InitializationTime;
        public bool PendingUpdateUnusedAssets;
    }
}