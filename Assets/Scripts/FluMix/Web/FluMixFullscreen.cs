using System.Runtime.InteropServices;
using UnityEngine;

namespace FluMix
{
    /// <summary>
    /// WebGL: request browser fullscreen + try landscape lock (must run on user click).
    /// </summary>
    public static class FluMixFullscreen
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void FluMix_RequestFullscreenLandscape();
#endif

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
            // Editor / standalone: best-effort only.
            Screen.fullScreen = true;
#endif
        }
    }
}
