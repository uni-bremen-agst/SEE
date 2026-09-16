namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Owns context-local reconstruction plans and their exactly-once attempt
    /// state, keyed by complete P3 binary identity.
    /// </summary>
    internal sealed class ExternalSupportingSourceReconstructionCatalog
    {
        /// <summary>
        /// Serializes registration and attempt-state transitions.
        /// </summary>
        private readonly object gate = new();

        /// <summary>
        /// Stores plans by exact binary identity.
        /// </summary>
        private readonly Dictionary<ExternalAssemblyReferenceDescriptor, Entry> entries = new();

        /// <summary>
        /// Registers one immutable plan without performing candidate I/O.
        /// </summary>
        /// <param name="plan">The plan to register.</param>
        /// <returns>
        /// <see langword="true"/> for a new or semantically idempotent plan;
        /// otherwise <see langword="false"/> for a conflicting plan.
        /// </returns>
        public bool TryRegister(ExternalSupportingSourceReconstructionPlan plan)
        {
            if (plan == null)
            {
                return false;
            }

            lock (gate)
            {
                if (entries.TryGetValue(plan.TargetAssembly, out Entry? existing))
                {
                    return existing.Plan.Equals(plan);
                }

                entries.Add(plan.TargetAssembly, new Entry(plan));
                return true;
            }
        }

        /// <summary>
        /// Atomically begins the only allowed attempt for one exact identity.
        /// </summary>
        /// <param name="targetAssembly">The complete P3 binary identity.</param>
        /// <param name="plan">The registered plan when an attempt begins.</param>
        /// <returns>
        /// <see langword="true"/> only for the first lookup of a registered,
        /// not-yet-attempted plan; otherwise <see langword="false"/>.
        /// </returns>
        public bool TryBegin(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            out ExternalSupportingSourceReconstructionPlan plan)
        {
            if (targetAssembly == null)
            {
                plan = null!;
                return false;
            }

            lock (gate)
            {
                if (!entries.TryGetValue(targetAssembly, out Entry? entry)
                    || entry.State != AttemptState.NotAttempted)
                {
                    plan = null!;
                    return false;
                }

                entry.State = AttemptState.InProgress;
                plan = entry.Plan;
                return true;
            }
        }

        /// <summary>
        /// Completes an in-progress attempt as successful or failed.
        /// </summary>
        /// <param name="targetAssembly">The attempted P3 identity.</param>
        /// <param name="succeeded">Whether reconstruction and P6A registration succeeded.</param>
        public void Complete(
            ExternalAssemblyReferenceDescriptor targetAssembly,
            bool succeeded)
        {
            if (targetAssembly == null)
            {
                return;
            }

            lock (gate)
            {
                if (entries.TryGetValue(targetAssembly, out Entry? entry)
                    && entry.State == AttemptState.InProgress)
                {
                    entry.State = succeeded
                        ? AttemptState.Succeeded
                        : AttemptState.Failed;
                }
            }
        }

        /// <summary>
        /// Stores one plan and its minimal mutable state.
        /// </summary>
        private sealed class Entry
        {
            /// <summary>
            /// Initializes a not-attempted plan entry.
            /// </summary>
            /// <param name="plan">The immutable plan.</param>
            public Entry(ExternalSupportingSourceReconstructionPlan plan)
            {
                Plan = plan;
            }

            /// <summary>
            /// Gets the immutable plan.
            /// </summary>
            /// <value>The exact registered reconstruction plan.</value>
            public ExternalSupportingSourceReconstructionPlan Plan { get; }

            /// <summary>
            /// Gets or sets the attempt state guarded by <see cref="gate"/>.
            /// </summary>
            /// <value>The current exactly-once attempt state.</value>
            public AttemptState State { get; set; }
        }

        /// <summary>
        /// Represents the exactly-once lifecycle for one plan.
        /// </summary>
        private enum AttemptState
        {
            /// <summary>
            /// The prepared plan has not been used.
            /// </summary>
            NotAttempted,

            /// <summary>
            /// One caller currently owns the only reconstruction attempt.
            /// </summary>
            InProgress,

            /// <summary>
            /// Reconstruction and P6A registration succeeded.
            /// </summary>
            Succeeded,

            /// <summary>
            /// Reconstruction or P6A registration failed closed.
            /// </summary>
            Failed,
        }
    }
}
