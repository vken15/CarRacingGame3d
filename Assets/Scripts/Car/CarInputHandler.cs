using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class CarInputHandler : NetworkBehaviour
{
    private NetworkVariable<Vector2> inputVector = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> inputNitro = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> inputBrake = new(writePerm: NetworkVariableWritePermission.Owner);

    public int playerNumber = 1;

    private CarController carController;

    private Controls control;

    public override void OnNetworkSpawn()
    {
        PlayerClientRPC();
    }

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        carController = GetComponent<CarController>();
        control = InputManager.instance.Controllers;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.networkStatus == NetworkStatus.online)
        {
            if (!IsOwner) return;
            inputVector.Value = control.Player.Move.ReadValue<Vector2>();
            inputNitro.Value = control.Player.Nitro.IsPressed();
            inputBrake.Value = control.Player.Brake.IsPressed();

            //if (IsClient) return;
            //carController.SetInput(inputVector.Value, inputNitro.Value, inputBrake.Value);
        }
        else
        {
            Vector2 inputVector = control.Player.Move.ReadValue<Vector2>();
            bool inputNitro = control.Player.Nitro.IsPressed();
            bool inputBrake = control.Player.Brake.IsPressed();

            carController.SetInput(inputVector, inputNitro, inputBrake);
        }
    }

    [ClientRpc]
    private void PlayerClientRPC()
    {
        inputVector.OnValueChanged += (Vector2 prevValue, Vector2 newValue) =>
        {
            carController.SetInput(inputVector.Value, inputNitro.Value, inputBrake.Value);
        };
        inputNitro.OnValueChanged += (bool prevValue, bool newValue) =>
        {
            carController.SetInput(inputVector.Value, inputNitro.Value, inputBrake.Value);
        };
        inputBrake.OnValueChanged += (bool prevValue, bool newValue) =>
        {
            carController.SetInput(inputVector.Value, inputNitro.Value, inputBrake.Value);
        };
    }
}
