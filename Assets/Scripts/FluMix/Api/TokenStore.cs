using UnityEngine;

namespace FluMix.Api
{
    public static class TokenStore
    {
        const string AccessKey = "flumist.accessToken";
        const string RefreshKey = "flumist.refreshToken";
        const string UsernameKey = "flumist.username";
        const string FullNameKey = "flumist.fullName";
        const string UuidKey = "flumist.uuid";

        public static string AccessToken
        {
            get => PlayerPrefs.GetString(AccessKey, string.Empty);
            set => PlayerPrefs.SetString(AccessKey, value ?? string.Empty);
        }

        public static string RefreshToken
        {
            get => PlayerPrefs.GetString(RefreshKey, string.Empty);
            set => PlayerPrefs.SetString(RefreshKey, value ?? string.Empty);
        }

        public static string Username
        {
            get => PlayerPrefs.GetString(UsernameKey, string.Empty);
            set => PlayerPrefs.SetString(UsernameKey, value ?? string.Empty);
        }

        public static string FullName
        {
            get => PlayerPrefs.GetString(FullNameKey, string.Empty);
            set => PlayerPrefs.SetString(FullNameKey, value ?? string.Empty);
        }

        public static string Uuid
        {
            get => PlayerPrefs.GetString(UuidKey, string.Empty);
            set => PlayerPrefs.SetString(UuidKey, value ?? string.Empty);
        }

        public static bool HasSession =>
            !string.IsNullOrEmpty(AccessToken) || !string.IsNullOrEmpty(RefreshToken);

        public static void SaveAuth(AuthDto auth)
        {
            if (auth == null)
                return;

            AccessToken = auth.accessToken;
            RefreshToken = auth.refreshToken;
            if (auth.player != null)
            {
                Username = auth.player.username;
                FullName = string.IsNullOrEmpty(auth.player.fullName)
                    ? auth.player.username
                    : auth.player.fullName;
                Uuid = auth.player.uuid;
            }

            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(AccessKey);
            PlayerPrefs.DeleteKey(RefreshKey);
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.DeleteKey(FullNameKey);
            PlayerPrefs.DeleteKey(UuidKey);
            PlayerPrefs.Save();
        }
    }
}
