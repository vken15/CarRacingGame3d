using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Common data and RPCs for the room state.
    /// </summary>
    public class NetworkRoom : NetworkBehaviour
    {
        /// <summary>
        /// Describes one of the players in the lobby, and their current character-select status.
        /// </summary>
        /// <remarks>
        /// Putting FixedString inside an INetworkSerializeByMemcpy struct is not recommended because it will lose the
        /// bandwidth optimization provided by INetworkSerializable -- an empty FixedString128Bytes serialized normally
        /// or through INetworkSerializable will use 4 bytes of bandwidth, but inside an INetworkSerializeByMemcpy, that
        /// same empty value would consume 132 bytes of bandwidth. 
        /// </remarks>
        public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
        {
            public ulong ClientId;

            private FixedPlayerName m_PlayerName;

            public ushort PlayerNumber; // this player's assigned "P#". (1=P1, 2=P2, etc.)
            public int SeatId; // the latest seat they were in. -1 means none
            public ushort CarId;
            public SeatState SeatState;

            public LobbyPlayerState(ulong clientId, string name, ushort playerNumber, SeatState state, int seatId = -1, ushort carId = 1)
            {
                ClientId = clientId;
                PlayerNumber = playerNumber;
                SeatState = state;
                SeatId = seatId;
                CarId = carId;
                m_PlayerName = new FixedPlayerName();

                PlayerName = name;
            }

            public string PlayerName
            {
                get => m_PlayerName;
                private set => m_PlayerName = value;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ClientId);
                serializer.SerializeValue(ref m_PlayerName);
                serializer.SerializeValue(ref PlayerNumber);
                serializer.SerializeValue(ref SeatState);
                serializer.SerializeValue(ref SeatId);
            }

            public bool Equals(LobbyPlayerState other)
            {
                return ClientId == other.ClientId &&
                       m_PlayerName.Equals(other.m_PlayerName) &&
                       PlayerNumber == other.PlayerNumber &&
                       SeatId == other.SeatId &&
                       SeatState == other.SeatState;
            }
        }

        private NetworkList<LobbyPlayerState> m_LobbyPlayers;

        private void Awake()
        {
            m_LobbyPlayers = new NetworkList<LobbyPlayerState>();
        }

        /// <summary>
        /// Current state of all players in the lobby.
        /// </summary>
        public NetworkList<LobbyPlayerState> LobbyPlayers => m_LobbyPlayers;

        /// <summary>
        /// When this becomes true, the lobby is closed and in process of terminating (switching to gameplay).
        /// </summary>
        public NetworkVariable<bool> IsLobbyClosed { get; } = new NetworkVariable<bool>(false);

        /// <summary>
        /// Server notification when a client requests a different lobby-seat, or locks in their seat choice
        /// </summary>
        public event Action<ulong, ushort, bool> OnClientReady;

        /// <summary>
        /// RPC to notify the server that a client has chosen a seat.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void ChangeSeatServerRpc(ulong clientId, ushort carId, bool lockedIn)
        {
            OnClientReady?.Invoke(clientId, carId, lockedIn);
        }
    }
}
