using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using UnityEngine;

namespace CarRacingGame3d
{
    public class ConnectionStatusMessageUIManager : MonoBehaviour
    {
        public static ConnectionStatusMessageUIManager instance = null;

        PopupPanel currentReconnectPopup;

        void Awake()
        {
            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        public void OnConnectStatus(ConnectStatus status)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    PopupManager.ShowPopupPanel("Connection Failed", "The Host is full and cannot accept any additional connections.");
                    break;
                case ConnectStatus.Success:
                    break;
                case ConnectStatus.LoggedInAgain:
                    PopupManager.ShowPopupPanel("Connection Failed", "You have logged in elsewhere using the same account. If you still want to connect, select a different profile by using the 'Change Profile' button.");
                    break;
                case ConnectStatus.IncompatibleBuildType:
                    PopupManager.ShowPopupPanel("Connection Failed", "Server and client builds are not compatible. You cannot connect a release build to a development build or an in-editor session.");
                    break;
                case ConnectStatus.GenericDisconnect:
                    PopupManager.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost.");
                    break;
                case ConnectStatus.HostEndedSession:
                    PopupManager.ShowPopupPanel("Disconnected From Host", "The host has ended the game session.");
                    break;
                case ConnectStatus.Reconnecting:
                    break;
                case ConnectStatus.StartHostFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting host failed.");
                    break;
                case ConnectStatus.StartClientFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting client failed.");
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }

        public void OnReconnectMessage(ReconnectMessage message)
        {
            if (message.CurrentAttempt == message.MaxAttempt)
            {
                CloseReconnectPopup();
            }
            else if (currentReconnectPopup != null)
            {
                currentReconnectPopup.SetupPopupPanel("Connection lost", $"Attempting to reconnect...\nAttempt {message.CurrentAttempt + 1}/{message.MaxAttempt}", closeableByUser: false);
            }
            else
            {
                currentReconnectPopup = PopupManager.ShowPopupPanel("Connection lost", $"Attempting to reconnect...\nAttempt {message.CurrentAttempt + 1}/{message.MaxAttempt}", closeableByUser: false);
            }
        }

        void CloseReconnectPopup()
        {
            if (currentReconnectPopup != null)
            {
                currentReconnectPopup.Hide();
                currentReconnectPopup = null;
            }
        }

        public void ServiceErrorHandler(UnityServiceErrorMessage error)
        {
            var errorMessage = error.Message;
            switch (error.AffectedService)
            {
                case UnityServiceErrorMessage.Service.Lobby:
                    {
                        HandleLobbyError(error);
                        break;
                    }
                case UnityServiceErrorMessage.Service.Authentication:
                    {
                        PopupManager.ShowPopupPanel(
                            "Authentication Error",
                            $"{error.OriginalException.Message} \n tip: You can still use the Direct IP/LAN connection option.");
                        break;
                    }
                default:
                    {
                        PopupManager.ShowPopupPanel("Service error: " + error.Title, errorMessage);
                        break;
                    }
            }
        }

        void HandleLobbyError(UnityServiceErrorMessage error)
        {
            var exception = error.OriginalException as LobbyServiceException;
            if (exception != null)
            {
                switch (exception.Reason)
                {
                    // If the error is one of the following, the player needs to know about it, so show in a popup message. Otherwise, the log in the console is sufficient.
                    case LobbyExceptionReason.ValidationError:
                        PopupManager.ShowPopupPanel("Validation Error", "Validation check failed on Lobby. Is the join code correctly formatted?");
                        break;
                    case LobbyExceptionReason.LobbyNotFound:
                        PopupManager.ShowPopupPanel("Lobby Not Found", "Requested lobby not found. The join code is incorrect or the lobby has ended.");
                        break;
                    case LobbyExceptionReason.LobbyConflict:
                        // LobbyConflict can have multiple causes. Let's add other solutions here if there's other situations that arise for this.
                        Debug.LogError($"Got service error {error.Message} with LobbyConflict. Possible conflict cause: Trying to play with two builds on the " +
                            $"same machine. Please change profile in-game or use command line arg '{ProfileManager.AuthProfileCommandLineArg} someName' to set a different auth profile.\n");
                        PopupManager.ShowPopupPanel("Failed to join Lobby", "Failed to join Lobby due to a conflict. If trying to connect two local builds to the same lobby, they need to have different profiles. See logs for more details.");
                        break;
                    case LobbyExceptionReason.NoOpenLobbies:
                        PopupManager.ShowPopupPanel("Failed to join Lobby", "No accessible lobbies are currently available for quick-join.");
                        break;
                    case LobbyExceptionReason.LobbyFull:
                        PopupManager.ShowPopupPanel("Failed to join Lobby", "Lobby is full and can't accept more players.");
                        break;
                    case LobbyExceptionReason.Unauthorized:
                        PopupManager.ShowPopupPanel("Lobby error", "Received HTTP error 401 Unauthorized from Lobby Service.");
                        break;
                    case LobbyExceptionReason.RequestTimeOut:
                        PopupManager.ShowPopupPanel("Lobby error", "Received HTTP error 408 Request timed out from Lobby Service.");
                        break;
                }
            }
        }
    }
}
