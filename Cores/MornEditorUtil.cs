using UnityEngine;

namespace MornLib
{
    public static class MornEditorUtil
    {
        /// <summary>
        /// Editor上でオブジェクトをDirtyとしてマーク。ランタイムでは何もしない。
        /// </summary>
        public static void SetDirty(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(target);
            }
#endif
        }
    }
}
