using CarRacingGame3d.UnityServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarRacingGame3d
{
    public class MainMenuUIHandler : MonoBehaviour
    {
        [SerializeField] Button onlineBtn;
        [SerializeField] Canvas profileCanvas;
        [SerializeField] CanvasGroup mainMenuCanvas;
        [SerializeField] TMP_InputField profileInput;

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
            if (!ProfileManager.Instance.AvailableProfiles.Contains("Tester"))
            {
                ProfileManager.Instance.CreateProfile("Tester");
                ProfileManager.Instance.Profile = "Tester";
                profileCanvas.enabled = true;
                mainMenuCanvas.interactable = false;
            }
            profileInput.text = ProfileManager.Instance.AvailableProfiles[^1];
        }

        void OnDestroy()
        {
            ProfileManager.Instance.onProfileChanged -= OnProfileChanged;
        }

        private async void TrySignIn()
        {
            try
            {
                var unityAuthenticationInitOptions =
                    AuthenticationServiceFacade.Instance.GenerateAuthenticationOptions(ProfileManager.Instance.Profile);

                await AuthenticationServiceFacade.Instance.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                ProfileManager.Instance.onProfileChanged += OnProfileChanged;
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
            //m_SignInSpinner.SetActive(false);

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

            //if (m_SignInSpinner)
            //{
            //    m_SignInSpinner.SetActive(false);
            //}
        }

        async void OnProfileChanged()
        {
            onlineBtn.interactable = false;
            //m_SignInSpinner.SetActive(true);
            await AuthenticationServiceFacade.Instance.SwitchProfileAndReSignInAsync(ProfileManager.Instance.Profile);

            onlineBtn.interactable = true;
            //m_SignInSpinner.SetActive(false);

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
            if (!ProfileManager.Instance.AvailableProfiles.Contains(profileInput.text))
            {
                ProfileManager.Instance.CreateProfile(profileInput.text);
                ProfileManager.Instance.Profile = profileInput.text;
            }
            profileCanvas.enabled = false;
            mainMenuCanvas.interactable = true;
        }

        public void OnQuit()
        {
            Application.Quit();
        }
    }
}
