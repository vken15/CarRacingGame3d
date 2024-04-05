using UnityEngine;

namespace CarRacingGame3d
{
    /// <summary>
    /// Singleton class which saves/loads local-client settings.
    /// (This is just a wrapper around the PlayerPrefs system,
    /// so that all the calls are in the same place.)
    /// </summary>
    public static class ClientPrefs
    {
        const string masterVolumeKey = "MasterVolume";
        const string musicVolumeKey = "MusicVolume";
        const string sfxVolumeKey = "SFXVolume";
        const string clientGUIDKey = "client_guid";
        const string availableProfileKey = "AvailableProfile";
        const string resolutionWidthKey = "resolutionWidth";
        const string resolutionHeightKey = "resolutionHeight";
        const string fullScreenKey = "fullScreen";
        const string rebindKey = "rebinds";

        const float defaultMasterVolume = 0.5f;
        const float defaultMusicVolume = 0.8f;
        const float defaultSFXVolume = 0.8f;
        const int defaultResolutionWidth = 1920;
        const int defaultResolutionHeight = 1080;
        const int defaultFullScreen = 0;

        public static float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat(masterVolumeKey, defaultMasterVolume);
        }

        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(masterVolumeKey, volume);
        }

        public static float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat(musicVolumeKey, defaultMusicVolume);
        }

        public static void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat(musicVolumeKey, volume);
        }

        public static float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat(sfxVolumeKey, defaultSFXVolume);
        }

        public static void SetSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat(sfxVolumeKey, volume);
        }

        public static int GetResolutionWidth()
        {
            return PlayerPrefs.GetInt(resolutionWidthKey, defaultResolutionWidth);
        }

        public static void SetResolutionWidth(int width)
        {
            PlayerPrefs.SetInt(resolutionWidthKey, width);
        }

        public static int GetResolutionHeight()
        {
            return PlayerPrefs.GetInt(resolutionHeightKey, defaultResolutionHeight);
        }

        public static void SetResolutionHeight(int height)
        {
            PlayerPrefs.SetInt(resolutionHeightKey, height);
        }

        public static int GetFullScreen()
        {
            return PlayerPrefs.GetInt(fullScreenKey, defaultFullScreen);
        }

        public static void SetFullScreen(int isFullScreen)
        {
            PlayerPrefs.SetInt(fullScreenKey, isFullScreen);
        }

        public static string GetRebinds()
        {
            return PlayerPrefs.GetString(rebindKey);
        }

        public static void SetRebinds(string rebinds)
        {
            PlayerPrefs.SetString(rebindKey, rebinds);
        }

        public static void DeleteRebinds()
        {
            PlayerPrefs.DeleteKey(rebindKey);
        }

        /// <summary>
        /// Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it.
        /// </summary>
        /// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
        public static string GetGuid()
        {
            if (PlayerPrefs.HasKey(clientGUIDKey))
            {
                return PlayerPrefs.GetString(clientGUIDKey);
            }

            var guid = System.Guid.NewGuid();
            var guidString = guid.ToString();

            PlayerPrefs.SetString(clientGUIDKey, guidString);
            return guidString;
        }

        public static string GetAvailableProfile()
        {
            return PlayerPrefs.GetString(availableProfileKey, "");
        }

        public static void SetAvailableProfile(string availableProfiles)
        {
            PlayerPrefs.SetString(availableProfileKey, availableProfiles);
        }
    }
}