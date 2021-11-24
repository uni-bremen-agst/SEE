namespace Dissonance
{
    internal interface ILossEstimator
    {
        float PacketLoss { get; }
    }
}
