using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public struct DriverInput : INetworkSerializable
    {
        public Vector2 Move;
        public bool Brake;
        public bool Nitro;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Move);
            serializer.SerializeValue(ref Brake);
            serializer.SerializeValue(ref Nitro);
        }
    }

    public class CarInputHandler : NetworkBehaviour
    {
        private readonly NetworkVariable<DriverInput> playerInput = new(writePerm: NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> itemInput = new(writePerm: NetworkVariableWritePermission.Owner);

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

            playerInput.Value = new()
            {
                Move = control.Player.Move.ReadValue<Vector2>(),
                Brake = control.Player.Brake.IsPressed(),
                Nitro = control.Player.Nitro.IsPressed(),
            };

            itemInput.Value = control.Player.Item.IsPressed();

            if (GameManager.instance.networkStatus == NetworkStatus.online) return;

            if (control.Player.Item.IsPressed())
                itemController.UseItem();

            carController.SetInput(playerInput.Value);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            PlayerClientRPC();
        }

        [ClientRpc]
        private void PlayerClientRPC()
        {
            playerInput.OnValueChanged += (DriverInput prevValue, DriverInput newValue) =>
            {
                carController.SetInput(newValue);
            };
            itemInput.OnValueChanged += (bool prevValue, bool newValue) =>
            {
                if (newValue)
                    itemController.UseItem();
            };
        }

        public void SetMinimapColor()
        {
            minimapIcon.color = playerColors[playerNumber];
        }
    }
}