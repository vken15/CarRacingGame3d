using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

namespace CarRacingGame3d
{
    public class KeyboardBindingsUIHandler : MonoBehaviour
    {
        [Header("Input Text")]
        [SerializeField] private Text moveFowardTxt;
        [SerializeField] private Text moveBackTxt;
        [SerializeField] private Text turnLeftTxt;
        [SerializeField] private Text turnRightTxt;
        [SerializeField] private Text brakeTxt;
        [SerializeField] private Text specialTxt;
        [SerializeField] private Text itemTxt;
        [SerializeField] private Text escTxt;

        [Header("Canvas")]
        [SerializeField] private Canvas wattingCanvas;

        private Controls controllers;
        private InputAction move;
        private InputAction brake;
        private InputAction special;
        private InputAction useItem;
        private Canvas canvas;

        private RebindingOperation rebindingOperation;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;
        }

        private void Start()
        {
            controllers = InputManager.instance.Controllers;
            move = controllers.Player.Move;
            special = controllers.Player.Nitro;
            brake = controllers.Player.Brake;
            useItem = controllers.Player.Item;
            ChangeInputDisplay();
        }

        private void ChangeInputDisplay()
        {
            moveFowardTxt.text = InputControlPath.ToHumanReadableString(move.bindings[1].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            moveBackTxt.text = InputControlPath.ToHumanReadableString(move.bindings[2].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            turnLeftTxt.text = InputControlPath.ToHumanReadableString(move.bindings[3].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            turnRightTxt.text = InputControlPath.ToHumanReadableString(move.bindings[4].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            brakeTxt.text = InputControlPath.ToHumanReadableString(brake.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            specialTxt.text = InputControlPath.ToHumanReadableString(special.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            itemTxt.text = InputControlPath.ToHumanReadableString(useItem.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
            escTxt.text = InputControlPath.ToHumanReadableString(controllers.Player.ESC.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        public void OnMoveFowardBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            moveFowardTxt.enabled = false;
            rebindingOperation = move.PerformInteractiveRebinding()
                .WithTargetBinding(1)
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithControlsExcluding(move.bindings[2].effectivePath)
                .WithControlsExcluding(move.bindings[3].effectivePath)
                .WithControlsExcluding(move.bindings[4].effectivePath)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    moveFowardTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    moveFowardTxt.enabled = true;
                    RebindComplete();
                    moveFowardTxt.text = InputControlPath.ToHumanReadableString(move.bindings[1].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnMoveBackBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            moveBackTxt.enabled = false;
            rebindingOperation = move.PerformInteractiveRebinding()
                .WithTargetBinding(2)
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithControlsExcluding(move.bindings[1].effectivePath)
                .WithControlsExcluding(move.bindings[3].effectivePath)
                .WithControlsExcluding(move.bindings[4].effectivePath)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    moveBackTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    moveBackTxt.enabled = true;
                    RebindComplete();
                    moveBackTxt.text = InputControlPath.ToHumanReadableString(move.bindings[2].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnTurnLeftBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            turnLeftTxt.enabled = false;
            rebindingOperation = move.PerformInteractiveRebinding()
                .WithTargetBinding(3)
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithControlsExcluding(move.bindings[2].effectivePath)
                .WithControlsExcluding(move.bindings[1].effectivePath)
                .WithControlsExcluding(move.bindings[4].effectivePath)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    turnLeftTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    turnLeftTxt.enabled = true;
                    RebindComplete();
                    turnLeftTxt.text = InputControlPath.ToHumanReadableString(move.bindings[3].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnTurnRightBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            turnRightTxt.enabled = false;
            rebindingOperation = move.PerformInteractiveRebinding()
                .WithTargetBinding(4)
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithControlsExcluding(move.bindings[2].effectivePath)
                .WithControlsExcluding(move.bindings[3].effectivePath)
                .WithControlsExcluding(move.bindings[1].effectivePath)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    turnRightTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    turnRightTxt.enabled = true;
                    RebindComplete();
                    turnRightTxt.text = InputControlPath.ToHumanReadableString(move.bindings[4].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnBrakeBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            brakeTxt.enabled = false;
            rebindingOperation = brake.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithControlsExcluding(move.bindings[4].effectivePath)
                .WithControlsExcluding(move.bindings[3].effectivePath)
                .WithControlsExcluding(move.bindings[1].effectivePath)
                .WithControlsExcluding(special.bindings[0].effectivePath)
                .WithControlsExcluding(useItem.bindings[0].effectivePath)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    brakeTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    brakeTxt.enabled = true;
                    RebindComplete();
                    brakeTxt.text = InputControlPath.ToHumanReadableString(brake.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnSpecialBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            specialTxt.enabled = false;
            rebindingOperation = special.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    specialTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    specialTxt.enabled = true;
                    RebindComplete();
                    specialTxt.text = InputControlPath.ToHumanReadableString(special.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnItemBtn()
        {
            controllers.Disable();
            wattingCanvas.enabled = true;
            itemTxt.enabled = false;
            rebindingOperation = useItem.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .WithControlsExcluding(move.bindings[1].effectivePath)
                .WithControlsExcluding(move.bindings[2].effectivePath)
                .WithControlsExcluding(move.bindings[3].effectivePath)
                .WithControlsExcluding(move.bindings[4].effectivePath)
                .WithControlsExcluding(special.bindings[0].effectivePath)
                .WithControlsExcluding(brake.bindings[0].effectivePath)
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(_ =>
                {
                    itemTxt.enabled = true;
                    RebindComplete();
                })
                .OnComplete(operation =>
                {
                    itemTxt.enabled = true;
                    RebindComplete();
                    itemTxt.text = InputControlPath.ToHumanReadableString(useItem.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        public void OnESCBtn()
        {
            InputAction esc = controllers.Player.ESC;
            controllers.Disable();
            wattingCanvas.enabled = true;
            escTxt.enabled = false;
            rebindingOperation = esc.PerformInteractiveRebinding()
                .WithControlsExcluding("Mouse")
                .WithControlsExcluding("<keyboard>/anyKey")
                .OnComplete(operation =>
                {
                    escTxt.enabled = true;
                    RebindComplete();
                    escTxt.text = InputControlPath.ToHumanReadableString(esc.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
                }).Start();
        }

        private void RebindComplete()
        {
            rebindingOperation.Dispose();
            controllers.Enable();
            wattingCanvas.enabled = false;
        }

        public void OnResetBindings()
        {
            controllers.RemoveAllBindingOverrides();
            ChangeInputDisplay();
            ClientPrefs.DeleteRebinds();
        }

        public void OnClose()
        {
            canvas.enabled = false;
        }
    }
}
