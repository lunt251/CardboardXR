using UnityEngine;

namespace CardboardXR.Demos
{
    // Opens the phone's gallery to pick a picture (Android only), through a small helper activity
    // (Assets/Plugins/Android/PhotoPickerActivity.java). The helper shrinks the picture, saves it in the
    // app's cache and calls the receiver's method with the file's path, or "" if nothing was picked.
    public static class PhotoPicker
    {
        // The longest side of the picture handed back, in pixels: more is more memory than it's worth.
        const int k_MaxSide = 2048;

        public static void Pick(string receiver, string method)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var picker = new AndroidJavaClass("com.brenpoly.cardboardxr.PhotoPickerActivity");
                picker.CallStatic("pick", activity, receiver, method, k_MaxSide);
                Debug.Log("[Photo] Opened the gallery");
            }
            catch (AndroidJavaException e)
            {
                Debug.LogWarning($"[Photo] Couldn't open the gallery: {e.Message}");
            }
#else
            Debug.Log($"[Photo] The gallery picker only works on Android (would hand back at most {k_MaxSide} px)");
#endif
        }
    }
}
