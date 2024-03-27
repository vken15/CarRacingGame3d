using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] Image minimapIcon;

        public int playerNumber = 1;

        private CarController carController;
        private ItemController itemController;
        private Controls control;
        private readonly Color[] playerColors = { Color.black, Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.gray, Color.cyan, Color.black };

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
            if (GameManager.instance.GetGameState() == GameStates.Countdown) return;
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
        }
    
        public void SetMinimapColor()
        {
            minimapIcon.color = playerColors[playerNumber];
        }
    }
}