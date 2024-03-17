using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CarRacingGame3d
{
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI titleText;
        [SerializeField]
        TextMeshProUGUI mainText;
        [SerializeField]
        GameObject confirmButton;
        [SerializeField]
        GameObject loadingSpinner;
        [SerializeField]
        CanvasGroup canvasGroup;

        public bool IsDisplaying => isDisplaying;

        bool isDisplaying;

        bool closableByUser;

        void Awake()
        {
            Hide();
        }

        public void SetupPopupPanel(string titleText, string mainText, bool closeableByUser = true)
        {
            this.titleText.text = titleText;
            this.mainText.text = mainText;
            closableByUser = closeableByUser;
            confirmButton.SetActive(closableByUser);
            loadingSpinner.SetActive(!closableByUser);
            Show();
        }

        void Show()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            isDisplaying = true;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            isDisplaying = false;
        }

        //Button
        public void OnConfirmClick()
        {
            if (closableByUser)
            {
                Hide();
            }
        }
    }
}
