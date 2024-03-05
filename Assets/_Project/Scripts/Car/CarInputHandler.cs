using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CarRacingGame3d
{
    public struct DriverInput
    {
        public Vector2 Move;
        public bool Brake;
        public bool Nitro;
    }

    public class CarInputHandler : NetworkBehaviour
    {
        public int playerNumber = 1;

        private CarController carController;
        private ItemController itemController;

        private Controls control;

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            carController = GetComponent<CarController>();
            itemController = GetComponent<ItemController>();
            control = InputManager.instance.Controllers;
        }

        // Update is called once per frame
        void Update()
        {
            if (GameManager.instance.GetGameState() == GameStates.countdown) return;
            if (GameManager.instance.networkStatus == NetworkStatus.online && !IsOwner) return;

            DriverInput playerInput = new()
            {
                Move = control.Player.Move.ReadValue<Vector2>(),
                Brake = control.Player.Brake.IsPressed(),
                Nitro = control.Player.Nitro.IsPressed()
            };

            carController.SetInput(playerInput);

            if (control.Player.Item.IsPressed())
                itemController.UseItem();

            //Test
            if (control.Player.ESC.IsPressed())
            {
                transform.position += transform.forward * 20f;
            }

        }
    }
}