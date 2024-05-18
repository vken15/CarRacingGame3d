using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class PlayerSeatUIHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text isReadyText;
        [SerializeField] private Image carImage;
        [SerializeField] private Image randomCarImage;
        [SerializeField] private GameObject addAIBtn;
        [SerializeField] private GameObject removeAIBtn;
        [SerializeField] private GameObject kickBtn;

        private int seatIndex;

        // playerNumber of who is sitting in this seat right now. 1-based; e.g. this is 1 for Player 1, 2 for Player 2, etc. Meaningless when state is Inactive (and in that case it is set to 0 for clarity)
        private ushort playerNumber;

        // the last SeatState we were assigned
        private SeatState state;

        public void Initialize(int seat, bool isBlocked)
        {
            seatIndex = seat;
            state = isBlocked ? SeatState.Block : SeatState.Inactive;
            playerNumber = 0;
            carImage.gameObject.SetActive(false);
            randomCarImage.gameObject.SetActive(false);
            kickBtn.SetActive(false);
            ConfigureStateGraphics();
        }

        public void SetState(SeatState seatState, ushort playerIndex, string playerName)
        {
            if (seatState == state && playerIndex == playerNumber)
                return; // no actual changes

            state = seatState;
            playerNumber = playerIndex;
            playerNameText.text = playerName;
            if (state == SeatState.Inactive)
                playerNumber = 0;
            ConfigureStateGraphics();
        }

        public void SetCarSprite(Sprite sprite)
        {
            carImage.sprite = sprite;
        }

        public bool IsLocked()
        {
            return state == SeatState.LockedIn;
        }

        private void ConfigureStateGraphics()
        {
            if (state == SeatState.Inactive)
            {
                isReadyText.text = "";
                playerNameText.text = "EMPTY";
                carImage.gameObject.SetActive(false);
                randomCarImage.gameObject.SetActive(false);
                kickBtn.SetActive(false);
                if (NetworkManager.Singleton.IsServer)
                {
                    addAIBtn.SetActive(true);
                    removeAIBtn.SetActive(false);
                }
            }
            else if (state == SeatState.Block)
            {
                isReadyText.text = "";
                playerNameText.text = "";
            }
            else if (state == SeatState.AI)
            {
                isReadyText.text = "";
                playerNameText.text = "AI";
                carImage.gameObject.SetActive(false);
                randomCarImage.gameObject.SetActive(true);
                kickBtn.SetActive(false);
                if (NetworkManager.Singleton.IsServer)
                {
                    addAIBtn.SetActive(false);
                    removeAIBtn.SetActive(true);
                }
            }
            else
            {
                addAIBtn.SetActive(false);
                removeAIBtn.SetActive(false);
                carImage.gameObject.SetActive(true);
                randomCarImage.gameObject.SetActive(false);

                if (NetworkManager.Singleton.IsServer)
                {
                    kickBtn.SetActive(true);
                }

                if (state == SeatState.LockedIn)
                {
                    isReadyText.text = "READY";
                }
                else if (state == SeatState.Host)
                {
                    isReadyText.text = "OWNER";
                    kickBtn.SetActive(false);
                }
                else
                {
                    isReadyText.text = "NOT READY";
                }
            }
        }
    }
}
