namespace SEE.Controls.Actions
{
    public static class ActionState
    {
        public enum Type
        {
            Move,
            Rotate,
            Map
        }

        private static Type value = 0;
        public static Type Value
        {
            get => value;
            set
            {
                if (ActionState.value != value)
                {
                    ActionState.value = value;
                    OnStateChanged?.Invoke(ActionState.value);
                }
            }
        }

        public delegate void OnStateChangedFn(Type value);
        public static event OnStateChangedFn OnStateChanged;
    }
}
