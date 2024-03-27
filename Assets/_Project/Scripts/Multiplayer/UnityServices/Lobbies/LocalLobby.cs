using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace CarRacingGame3d
{
    [Serializable]
    public sealed class LocalLobby
    {
        public static LocalLobby Instance => instance ??= new LocalLobby();

        static LocalLobby instance;

        public event Action<LocalLobby> Changed;

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public static List<LocalLobby> CreateLocalLobbies(QueryResponse response)
        {
            var retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
            {
                retLst.Add(Create(lobby));
            }
            return retLst;
        }

        public static LocalLobby Create(Lobby lobby)
        {
            var data = new LocalLobby();
            data.ApplyRemoteData(lobby);
            return data;
        }

        Dictionary<string, LocalLobbyUser> m_LobbyUsers = new();
        public Dictionary<string, LocalLobbyUser> LobbyUsers => m_LobbyUsers;

        public struct LobbyData
        {
            public string LobbyID { get; set; }
            public string LobbyCode { get; set; }
            public string RelayJoinCode { get; set; }
            public string LobbyName { get; set; }
            public bool Private { get; set; }
            public int MaxPlayerCount { get; set; }
            public ushort MapId { get; set; }
            public ushort MaxRound { get; set; }
            public ushort CurRound { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayJoinCode = existing.RelayJoinCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
                MapId = existing.MapId;
                MaxRound = existing.MaxRound;
                CurRound = existing.CurRound;
            }

            public LobbyData(string lobbyCode)
            {
                LobbyID = null;
                LobbyCode = lobbyCode;
                RelayJoinCode = null;
                LobbyName = null;
                Private = false;
                MaxPlayerCount = -1;
                MapId = 0;
                MaxRound = 0;
                CurRound = 0;
            }
        }

        LobbyData m_Data;
        public LobbyData Data => new(m_Data);

        public void AddUser(LocalLobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                DoAddUser(user);
                OnChanged();
            }
        }

        void DoAddUser(LocalLobbyUser user)
        {
            m_LobbyUsers.Add(user.ID, user);
            user.Changed += OnChangedUser;
        }

        public void RemoveUser(LocalLobbyUser user)
        {
            DoRemoveUser(user);
            OnChanged();
        }

        void DoRemoveUser(LocalLobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
            user.Changed -= OnChangedUser;
        }

        void OnChangedUser(LocalLobbyUser user)
        {
            OnChanged();
        }

        void OnChanged()
        {
            Changed?.Invoke(this);
        }

        public string LobbyID
        {
            get => m_Data.LobbyID;
            set
            {
                m_Data.LobbyID = value;
                OnChanged();
            }
        }

        public string LobbyCode
        {
            get => m_Data.LobbyCode;
            set
            {
                m_Data.LobbyCode = value;
                OnChanged();
            }
        }

        public string RelayJoinCode
        {
            get => m_Data.RelayJoinCode;
            set
            {
                m_Data.RelayJoinCode = value;
                OnChanged();
            }
        }

        public string LobbyName
        {
            get => m_Data.LobbyName;
            set
            {
                m_Data.LobbyName = value;
                OnChanged();
            }
        }

        public bool Private
        {
            get => m_Data.Private;
            set
            {
                m_Data.Private = value;
                OnChanged();
            }
        }

        public int PlayerCount => m_LobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => m_Data.MaxPlayerCount;
            set
            {
                m_Data.MaxPlayerCount = value;
                OnChanged();
            }
        }

        public ushort MapId
        {
            get => m_Data.MapId;
            set
            {
                m_Data.MapId = value;
                OnChanged();
            }
        }

        public ushort CurRound
        {
            get => m_Data.CurRound;
            set
            {
                m_Data.CurRound = value;
                OnChanged();
            }
        }

        public ushort MaxRound
        {
            get => m_Data.MaxRound;
            set
            {
                m_Data.MaxRound = value;
                OnChanged();
            }
        }

        public void CopyDataFrom(LobbyData data, Dictionary<string, LocalLobbyUser> currUsers)
        {
            m_Data = data;

            if (currUsers == null)
            {
                m_LobbyUsers = new Dictionary<string, LocalLobbyUser>();
            }
            else
            {
                List<LocalLobbyUser> toRemove = new();
                foreach (var oldUser in m_LobbyUsers)
                {
                    if (currUsers.ContainsKey(oldUser.Key))
                    {
                        oldUser.Value.CopyDataFrom(currUsers[oldUser.Key]);
                    }
                    else
                    {
                        toRemove.Add(oldUser.Value);
                    }
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var currUser in currUsers)
                {
                    if (!m_LobbyUsers.ContainsKey(currUser.Key))
                    {
                        DoAddUser(currUser.Value);
                    }
                }
            }

            OnChanged();
        }

        public Dictionary<string, DataObject> GetDataForUnityServices() =>
            new()
            {
            {"RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Public,  RelayJoinCode)},
            {"MapId", new DataObject(DataObject.VisibilityOptions.Public,  MapId.ToString())},
            {"CurRound", new DataObject(DataObject.VisibilityOptions.Public,  CurRound.ToString())},
            {"MaxRound", new DataObject(DataObject.VisibilityOptions.Public,  MaxRound.ToString())}
            };

        public void ApplyRemoteData(Lobby lobby)
        {
            var info = new LobbyData
            {
                LobbyID = lobby.Id,
                LobbyCode = lobby.LobbyCode,
                Private = lobby.IsPrivate,
                LobbyName = lobby.Name,
                MaxPlayerCount = lobby.MaxPlayers,
            }; // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.

            if (lobby.Data != null)
            {
                info.RelayJoinCode = lobby.Data.ContainsKey("RelayJoinCode") ? lobby.Data["RelayJoinCode"].Value : null; // By providing RelayCode through the lobby data with Member visibility, we ensure a client is connected to the lobby before they could attempt a relay connection, preventing timing issues between them.
                info.MapId = (ushort)(lobby.Data.ContainsKey("MapId") ? ushort.Parse(lobby.Data["MapId"].Value) : 1);
                info.CurRound = (ushort)(lobby.Data.ContainsKey("CurRound") ? ushort.Parse(lobby.Data["CurRound"].Value) : 1);
                info.MaxRound = (ushort)(lobby.Data.ContainsKey("MaxRound") ? ushort.Parse(lobby.Data["MaxRound"].Value) : 1);
            }
            else
            {
                info.RelayJoinCode = null;
                info.MapId = 1;
                info.CurRound = 1;
                info.MaxRound = 1;
            }

            var lobbyUsers = new Dictionary<string, LocalLobbyUser>();
            foreach (var player in lobby.Players)
            {
                if (player.Data != null)
                {
                    if (LobbyUsers.ContainsKey(player.Id))
                    {
                        lobbyUsers.Add(player.Id, LobbyUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the lobby knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                var incomingData = new LocalLobbyUser
                {
                    IsHost = lobby.HostId.Equals(player.Id),
                    DisplayName = player.Data != null && player.Data.ContainsKey("DisplayName") ? player.Data["DisplayName"].Value : default,
                    ID = player.Id
                };

                lobbyUsers.Add(incomingData.ID, incomingData);
            }

            CopyDataFrom(info, lobbyUsers);
        }

        public void Reset(LocalLobbyUser localUser)
        {
            CopyDataFrom(new LobbyData(), new Dictionary<string, LocalLobbyUser>());
            AddUser(localUser);
        }
    }
}
