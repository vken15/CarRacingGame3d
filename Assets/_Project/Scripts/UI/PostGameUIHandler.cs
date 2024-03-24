using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public struct PostGameData : INetworkSerializable, IEquatable<PostGameData>
    {
        public FixedPlayerName PlayerName;
        public ushort PlayerNumber;
        public int Score;

        public PostGameData(string name, ushort playerNumber, int score)
        {
            PlayerNumber = playerNumber;
            Score = score;
            PlayerName = name;
        }

        public bool Equals(PostGameData other)
        {
            return PlayerName.Equals(other.PlayerName) && PlayerNumber == other.PlayerNumber && Score == other.Score;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref PlayerNumber);
            serializer.SerializeValue(ref Score);
        }
    }

    public class PostGameUIHandler : NetworkBehaviour
    {
        [SerializeField] private GameObject leaderboardItemPrefab;
        [SerializeField] Transform leaderboardLayoutGroup;

        private Color[] playerColors = { Color.black, Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.gray, Color.cyan };

        private SetLeaderboardItemInfo[] setLeaderboardItemInfo;

        NetworkList<PostGameData> playerDatas = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                //Get all drivers 
                List<Driver> driverList = GameManager.instance.GetDriverList();

                driverList = driverList.OrderByDescending(x => x.Score).ToList();

                for (int i = 0; i < driverList.Count; i++)
                {
                    PostGameData data = new()
                    {
                        PlayerName = driverList[i].Name,
                        PlayerNumber = driverList[i].PlayerNumber,
                        Score = driverList[i].Score
                    };
                    playerDatas.Add(data);
                }
            }

            SetUpPostData();
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (GameManager.instance.networkStatus == NetworkStatus.offline)
            {
                //Get all drivers 
                List<Driver> driverList = GameManager.instance.GetDriverList();

                driverList = driverList.OrderByDescending(x => x.Score).ToList();

                //Allocate the array
                setLeaderboardItemInfo = new SetLeaderboardItemInfo[driverList.Count];

                //Create the leaderboard items
                for (int i = 0; i < driverList.Count; i++)
                {
                    //Set the position
                    GameObject leaderboardInfoGameObject = Instantiate(leaderboardItemPrefab, leaderboardLayoutGroup);

                    setLeaderboardItemInfo[i] = leaderboardInfoGameObject.GetComponent<SetLeaderboardItemInfo>();
                    setLeaderboardItemInfo[i].SetDriverNameText(driverList[i].Name, playerColors[driverList[i].PlayerNumber]);
                    setLeaderboardItemInfo[i].SetPositionText($"{i + 1}.");
                    setLeaderboardItemInfo[i].SetDriverFinishTimeText("");
                    setLeaderboardItemInfo[i].SetDriverScoreText($"{driverList[i].Score}");
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        void SetUpPostData()
        {
            //Allocate the array
            setLeaderboardItemInfo = new SetLeaderboardItemInfo[playerDatas.Count];

            //Create the leaderboard items
            for (int i = 0; i < playerDatas.Count; i++)
            {
                //Set the position
                GameObject leaderboardInfoGameObject = Instantiate(leaderboardItemPrefab, leaderboardLayoutGroup);

                setLeaderboardItemInfo[i] = leaderboardInfoGameObject.GetComponent<SetLeaderboardItemInfo>();
                setLeaderboardItemInfo[i].SetDriverNameText(playerDatas[i].PlayerName, playerColors[playerDatas[i].PlayerNumber]);
                setLeaderboardItemInfo[i].SetPositionText($"{i + 1}.");
                setLeaderboardItemInfo[i].SetDriverFinishTimeText("");
                setLeaderboardItemInfo[i].SetDriverScoreText($"{playerDatas[i].Score}");
            }
        }

        public void OnReturn()
        {
            ConnectionManager.instance.RequestShutdown();
        }
    }
}
