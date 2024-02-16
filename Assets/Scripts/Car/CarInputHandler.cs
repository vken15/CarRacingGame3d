using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CarInputHandler : MonoBehaviour
{
    public int playerNumber = 1;

    private CarController carController;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        carController = GetComponent<CarController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 inputVector = Vector2.zero;
        inputVector.y = Input.GetAxis("Vertical");
        inputVector.x = Input.GetAxis("Horizontal");
        bool nitroInput = Input.GetKey(KeyCode.LeftShift);
        bool brakeInput = Input.GetKey(KeyCode.Space);

        carController.GetInputs(inputVector, nitroInput, brakeInput);
    }
}
