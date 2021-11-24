using System;

namespace Dissonance.Datastructures
{
    internal class PacketLossCalculator
        : BaseWindowCalculator<bool>
    {
        private uint _lost;

        public float PacketLoss
        {
            get
            {
                if (Count <= 0)
                    return 0;

                return Math.Min(1, Math.Max(0, _lost / (float)Count));
            }
        }

        public PacketLossCalculator(uint size)
            : base(size)
        {
        }

        protected override void Updated(bool? removed, bool added)
        {
            if (removed.HasValue && !removed.Value)
                _lost--;

            if (!added)
                _lost++;
        }

        public override void Clear()
        {
            base.Clear();

            _lost = 0;
        }
    }
}
