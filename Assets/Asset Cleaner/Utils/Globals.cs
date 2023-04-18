namespace Asset_Cleaner {
    static class Globals<T> where T : class {
        static T _instance;

        public static T Value {
            get {
                Asr.IsFalse(_instance == null);
                return _instance;
            }
            set {
                var was = HasValue();
                _instance = value;

                // keep counter to check during deinitialization if all Globals are cleared     
                if (was && !HasValue())
                    __GlobalsCounter.Counter -= 1;
                if (!was && HasValue())
                    __GlobalsCounter.Counter += 1;

                bool HasValue() => _instance != null;
            }
        }
    }

    static class __GlobalsCounter {
        internal static int Counter;
        public static bool HasAnyValue() => Counter > 0;
    }
}