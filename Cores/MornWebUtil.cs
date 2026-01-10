using System.Diagnostics;
using UnityEngine;

namespace MornLib
{
    public static class MornWebUtil
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private extern static void OpenWindow(string url);
#endif
        public static void Open(string url)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                #if UNITY_WEBGL
                OpenWindow(url);
                #endif
            }
            else
            {
#if UNITY_EDITOR
                Process.Start(url);
#else
				UnityEngine.Debug.Log ("WebGL以外では実行できません。");
				UnityEngine.Debug.Log (url);
#endif
            }
        }
    }
}