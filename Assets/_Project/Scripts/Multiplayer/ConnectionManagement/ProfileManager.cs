using System;
using UnityEngine;

#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
#endif

namespace CarRacingGame3d
{
    public class ProfileManager
    {
        public static ProfileManager Instance => instance ??= new ProfileManager();

        static ProfileManager instance;

        public const string AuthProfileCommandLineArg = "-AuthProfile";

        string profile;

        public string Profile
        {
            get
            {
                return profile ??= GetProfile();
            }
            set
            {
                profile = value;
                OnProfileChanged?.Invoke();
            }
        }

        public event Action OnProfileChanged;

        string availableProfile;

        public string AvailableProfile
        {
            get
            {
                if (availableProfile == null)
                {
                    LoadProfiles();
                }

                return availableProfile;
            }
        }

        public void CreateProfile(string profile)
        {
            availableProfile = profile;
            ClientPrefs.SetAvailableProfile(availableProfile);
        }

        static string GetProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == AuthProfileCommandLineArg)
                {
                    return arguments[i + 1];
                }
            }

#if UNITY_EDITOR

            // When running in the Editor make a unique ID from the Application.dataPath.
            // This will work for cloning projects manually, or with Virtual Projects.
            // Since only a single instance of the Editor can be open for a specific
            // dataPath, uniqueness is ensured.
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            // Authentication service only allows profile names of maximum 30 characters. We're generating a GUID based
            // on the project's path. Truncating the first 30 characters of said GUID string suffices for uniqueness.
            return new Guid(hashedBytes).ToString("N")[..30];
#else
            return "";
#endif
        }

        void LoadProfiles()
        {
            availableProfile = ClientPrefs.GetAvailableProfile();
        }
    }
}
