using System.Runtime.InteropServices;
using UnityEngine;

namespace FluMix
{
    /// <summary>
    /// WebGL: request browser fullscreen + landscape lock (must run on user click — Login/Register).
    /// </summary>
    public static class FluMixFullscreen
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void FluMix_RequestFullscreenLandscape();
#endif

        /// <summary>
        /// Call from Login / Register button handlers (user gesture).
        /// </summary>
        public static void EnterForPlay()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                FluMix_RequestFullscreenLandscape();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[FluMist] Fullscreen request failed: " + ex.Message);
            }
#else
            Screen.fullScreen = true;
#endif
        }
    }
}
