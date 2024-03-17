using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CarRacingGame3d
{
    public class PlayerSeatUIHandler : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text isReadyText;

        // just a way to designate which seat we are -- the leftmost seat on the lobby UI is index 0, the next one is index 1, etc.
        private int seatIndex;

        // playerNumber of who is sitting in this seat right now. 1-based; e.g. this is 1 for Player 1, 2 for Player 2, etc. Meaningless when state is Inactive (and in that case it is set to 0 for clarity)
        private ushort playerNumber;

        // the last SeatState we were assigned
        private SeatState state;

        public void Initialize(int seat)
        {
            seatIndex = seat;
            state = SeatState.Inactive;
            playerNumber = 0;
            ConfigureStateGraphics();
        }

        public void InitializeBlock(int seat)
        {
            seatIndex = seat;
            state = SeatState.Block;
            playerNumber = 0;
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
            }
            else if (state == SeatState.Block)
            {
                isReadyText.text = "";
                playerNameText.text = "";
            }
            else if (state == SeatState.AI)
            {

            }
            else
            {
                if (state == SeatState.LockedIn)
                {
                    isReadyText.text = "READY";
                }
                else if (state == SeatState.Host)
                {
                    isReadyText.text = "OWNER";
                }
                else
                {
                    isReadyText.text = "NOT READY";
                }
            }
        }
    }
}
