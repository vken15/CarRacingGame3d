using Unity.Netcode;
using UnityEngine;

public struct DiscoveryResponseData: INetworkSerializable
{
    public ushort Port;
    public string ServerName;
    public ushort CurrentPlayer;
    public ushort MaxPlayer;
    public ushort MapId;
    public ushort MaxRound;
    public ushort CurRound;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Port);
        serializer.SerializeValue(ref ServerName);
        serializer.SerializeValue(ref CurrentPlayer);
        serializer.SerializeValue(ref MaxPlayer);
        serializer.SerializeValue(ref MapId);
        serializer.SerializeValue(ref MaxRound);
        serializer.SerializeValue(ref CurRound);
    }
}
