using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CardboardXR.Stereo
{
    // Development overrides passed when launching the app, so settings can be compared without a
    // rebuild. The launch scripts take them the same way on both platforms:
    //   zsh Tools/build_and_run.sh --ei dof 6 --ei passthrough 1       (Android)
    //   zsh Tools/ios_launch.sh --ei dof 6 --ei passthrough 1          (iOS)
    // On Android they arrive as intent extras from `adb shell am start --ei`. iOS has no intents, so
    // there they arrive as environment variables named with k_EnvironmentPrefix, which devicectl sets
    // on launch; the same goes for the Editor and anything else. Only a launch from the Mac carries
    // them: opened from the home screen, the app takes its defaults.
    public static class LaunchOptions
    {
        public const string k_EnvironmentPrefix = "CARDBOARDXR_";

        const string k_SavedPrefix = "opt_";

        // A launch option given by the launch script wins; failing that, the value saved from the app's
        // own settings screen (SetSaved); failing that, the fallback.
        public static int GetInt(string name, int fallback)
        {
            if (TryGetLaunched(name, out var launched))
                return launched;
            return HasSaved(name) ? PlayerPrefs.GetInt(k_SavedPrefix + name) : fallback;
        }

        public static bool HasSaved(string name) => PlayerPrefs.HasKey(k_SavedPrefix + name);

        // Keeps a value for the next launches, for options the in-app settings screen sets.
        public static void SetSaved(string name, int value)
        {
            PlayerPrefs.SetInt(k_SavedPrefix + name, value);
            PlayerPrefs.Save();
        }

        public static void ClearSaved(string name)
        {
            PlayerPrefs.DeleteKey(k_SavedPrefix + name);
            PlayerPrefs.Save();
        }

        static bool TryGetLaunched(string name, out int value)
        {
            value = 0;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                if (!intent.Call<bool>("hasExtra", name))
                    return false;
                value = intent.Call<int>("getIntExtra", name, 0);
                return true;
            }
            catch (AndroidJavaException e)
            {
                Debug.LogWarning($"[Stereo] Couldn't read launch option {name}: {e.Message}");
            }
            return false;
#else
            return TryParse(Environment.GetEnvironmentVariable(k_EnvironmentPrefix + name), out value);
#endif
        }

        public static bool TryParse(string text, out int value)
        {
            value = 0;
            return text != null && int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

#if !UNITY_ANDROID || UNITY_EDITOR
        // Lists the options the environment carries, so a misspelled one shows rather than quietly
        // leaving a default in place.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LogOptions()
        {
            var found = new List<string>();
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = (string)entry.Key;
                if (!key.StartsWith(k_EnvironmentPrefix, StringComparison.Ordinal))
                    continue;
                var text = (string)entry.Value;
                found.Add(TryParse(text, out _)
                    ? $"{key.Substring(k_EnvironmentPrefix.Length)} {text}"
                    : $"{key.Substring(k_EnvironmentPrefix.Length)} \"{text}\" (not a whole number, ignored)");
            }
            if (found.Count > 0)
                Debug.Log($"[Stereo] Launch options: {string.Join(", ", found)}");
        }
#endif
    }
}
