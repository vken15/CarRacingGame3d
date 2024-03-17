using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public struct SessionPlayerData : ISessionPlayerData
    {
        public string PlayerName;
        public ushort PlayerNumber;
        public int Score;

        public SessionPlayerData(ulong clientID, string name, bool isConnected = false, int score = 0)
        {
            ClientID = clientID;
            PlayerName = name;
            PlayerNumber = 0;
            Score = score;
            IsConnected = isConnected;
        }

        public bool IsConnected { get; set; }
        public ulong ClientID { get; set; }

        public void Reinitialize()
        {
        }
    }
}
