using System.Collections.Generic;
using UnityEngine;

namespace CarRacingGame3d
{
    public class PopupManager : MonoBehaviour
    {
        [SerializeField]
        GameObject popupPanelPrefab;

        [SerializeField]
        GameObject canvas;

        List<PopupPanel> popupPanels = new();

        static PopupManager instance = null;

        const float k_Offset = 30;
        const float k_MaxOffset = 200;

        void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(canvas);
        }

        public static PopupPanel ShowPopupPanel(string titleText, string mainText, bool closeableByUser = true)
        {
            if (instance != null)
            {
                return instance.DisplayPopupPanel(titleText, mainText, closeableByUser);
            }

            Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            return null;
        }

        PopupPanel DisplayPopupPanel(string titleText, string mainText, bool closeableByUser)
        {
            var popup = GetNextAvailablePopupPanel();
            if (popup != null)
            {
                popup.SetupPopupPanel(titleText, mainText, closeableByUser);
            }

            return popup;
        }

        PopupPanel GetNextAvailablePopupPanel()
        {
            int nextAvailablePopupIndex = 0;
            // Find the index of the first PopupPanel that is not displaying and has no popups after it that are currently displaying
            for (int i = 0; i < popupPanels.Count; i++)
            {
                if (popupPanels[i].IsDisplaying)
                {
                    nextAvailablePopupIndex = i + 1;
                }
            }

            if (nextAvailablePopupIndex < popupPanels.Count)
            {
                return popupPanels[nextAvailablePopupIndex];
            }

            // None of the current PopupPanels are available, so instantiate a new one
            var popupGameObject = Instantiate(popupPanelPrefab, gameObject.transform);
            popupGameObject.transform.position += new Vector3(1, -1) * (k_Offset * popupPanels.Count % k_MaxOffset);
            var popupPanel = popupGameObject.GetComponent<PopupPanel>();
            if (popupPanel != null)
            {
                popupPanels.Add(popupPanel);
            }
            else
            {
                Debug.LogError("PopupPanel prefab does not have a PopupPanel component!");
            }

            return popupPanel;
        }
    }
}
