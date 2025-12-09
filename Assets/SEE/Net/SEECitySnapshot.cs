using SEE.Game.City;
using SEE.Utils.Paths;
using Unity.Netcode;

public class SEECitySnapshot : INetworkSerializable
{
    public string ConfigPath;

    public string GraphPath;

    public string LayoutPath;

    public string CityName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ConfigPath);
        serializer.SerializeValue(ref GraphPath);
        serializer.SerializeValue(ref LayoutPath);
        serializer.SerializeValue(ref CityName);
    }
}
