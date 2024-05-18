using CarRacingGame3d.UnityServices;
using System;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

namespace CarRacingGame3d
{
    public class MainMenuUIHandler : MonoBehaviour
    {
        [SerializeField] Button onlineBtn;
        [SerializeField] Canvas profileCanvas;
        [SerializeField] CanvasGroup mainMenuCanvas;
        [SerializeField] TMP_InputField profileInput;
        [SerializeField] GameObject signInSpinner;
        [SerializeField] GameObject optionsCanvas;

        void Awake()
        {
            onlineBtn.interactable = false;
            profileCanvas.enabled = false;
            GameManager.instance.networkStatus = NetworkStatus.offline;
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                OnSignInFailed();
                return;
            }

            TrySignIn();
        }

        void Start()
        {
            if (ProfileManager.Instance.AvailableProfile.Equals(""))
            {
                ProfileManager.Instance.CreateProfile("Tester");
                ProfileManager.Instance.Profile = "Tester";
                profileCanvas.enabled = true;
                mainMenuCanvas.interactable = false;
            }
            profileInput.text = ProfileManager.Instance.AvailableProfile;
        }

        void OnDestroy()
        {
            ProfileManager.Instance.OnProfileChanged -= OnProfileChanged;
        }

        private async void TrySignIn()
        {
            try
            {
                var unityAuthenticationInitOptions =
                    AuthenticationServiceFacade.Instance.GenerateAuthenticationOptions(ProfileManager.Instance.Profile);

                await AuthenticationServiceFacade.Instance.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                ProfileManager.Instance.OnProfileChanged += OnProfileChanged;
            }
            catch (Exception)
            {
                OnSignInFailed();
            }
        }

        private void OnAuthSignIn()
        {
            onlineBtn.interactable = true;
            //m_UGSSetupTooltipDetector.enabled = false;
            signInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            LocalLobbyUser.Instance.ID = AuthenticationService.Instance.PlayerId;

            // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
            LocalLobby.Instance.AddUser(LocalLobbyUser.Instance);
        }

        private void OnSignInFailed()
        {
            if (onlineBtn)
            {
                onlineBtn.interactable = false;
                //m_UGSSetupTooltipDetector.enabled = true;
            }

            if (signInSpinner)
            {
                signInSpinner.SetActive(false);
            }
        }

        async void OnProfileChanged()
        {
            onlineBtn.interactable = false;
            signInSpinner.SetActive(true);
            await AuthenticationServiceFacade.Instance.SwitchProfileAndReSignInAsync(ProfileManager.Instance.Profile);

            onlineBtn.interactable = true;
            signInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Updating LocalUser and LocalLobby
            LocalLobby.Instance.RemoveUser(LocalLobbyUser.Instance);
            LocalLobbyUser.Instance.ID = AuthenticationService.Instance.PlayerId;
            LocalLobby.Instance.AddUser(LocalLobbyUser.Instance);
        }

        //Buttons

        public void OnOnline()
        {
            GameManager.instance.networkStatus = NetworkStatus.online;
            SceneManager.LoadScene("LobbyOnline");
        }

        public void OnLAN()
        {
            GameManager.instance.networkStatus = NetworkStatus.online;
            SceneManager.LoadScene("Lobby");
        }

        public void OnProfile()
        {
            profileCanvas.enabled = true;
            mainMenuCanvas.interactable = false;
        }

        public void OnProfileConfirm()
        {
            Debug.Log($"New profile: {profileInput.text}");
            if (!ProfileManager.Instance.AvailableProfile.Contains(profileInput.text))
            {
                ProfileManager.Instance.CreateProfile(profileInput.text);
                ProfileManager.Instance.Profile = profileInput.text;
            }
            profileCanvas.enabled = false;
            mainMenuCanvas.interactable = true;
        }

        public void OnOptions()
        {
            optionsCanvas.SetActive(true);
        }

        public void OnQuit()
        {
            Application.Quit();
        }
    }
}
