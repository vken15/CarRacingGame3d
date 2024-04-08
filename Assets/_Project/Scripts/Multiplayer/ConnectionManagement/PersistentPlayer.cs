using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// NetworkBehaviour that represents a player connection and is the "Default Player Prefab" inside Netcode for
    /// GameObjects' (Netcode) NetworkManager. This NetworkBehaviour will contain several other NetworkBehaviours that
    /// should persist throughout the duration of this connection, meaning it will persist between scenes.
    /// </summary>
    /// <remarks>
    /// It is not necessary to explicitly mark this as a DontDestroyOnLoad object as Netcode will handle migrating this
    /// Player object between scene loads.
    /// </remarks>
    [RequireComponent(typeof(NetworkObject))]
    public class PersistentPlayer : NetworkBehaviour
    {
        [SerializeField]
        PersistentPlayerRuntimeCollection persistentPlayerRuntimeCollection;

        [SerializeField]
        NetworkNameState networkNameState;

        public NetworkNameState NetworkNameState => networkNameState;

        public override void OnNetworkSpawn()
        {
            gameObject.name = "PersistentPlayer" + OwnerClientId;

            // Note that this is done here on OnNetworkSpawn in case this NetworkBehaviour's properties are accessed
            // when this element is added to the runtime collection. If this was done in OnEnable() there is a chance
            // that OwnerClientID could be its default value (0).
            persistentPlayerRuntimeCollection.Add(this);
            if (IsServer)
            {
                var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    networkNameState.Name.Value = sessionPlayerData.Value.PlayerName;
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RemovePersistentPlayer();
        }

        public override void OnNetworkDespawn()
        {
            RemovePersistentPlayer();
        }

        void RemovePersistentPlayer()
        {
            persistentPlayerRuntimeCollection.Remove(this);
            if (IsServer)
            {
                var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);

                if (sessionPlayerData.HasValue)
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, sessionPlayerData.Value);
            }
        }
    }
}
